namespace SafetyMonitorView.Models;

public class ColorScheme {
    #region Public Properties

    public bool IsGradient { get; set; } = false;
    public string Name { get; set; } = "Default";
    public List<ColorStop> Stops { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

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

    private static Color InterpolateColor(Color c1, Color c2, double ratio) {
        int r = (int)(c1.R + (c2.R - c1.R) * ratio);
        int g = (int)(c1.G + (c2.G - c1.G) * ratio);
        int b = (int)(c1.B + (c2.B - c1.B) * ratio);
        return Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    #endregion Private Methods
}

public class ColorStop {
    #region Public Properties

    public Color Color { get; set; }
    public string Description { get; set; } = "";
    public double Value { get; set; }

    #endregion Public Properties
}
