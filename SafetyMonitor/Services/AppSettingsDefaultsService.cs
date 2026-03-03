using SafetyMonitor.Models;

namespace SafetyMonitor.Services;

public static class AppSettingsDefaultsService {
    public static AppSettings CreateDefaults() {
        return new AppSettings {
            IsDarkTheme = false,
            IsMaximized = false,
            LinkChartPeriods = false,
            MinimizeToTray = false,
            ShowRefreshIndicator = true,
            StartMinimized = false,
            MaterialColorScheme = "Teal",
            ChartStaticModeTimeoutSeconds = 120,
            ChartStaticAggregationPresetMatchTolerancePercent = 10,
            ChartStaticAggregationTargetPointCount = 300,
            ChartRawDataPointIntervalSeconds = 3,
            LastDashboardId = null,
            RefreshInterval = 5,
            ValueTileLookbackMinutes = 60,
            ChartPeriodPresets = ChartPeriodPresetStore.CreateDefaultPresets(300, 3),
            MetricAxisRules = [
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 45, MaxSpan = null, Metric = MetricType.Temperature, MinBoundary = -30, MinSpan = 3 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 110, MaxSpan = null, Metric = MetricType.Humidity, MinBoundary = -10, MinSpan = 2 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 1060, MaxSpan = null, Metric = MetricType.Pressure, MinBoundary = 940, MinSpan = 10 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 45, MaxSpan = null, Metric = MetricType.DewPoint, MinBoundary = -30, MinSpan = 3 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 110, MaxSpan = null, Metric = MetricType.CloudCover, MinBoundary = -10, MinSpan = 2 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 45, MaxSpan = null, Metric = MetricType.SkyTemperature, MinBoundary = -50, MinSpan = 3 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 100000, MaxSpan = null, Metric = MetricType.SkyBrightness, MinBoundary = -1, MinSpan = null },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 25, MaxSpan = null, Metric = MetricType.SkyQuality, MinBoundary = 5, MinSpan = 2 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 25, MaxSpan = null, Metric = MetricType.RainRate, MinBoundary = -5, MinSpan = 2 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 20, MaxSpan = null, Metric = MetricType.WindSpeed, MinBoundary = -5, MinSpan = 2 },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 20, MaxSpan = null, Metric = MetricType.WindGust, MinBoundary = -5, MinSpan = 2 },
                new MetricAxisRuleSetting { Enabled = false, MaxBoundary = null, MaxSpan = null, Metric = MetricType.StarFwhm, MinBoundary = null, MinSpan = null },
                new MetricAxisRuleSetting { Enabled = true, MaxBoundary = 110, MaxSpan = null, Metric = MetricType.IsSafe, MinBoundary = -10, MinSpan = 5 }
            ],
            MetricDisplaySettings = [],
            StoragePath = string.Empty,
            WindowHeight = 900,
            WindowWidth = 1400,
            WindowX = -1,
            WindowY = -1,
        };
    }

    public static AppSettings Normalize(AppSettings? loaded) {
        var defaults = CreateDefaults();
        if (loaded is null) {
            return defaults;
        }

        loaded.MetricAxisRules ??= defaults.MetricAxisRules;
        loaded.MetricDisplaySettings ??= defaults.MetricDisplaySettings;
        loaded.WindowWidth = loaded.WindowWidth <= 0 ? defaults.WindowWidth : loaded.WindowWidth;
        loaded.WindowHeight = loaded.WindowHeight <= 0 ? defaults.WindowHeight : loaded.WindowHeight;
        loaded.RefreshInterval = loaded.RefreshInterval <= 0 ? defaults.RefreshInterval : loaded.RefreshInterval;
        loaded.ValueTileLookbackMinutes = loaded.ValueTileLookbackMinutes <= 0 ? defaults.ValueTileLookbackMinutes : loaded.ValueTileLookbackMinutes;
        loaded.ChartStaticModeTimeoutSeconds = loaded.ChartStaticModeTimeoutSeconds <= 0 ? defaults.ChartStaticModeTimeoutSeconds : loaded.ChartStaticModeTimeoutSeconds;
        loaded.ChartStaticAggregationTargetPointCount = loaded.ChartStaticAggregationTargetPointCount <= 0 ? defaults.ChartStaticAggregationTargetPointCount : loaded.ChartStaticAggregationTargetPointCount;
        loaded.ChartStaticAggregationPresetMatchTolerancePercent = loaded.ChartStaticAggregationPresetMatchTolerancePercent <= 0 ? defaults.ChartStaticAggregationPresetMatchTolerancePercent : loaded.ChartStaticAggregationPresetMatchTolerancePercent;
        loaded.ChartRawDataPointIntervalSeconds = loaded.ChartRawDataPointIntervalSeconds <= 0 ? defaults.ChartRawDataPointIntervalSeconds : loaded.ChartRawDataPointIntervalSeconds;
        loaded.ChartPeriodPresets = loaded.ChartPeriodPresets?.Count > 0
            ? loaded.ChartPeriodPresets
            : ChartPeriodPresetStore.CreateDefaultPresets(loaded.ChartStaticAggregationTargetPointCount, loaded.ChartRawDataPointIntervalSeconds);
        loaded.StoragePath ??= string.Empty;
        loaded.MaterialColorScheme = AppColorizationService.Instance.NormalizeMaterialSchemeName(loaded.MaterialColorScheme);

        return loaded;
    }
}
