namespace SafetyMonitorView.Models;

public class AppSettings {
    #region Public Properties

    // Theme settings
    public bool IsDarkTheme { get; set; } = false;
    public bool IsMaximized { get; set; } = false;
    public bool LinkChartPeriods { get; set; } = false;
    public int ChartStaticModeTimeoutSeconds { get; set; } = 120;
    public double ChartStaticAggregationPresetMatchTolerancePercent { get; set; } = 10;
    public int ChartStaticAggregationTargetPointCount { get; set; } = 300;
    public int ChartAggregationRoundingSeconds { get; set; } = 1;

    // Dashboard settings
    public Guid? LastDashboardId { get; set; }
    public int RefreshInterval { get; set; } = 5;
    public int ValueTileLookbackMinutes { get; set; } = 60;

    // Data settings
    public List<ChartPeriodPresetDefinition> ChartPeriodPresets { get; set; } = ChartPeriodPresetStore.CreateDefaultPresets();
    public List<MetricAxisRuleSetting> MetricAxisRules { get; set; } = [];
    public string StoragePath { get; set; } = "";
    public int WindowHeight { get; set; } = 900;

    // Window settings
    public int WindowWidth { get; set; } = 1400;
    public int WindowX { get; set; } = -1;  // -1 = center
    public int WindowY { get; set; } = -1;

    #endregion Public Properties

    // -1 = center
}
