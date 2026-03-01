namespace SafetyMonitor.Models;

public class AppSettings {
    #region Public Properties

    // Theme settings
    public bool IsDarkTheme { get; set; }
    public bool IsMaximized { get; set; }
    public bool LinkChartPeriods { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool ShowRefreshIndicator { get; set; }
    public bool StartMinimized { get; set; }
    public string MaterialColorScheme { get; set; } = "Teal";
    public int ChartStaticModeTimeoutSeconds { get; set; }
    public double ChartStaticAggregationPresetMatchTolerancePercent { get; set; }
    public int ChartStaticAggregationTargetPointCount { get; set; }
    public int ChartRawDataPointIntervalSeconds { get; set; }

    // Dashboard settings
    public Guid? LastDashboardId { get; set; }
    public int RefreshInterval { get; set; }
    public int ValueTileLookbackMinutes { get; set; }

    // Data settings
    public List<ChartPeriodPresetDefinition> ChartPeriodPresets { get; set; } = [];
    public List<MetricAxisRuleSetting> MetricAxisRules { get; set; } = [];
    public List<MetricDisplaySetting> MetricDisplaySettings { get; set; } = [];
    public string StoragePath { get; set; } = "";
    public int WindowHeight { get; set; }

    // Window settings
    public int WindowWidth { get; set; }
    public int WindowX { get; set; } = -1;  // -1 = center
    public int WindowY { get; set; } = -1;

    #endregion Public Properties

    // -1 = center
}
