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
            MetricAxisRules = [],
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
