namespace SafetyMonitor.Models;

/// <summary>
/// Represents metric display setting and encapsulates its related behavior and state.
/// </summary>
public class MetricDisplaySetting {

    /// <summary>
    /// Gets or sets the decimals for metric display setting. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int Decimals { get; set; } = 2;
    /// <summary>
    /// Gets or sets the hide zeroes for metric display setting. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool HideZeroes { get; set; }
    /// <summary>
    /// Gets or sets the invert y for metric display setting. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public bool InvertY { get; set; }
    /// <summary>
    /// Gets or sets the log y for metric display setting. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public bool LogY { get; set; }
    /// <summary>
    /// Gets or sets the metric for metric display setting. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public MetricType Metric { get; set; }
    /// <summary>
    /// Gets or sets the tray name for metric display setting. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string TrayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the tray value scheme name for metric display setting. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string TrayValueSchemeName { get; set; } = string.Empty;
}
