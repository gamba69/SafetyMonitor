namespace SafetyMonitor.Models;

/// <summary>
/// Represents app settings and encapsulates its related behavior and state.
/// </summary>
public class AppSettings {
    #region Public Properties

    // Theme settings
    /// <summary>
    /// Gets or sets the is dark theme for app settings. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public bool IsDarkTheme { get; set; }
    /// <summary>
    /// Gets or sets the is maximized for app settings. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool IsMaximized { get; set; }
    /// <summary>
    /// Gets or sets the minimize to tray for app settings. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool MinimizeToTray { get; set; }
    /// <summary>
    /// Gets or sets the show refresh indicator for app settings. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowRefreshIndicator { get; set; }
    /// <summary>
    /// Gets or sets the start minimized for app settings. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool StartMinimized { get; set; }
    /// <summary>
    /// Gets or sets the material color scheme for app settings. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string MaterialColorScheme { get; set; } = "Teal";
    /// <summary>
    /// Gets or sets the chart static mode timeout seconds for app settings. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int ChartStaticModeTimeoutSeconds { get; set; }
    /// <summary>
    /// Gets or sets the chart static aggregation preset match tolerance percent for app settings. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public double ChartStaticAggregationPresetMatchTolerancePercent { get; set; }
    /// <summary>
    /// Gets or sets the chart static aggregation target point count for app settings. Specifies sizing or boundary constraints used by runtime calculations.
    /// </summary>
    public int ChartStaticAggregationTargetPointCount { get; set; }
    /// <summary>
    /// Gets or sets the chart raw data point interval seconds for app settings. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int ChartRawDataPointIntervalSeconds { get; set; }

    // Dashboard settings
    /// <summary>
    /// Gets or sets the last dashboard id for app settings. Identifies the related entity and is used for lookups, linking, or persistence.
    /// </summary>
    public Guid? LastDashboardId { get; set; }
    /// <summary>
    /// Gets or sets the refresh interval for app settings. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int RefreshInterval { get; set; }
    /// <summary>
    /// Gets or sets the value tile lookback minutes for app settings. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int ValueTileLookbackMinutes { get; set; }

    // Data settings
    /// <summary>
    /// Gets or sets the chart period presets for app settings. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public List<ChartPeriodPresetDefinition> ChartPeriodPresets { get; set; } = [];
    /// <summary>
    /// Gets or sets the metric axis rules for app settings. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<MetricAxisRuleSetting> MetricAxisRules { get; set; } = [];
    /// <summary>
    /// Gets or sets the metric display settings for app settings. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<MetricDisplaySetting> MetricDisplaySettings { get; set; } = [];
    /// <summary>
    /// Gets or sets the storage path for app settings. Specifies a filesystem location used to load or persist application data.
    /// </summary>
    public string StoragePath { get; set; } = "";
    /// <summary>
    /// Gets or sets the validate database structure on startup for app settings. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ValidateDatabaseStructureOnStartup { get; set; } = false;
    /// <summary>
    /// Gets or sets the window height for app settings. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int WindowHeight { get; set; }

    // Window settings
    /// <summary>
    /// Gets or sets the window width for app settings. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int WindowWidth { get; set; }
    /// <summary>
    /// Gets or sets the window x for app settings. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int WindowX { get; set; } = -1;  // -1 = center
    /// <summary>
    /// Gets or sets the window y for app settings. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int WindowY { get; set; } = -1;

    #endregion Public Properties

    // -1 = center
}
