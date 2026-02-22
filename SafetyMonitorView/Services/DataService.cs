using DataStorage.Models;
using FirebirdSql.Data.FirebirdClient;
using SafetyMonitorView.Models;
using System.Threading;

namespace SafetyMonitorView.Services;

public class DataService {

    #region Private Fields

    private readonly DataStorage.DataStorage? _storage;
    private readonly int _valueTileLookbackMinutes;
    private readonly object _valueTileSnapshotLock = new();
    private bool _isConnectionFailed;
    private bool _isValueTileSnapshotActive;
    private bool _isValueTileSnapshotLoaded;
    private ObservingData? _valueTileSnapshotData;

    #endregion Private Fields

    #region Public Constructors

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
            var (startTime, endTime) = GetPeriodRange(period, customStart, customEnd, customDuration);
            if (aggregationInterval.HasValue && aggregationFunction.HasValue) {
                return [.. _storage.GetData(
                    startTime,
                    endTime,
                    aggregationInterval.Value,
                    aggregationFunction.Value
                )];
            } else {
                return [.. _storage.GetData(startTime, endTime)];
            }
        } catch (FbException ex) {
            HandleConnectionFailure(ex.Message);
            return [];
        } catch {
            return [];
        }
    }

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

    public IDisposable BeginValueTileSnapshot() {
        lock (_valueTileSnapshotLock) {
            _isValueTileSnapshotActive = true;
            _isValueTileSnapshotLoaded = false;
            _valueTileSnapshotData = null;
        }

        return new ValueTileSnapshotScope(this);
    }

    #endregion Public Methods

    #region Private Methods

    private static (DateTime start, DateTime end) GetPeriodRange(
        ChartPeriod period,
        DateTime? customStart,
        DateTime? customEnd,
        TimeSpan? customDuration) {
        var endTime = customEnd ?? DateTime.UtcNow;
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

    private void HandleConnectionFailure(string details) {
        if (_isConnectionFailed) {
            return;
        }

        _isConnectionFailed = true;
        ConnectionFailed?.Invoke(details);
    }

    private void EndValueTileSnapshot() {
        lock (_valueTileSnapshotLock) {
            _isValueTileSnapshotActive = false;
            _isValueTileSnapshotLoaded = false;
            _valueTileSnapshotData = null;
        }
    }

    private sealed class ValueTileSnapshotScope : IDisposable {

        #region Private Fields

        private DataService? _owner;

        #endregion Private Fields

        #region Public Constructors

        public ValueTileSnapshotScope(DataService owner) {
            _owner = owner;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose() {
            var owner = Interlocked.Exchange(ref _owner, null);
            owner?.EndValueTileSnapshot();
        }

        #endregion Public Methods
    }

    #endregion Private Methods
}
