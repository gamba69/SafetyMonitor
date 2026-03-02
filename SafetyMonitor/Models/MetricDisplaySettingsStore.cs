namespace SafetyMonitor.Models;

public static class MetricDisplaySettingsStore {

    private static List<MetricDisplaySetting> _settings = [];

    public static event Action? SettingsChanged;

    public static IReadOnlyList<MetricDisplaySetting> Settings => _settings;

    public static MetricDisplaySetting GetSettingOrDefault(MetricType metric) {
        return _settings.FirstOrDefault(s => s.Metric == metric) ?? CreateDefaultSetting(metric);
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
                LogY = s.LogY,
                TrayName = s.TrayName,
                TrayValueSchemeName = s.TrayValueSchemeName
            }))
            : new List<MetricDisplaySetting>();

        foreach (var metric in Enum.GetValues<MetricType>()) {
            if (loadedSettings.Any(s => s.Metric == metric)) {
                continue;
            }

            loadedSettings.Add(CreateDefaultSetting(metric));
        }

        _settings = [.. loadedSettings.OrderBy(s => (int)s.Metric)];
        SettingsChanged?.Invoke();
    }

    private static MetricDisplaySetting CreateDefaultSetting(MetricType metric) {
        return new MetricDisplaySetting {
            Metric = metric,
            TrayValueSchemeName = metric switch {
                MetricType.Temperature => "Temperature Status",
                MetricType.Humidity => "Humidity Status",
                MetricType.Pressure => "Pressure Status",
                MetricType.CloudCover => "Cloud Cover Status",
                MetricType.SkyBrightness => "Sky Brightness Status",
                MetricType.SkyQuality => "Sky Quality Status",
                MetricType.RainRate => "Rain Rate Status",
                MetricType.WindSpeed => "Wind Speed Status",
                MetricType.WindGust => "Wind Gust Status",
                MetricType.IsSafe => "Safety Status",
                _ => string.Empty
            }
        };
    }
}
