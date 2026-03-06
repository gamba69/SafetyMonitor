namespace SafetyMonitor.Models;

/// <summary>
/// Represents color scheme and encapsulates its related behavior and state.
/// </summary>
public class ColorScheme {
    #region Public Properties

    /// <summary>
    /// Gets or sets the is gradient for color scheme. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool IsGradient { get; set; } = false;
    /// <summary>
    /// Gets or sets the name for color scheme. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Name { get; set; } = "Default";
    /// <summary>
    /// Gets or sets the stops for color scheme. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<ColorStop> Stops { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Gets the color for color scheme.
    /// </summary>
    /// <param name="value">Input value for value.</param>
    /// <returns>The result of the operation.</returns>
    public Color GetColor(double value) {
        if (Stops.Count == 0) {
            return Color.Gray;
        }

        var sorted = Stops.OrderBy(s => s.Value).ToList();

        if (!IsGradient) {
            // Discrete mode — find first stop where value <= stop.Value
            foreach (var stop in sorted) {
                if (value <= stop.Value) {
                    return stop.Color;
                }
            }
            return sorted[^1].Color;
        }

        // Gradient mode — interpolate; values at specified points match exactly.
        if (sorted.Count == 1) {
            return sorted[0].Color;
        }

        if (value <= sorted[0].Value) {
            return sorted[0].Color;
        }

        if (value >= sorted[^1].Value) {
            return sorted[^1].Color;
        }

        for (int i = 0; i < sorted.Count - 1; i++) {
            if (value >= sorted[i].Value && value <= sorted[i + 1].Value) {
                var range = sorted[i + 1].Value - sorted[i].Value;
                if (range <= 0) {
                    return sorted[i].Color;
                }

                var ratio = (value - sorted[i].Value) / range;
                return InterpolateColor(sorted[i].Color, sorted[i + 1].Color, ratio);
            }
        }

        return sorted[^1].Color;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Executes interpolate color as part of color scheme processing.
    /// </summary>
    /// <param name="c1">Input value for c 1.</param>
    /// <param name="c2">Input value for c 2.</param>
    /// <param name="ratio">Input value for ratio.</param>
    /// <returns>The result of the operation.</returns>
    private static Color InterpolateColor(Color c1, Color c2, double ratio) {
        int r = (int)(c1.R + (c2.R - c1.R) * ratio);
        int g = (int)(c1.G + (c2.G - c1.G) * ratio);
        int b = (int)(c1.B + (c2.B - c1.B) * ratio);
        return Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    #endregion Private Methods
}

/// <summary>
/// Represents color stop and encapsulates its related behavior and state.
/// </summary>
public class ColorStop {
    #region Public Properties

    /// <summary>
    /// Gets or sets the color for color stop. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public Color Color { get; set; }
    /// <summary>
    /// Gets or sets the description for color stop. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// Gets or sets the value for color stop. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public double Value { get; set; }

    #endregion Public Properties
}
