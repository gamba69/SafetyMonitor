using DataStorage.Models;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Services;

public class DataService {

    #region Private Fields

    private readonly DataStorage.DataStorage? _storage;

    #endregion Private Fields

    #region Public Constructors

    public DataService(string? storagePath = null) {
        if (!string.IsNullOrEmpty(storagePath) && Directory.Exists(storagePath)) {
            try {
                _storage = new DataStorage.DataStorage(storagePath);
            } catch { _storage = null; }
        }
    }

    #endregion Public Constructors

    #region Public Properties

    public bool IsConnected => _storage != null;

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
        TimeSpan? aggregationInterval = null,
        AggregationFunction? aggregationFunction = null) {
        if (_storage == null) {
            return [];
        }
        try {
            var (startTime, endTime) = GetPeriodRange(period, customStart, customEnd);
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
        } catch {
            return [];
        }
    }

    public ObservingData? GetLatestData() {
        if (_storage == null) {
            return null;
        }
        try {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddMinutes(-500);
            var data = _storage.GetData(startTime, endTime);
            return data.OrderByDescending(d => d.Timestamp).FirstOrDefault();
        } catch {
            return null;
        }
    }

    #endregion Public Methods

    #region Private Methods

    private static (DateTime start, DateTime end) GetPeriodRange(
        ChartPeriod period,
        DateTime? customStart,
        DateTime? customEnd) {
        var endTime = customEnd ?? DateTime.UtcNow;
        var startTime = period switch {
            ChartPeriod.Last15Minutes => endTime.AddMinutes(-15),
            ChartPeriod.LastHour => endTime.AddHours(-1),
            ChartPeriod.Last6Hours => endTime.AddHours(-6),
            ChartPeriod.Last24Hours => endTime.AddHours(-24),
            ChartPeriod.Last7Days => endTime.AddDays(-7),
            ChartPeriod.Last30Days => endTime.AddDays(-30),
            ChartPeriod.Custom => customStart ?? endTime.AddHours(-24),
            _ => endTime.AddHours(-24)
        };
        return (startTime, endTime);
    }

    #endregion Private Methods
}
