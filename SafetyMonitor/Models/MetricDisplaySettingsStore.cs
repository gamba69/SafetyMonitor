using SafetyMonitor.Services;

namespace SafetyMonitor.Models;

public static class MetricDisplaySettingsStore {

    private static List<MetricDisplaySetting> _settings = [];

    public static event Action? SettingsChanged;

    public static IReadOnlyList<MetricDisplaySetting> Settings => _settings;

    public static MetricDisplaySetting GetSettingOrDefault(MetricType metric) {
        return _settings.FirstOrDefault(s => s.Metric == metric) ?? CreateDefaultSetting(metric);
    }

    public static MetricDisplaySetting GetDefaultSetting(MetricType metric) {
        return CreateDefaultSetting(metric);
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
        var setting = new MetricDisplaySetting {
            Metric = metric,
            TrayValueSchemeName = metric == MetricType.IsSafe
                ? ValueSchemeService.GetDefaultSchemeName(metric)
                : string.Empty
        };

        switch (metric) {
            case MetricType.Temperature:
                setting.Decimals = 1;
                setting.TrayName = "T";
                break;
            case MetricType.Humidity:
                setting.Decimals = 0;
                setting.TrayName = "H";
                break;
            case MetricType.DewPoint:
                setting.Decimals = 1;
                setting.TrayName = "D";
                break;
            case MetricType.CloudCover:
                setting.Decimals = 0;
                setting.TrayName = "C";
                break;
            case MetricType.SkyQuality:
                setting.Decimals = 1;
                setting.InvertY = true;
                setting.TrayName = "S";
                break;
            case MetricType.RainRate:
                setting.Decimals = 2;
                setting.TrayName = "R";
                break;
            case MetricType.WindSpeed:
                setting.Decimals = 1;
                setting.TrayName = "W";
                break;
            case MetricType.IsSafe:
                setting.Decimals = 0;
                setting.TrayName = "S";
                break;
            case MetricType.Pressure:
                setting.Decimals = 0;
                break;
            case MetricType.SkyTemperature:
                setting.Decimals = 1;
                break;
            case MetricType.SkyBrightness:
                setting.Decimals = 3;
                setting.HideZeroes = true;
                setting.LogY = true;
                break;
            case MetricType.WindGust:
                setting.Decimals = 1;
                break;
            case MetricType.WindDirection:
                setting.Decimals = 0;
                break;
            case MetricType.StarFwhm:
                setting.Decimals = 2;
                break;
        }

        return setting;
    }
}
