namespace SafetyMonitor.Models;

/// <summary>
/// Represents metric aggregation and encapsulates its related behavior and state.
/// </summary>
public class MetricAggregation {

    #region Public Properties

    /// <summary>
    /// Gets or sets the color for metric aggregation. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public Color Color { get; set; } = Color.Blue;
    /// <summary>
    /// Gets or sets the dark theme color for metric aggregation. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public Color DarkThemeColor { get; set; } = Color.Empty;
    /// <summary>
    /// Gets or sets the function for metric aggregation. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public DataStorage.Models.AggregationFunction Function { get; set; }
    /// <summary>
    /// Gets or sets the label for metric aggregation. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the line width for metric aggregation. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public float LineWidth { get; set; } = 2f;
    /// <summary>
    /// Gets or sets the metric for metric aggregation. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public MetricType Metric { get; set; }
    /// <summary>
    /// Gets or sets the smooth for metric aggregation. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool Smooth { get; set; } = false;
    /// <summary>
    /// Gets or sets the tension for metric aggregation. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public float Tension { get; set; } = 0.5f;
    /// <summary>
    /// Gets or sets the show markers for metric aggregation. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowMarkers { get; set; } = false;
    /// <summary>
    /// Gets or sets the value scheme name for metric aggregation. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string ValueSchemeName { get; set; } = "";

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Gets the color for theme for metric aggregation.
    /// </summary>
    /// <param name="isLightTheme">Input value for is light theme.</param>
    /// <returns>The result of the operation.</returns>
    public Color GetColorForTheme(bool isLightTheme) {
        if (isLightTheme) {
            return Color;
        }

        return DarkThemeColor.IsEmpty || DarkThemeColor.A == 0 ? Color : DarkThemeColor;
    }

    #endregion Public Methods
}
