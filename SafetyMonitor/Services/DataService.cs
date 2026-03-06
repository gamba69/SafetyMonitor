using DataStorage.Models;
using FirebirdSql.Data.FirebirdClient;
using SafetyMonitor.Models;
using System.Threading;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents data service and encapsulates its related behavior and state.
/// </summary>
public class DataService {

    #region Private Fields

    private readonly DataStorage.DataStorage? _storage;
    private readonly int _valueTileLookbackMinutes;
    private readonly Lock _valueTileSnapshotLock = new();
    private bool _isConnectionFailed;
    private bool _isValueTileSnapshotActive;
    private bool _isValueTileSnapshotLoaded;
    private ObservingData? _valueTileSnapshotData;

    // Cross-tile chart data cache — avoids duplicate DB queries when multiple
    // ChartTiles share the same period / aggregation function within one refresh cycle.
    private readonly Lock _chartSnapshotLock = new();
    private DateTime? _chartSnapshotNow;
    private Dictionary<(DateTime, DateTime, TimeSpan?, AggregationFunction?), List<ObservingData>>? _chartSnapshotCache;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DataService"/> class.
    /// </summary>
    /// <param name="storagePath">Path value for storage path.</param>
    /// <param name="valueTileLookbackMinutes">Input value for value tile lookback minutes.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public DataService(string? storagePath = null, int valueTileLookbackMinutes = 60) {
        _valueTileLookbackMinutes = Math.Max(1, valueTileLookbackMinutes);
        if (!string.IsNullOrEmpty(storagePath) && Directory.Exists(storagePath)) {
            try {
                _storage = new DataStorage.DataStorage(storagePath);
            } catch { _storage = null; }
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public bool IsConnected => _storage != null;

    public event Action<string>? ConnectionFailed;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Gets the recommended aggregation interval for data service.
    /// </summary>
    /// <param name="period">Input value for period.</param>
    /// <returns>The result of the operation.</returns>
    public static TimeSpan? GetRecommendedAggregationInterval(ChartPeriod period) => period switch {
        ChartPeriod.Last15Minutes => null,
        ChartPeriod.LastHour => TimeSpan.FromMinutes(1),
        ChartPeriod.Last6Hours => TimeSpan.FromMinutes(5),
        ChartPeriod.Last24Hours => TimeSpan.FromMinutes(15),
        ChartPeriod.Last7Days => TimeSpan.FromHours(1),
        ChartPeriod.Last30Days => TimeSpan.FromHours(6),
        _ => null
    };

    public List<ObservingData> GetChartData(
        ChartPeriod period,
        DateTime? customStart = null,
        DateTime? customEnd = null,
        TimeSpan? customDuration = null,
        TimeSpan? aggregationInterval = null,
        AggregationFunction? aggregationFunction = null) {
        if (_storage == null) {
            return [];
        }

        if (_isConnectionFailed) {
            return [];
        }

        try {
            // Use frozen UtcNow when a chart snapshot is active so every tile
            // in the same refresh cycle gets identical time boundaries.
            DateTime? frozenNow;
            Dictionary<(DateTime, DateTime, TimeSpan?, AggregationFunction?), List<ObservingData>>? cache;
            lock (_chartSnapshotLock) {
                frozenNow = _chartSnapshotNow;
                cache = _chartSnapshotCache;
            }

            var (startTime, endTime) = GetPeriodRange(period, customStart, customEnd, customDuration, frozenNow);

            var cacheKey = (startTime, endTime, aggregationInterval, aggregationFunction);
            if (cache != null) {
                lock (_chartSnapshotLock) {
                    if (_chartSnapshotCache != null && _chartSnapshotCache.TryGetValue(cacheKey, out var cached)) {
                        return cached;
                    }
                }
            }

            List<ObservingData> result;
            if (aggregationInterval.HasValue && aggregationFunction.HasValue) {
                result = [.. _storage.GetData(
                    startTime,
                    endTime,
                    aggregationInterval.Value,
                    aggregationFunction.Value
                )];
            } else {
                result = [.. _storage.GetData(startTime, endTime)];
            }

            if (cache != null) {
                lock (_chartSnapshotLock) {
                    _chartSnapshotCache?.TryAdd(cacheKey, result);
                }
            }

            return result;
        } catch (FbException ex) {
            HandleConnectionFailure(ex.Message);
            return [];
        } catch {
            return [];
        }
    }

    /// <summary>
    /// Gets the latest data for data service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ObservingData? GetLatestData() {
        if (_storage == null) {
            return null;
        }

        if (_isConnectionFailed) {
            return null;
        }

        try {
            var endTime = DateTime.UtcNow;
            var maxLookback = TimeSpan.FromMinutes(_valueTileLookbackMinutes);

            lock (_valueTileSnapshotLock) {
                if (_isValueTileSnapshotActive && _isValueTileSnapshotLoaded) {
                    return _valueTileSnapshotData;
                }
            }

            var latest = _storage.GetLatestData(endTime, maxLookback);

            lock (_valueTileSnapshotLock) {
                if (_isValueTileSnapshotActive) {
                    _isValueTileSnapshotLoaded = true;
                    _valueTileSnapshotData = latest;
                }
            }

            return latest;
        } catch (FbException ex) {
            HandleConnectionFailure(ex.Message);
            return null;
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Executes begin value tile snapshot as part of data service processing.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IDisposable BeginValueTileSnapshot() {
        lock (_valueTileSnapshotLock) {
            _isValueTileSnapshotActive = true;
            _isValueTileSnapshotLoaded = false;
            _valueTileSnapshotData = null;
        }

        return new ValueTileSnapshotScope(this);
    }

    /// <summary>
    /// Executes begin chart data snapshot as part of data service processing.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IDisposable BeginChartDataSnapshot() {
        lock (_chartSnapshotLock) {
            _chartSnapshotNow = DateTime.UtcNow;
            _chartSnapshotCache = [];
        }

        return new ChartDataSnapshotScope(this);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Initializes a new instance of the <see cref="static"/> class.
    /// </summary>
    /// <param name="start">Input value for start.</param>
    /// <param name="end">Input value for end.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    private static (DateTime start, DateTime end) GetPeriodRange(
        ChartPeriod period,
        DateTime? customStart,
        DateTime? customEnd,
        TimeSpan? customDuration,
        DateTime? frozenNow = null) {
        var endTime = customEnd ?? frozenNow ?? DateTime.UtcNow;
        var startTime = period switch {
            ChartPeriod.Last15Minutes => endTime.AddMinutes(-15),
            ChartPeriod.LastHour => endTime.AddHours(-1),
            ChartPeriod.Last6Hours => endTime.AddHours(-6),
            ChartPeriod.Last24Hours => endTime.AddHours(-24),
            ChartPeriod.Last7Days => endTime.AddDays(-7),
            ChartPeriod.Last30Days => endTime.AddDays(-30),
            ChartPeriod.Custom => customStart ?? (customDuration.HasValue ? endTime.Add(-customDuration.Value) : endTime.AddHours(-24)),
            _ => endTime.AddHours(-24)
        };
        return (startTime, endTime);
    }

    /// <summary>
    /// Handles the connection failure for data service.
    /// </summary>
    /// <param name="details">Input value for details.</param>
    private void HandleConnectionFailure(string details) {
        if (_isConnectionFailed) {
            return;
        }

        _isConnectionFailed = true;
        ConnectionFailed?.Invoke(details);
    }

    /// <summary>
    /// Executes end value tile snapshot as part of data service processing.
    /// </summary>
    private void EndValueTileSnapshot() {
        lock (_valueTileSnapshotLock) {
            _isValueTileSnapshotActive = false;
            _isValueTileSnapshotLoaded = false;
            _valueTileSnapshotData = null;
        }
    }

    /// <summary>
    /// Executes end chart data snapshot as part of data service processing.
    /// </summary>
    private void EndChartDataSnapshot() {
        lock (_chartSnapshotLock) {
            _chartSnapshotNow = null;
            _chartSnapshotCache = null;
        }
    }

    /// <summary>
    /// Represents value tile snapshot scope and encapsulates its related behavior and state.
    /// </summary>
    private sealed class ValueTileSnapshotScope(DataService owner) : IDisposable {

        #region Private Fields

        private DataService? _owner = owner;

        #endregion Private Fields
        #region Public Constructors

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Executes dispose as part of value tile snapshot scope processing.
        /// </summary>
        public void Dispose() {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.EndValueTileSnapshot();
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Represents chart data snapshot scope and encapsulates its related behavior and state.
    /// </summary>
    private sealed class ChartDataSnapshotScope(DataService owner) : IDisposable {

        #region Private Fields

        private DataService? _owner = owner;

        #endregion Private Fields
        #region Public Constructors

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Executes dispose as part of chart data snapshot scope processing.
        /// </summary>
        public void Dispose() {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.EndChartDataSnapshot();
        }

        #endregion Public Methods
    }

    #endregion Private Methods
}
