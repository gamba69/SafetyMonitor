namespace SafetyMonitor.Models;

/// <summary>
/// Represents value scheme and encapsulates its related behavior and state.
/// </summary>
public class ValueScheme {
    #region Public Properties

    /// <summary>
    /// Gets or sets the descending for value scheme. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool Descending { get; set; }
    /// <summary>
    /// Gets or sets the name for value scheme. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Name { get; set; } = "Default";
    /// <summary>
    /// Gets or sets the stops for value scheme. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<ValueStop> Stops { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Gets the text for value scheme.
    /// </summary>
    /// <param name="value">Input value for value.</param>
    /// <returns>The resulting string value.</returns>
    public string? GetText(double value) {
        if (Stops.Count == 0) {
            return null;
        }

        if (Descending) {
            var sorted = Stops.OrderByDescending(s => s.Value).ToList();

            foreach (var stop in sorted) {
                if (value >= stop.Value) {
                    return stop.Text;
                }
            }
            return sorted[^1].Text;
        } else {
            var sorted = Stops.OrderBy(s => s.Value).ToList();

            foreach (var stop in sorted) {
                if (value <= stop.Value) {
                    return stop.Text;
                }
            }
            return sorted[^1].Text;
        }
    }

    #endregion Public Methods
}

/// <summary>
/// Represents value stop and encapsulates its related behavior and state.
/// </summary>
public class ValueStop {
    #region Public Properties

    /// <summary>
    /// Gets or sets the description for value stop. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// Gets or sets the text for value stop. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Text { get; set; } = "";
    /// <summary>
    /// Gets or sets the value for value stop. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public double Value { get; set; }

    #endregion Public Properties
}
