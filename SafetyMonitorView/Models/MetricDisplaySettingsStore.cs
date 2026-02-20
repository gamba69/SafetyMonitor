namespace SafetyMonitorView.Models;

public static class MetricDisplaySettingsStore {

    private static List<MetricDisplaySetting> _settings = [];

    public static event Action? SettingsChanged;

    public static IReadOnlyList<MetricDisplaySetting> Settings => _settings;

    public static MetricDisplaySetting GetSettingOrDefault(MetricType metric) {
        return _settings.FirstOrDefault(s => s.Metric == metric) ?? new MetricDisplaySetting { Metric = metric };
    }

    public static string FormatMetricValue(MetricType metric, double value) {
        var setting = GetSettingOrDefault(metric);
        var decimals = Math.Max(0, setting.Decimals);

        if (metric == MetricType.IsSafe) {
            return value.ToString("0");
        }

        if (!setting.HideZeroes || value == 0d) {
            return value.ToString($"F{decimals}");
        }

        return value.ToString($"F{decimals}").TrimEnd('0').TrimEnd('.');
    }

    public static void SetSettings(IEnumerable<MetricDisplaySetting>? settings) {
        var loadedSettings = settings != null
            ? new List<MetricDisplaySetting>(settings.Select(s => new MetricDisplaySetting {
                Metric = s.Metric,
                Decimals = s.Decimals,
                HideZeroes = s.HideZeroes,
                InvertY = s.InvertY,
                TrayName = s.TrayName
            }))
            : new List<MetricDisplaySetting>();

        foreach (var metric in Enum.GetValues<MetricType>()) {
            if (loadedSettings.Any(s => s.Metric == metric)) {
                continue;
            }

            loadedSettings.Add(new MetricDisplaySetting { Metric = metric });
        }

        _settings = [.. loadedSettings.OrderBy(s => (int)s.Metric)];
        SettingsChanged?.Invoke();
    }
}
