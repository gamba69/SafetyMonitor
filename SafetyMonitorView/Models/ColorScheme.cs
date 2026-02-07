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

        if (!IsGradient) {
            // Discrete mode - find exact range
            var stop = Stops.FirstOrDefault(s =>
                (!s.MinValue.HasValue || value >= s.MinValue.Value) &&
                (!s.MaxValue.HasValue || value <= s.MaxValue.Value));
            return stop?.Color ?? Stops.Last().Color;
        }

        // Gradient mode - each stop is a control point; interpolate between them.
        // The representative value of a stop is: midpoint of (Min,Max), or Min, or Max.
        var points = Stops
            .Select(s => new { Value = GetControlPointValue(s), s.Color })
            .OrderBy(p => p.Value)
            .ToList();

        if (points.Count == 0) {
            return Color.Gray;
        }

        // Below first point
        if (value <= points[0].Value) {
            return points[0].Color;
        }

        // Above last point
        if (value >= points[^1].Value) {
            return points[^1].Color;
        }

        // Find two adjacent points to interpolate between
        for (int i = 0; i < points.Count - 1; i++) {
            if (value >= points[i].Value && value <= points[i + 1].Value) {
                var range = points[i + 1].Value - points[i].Value;
                if (range <= 0) {
                    return points[i].Color;
                }

                var ratio = (value - points[i].Value) / range;
                return InterpolateColor(points[i].Color, points[i + 1].Color, ratio);
            }
        }

        return points[^1].Color;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Gets a single representative value for a color stop (used as control point in gradient mode).
    /// </summary>
    private static double GetControlPointValue(ColorStop stop) {
        if (stop.MinValue.HasValue && stop.MaxValue.HasValue) {
            return (stop.MinValue.Value + stop.MaxValue.Value) / 2.0;
        }

        if (stop.MinValue.HasValue) {
            return stop.MinValue.Value;
        }

        if (stop.MaxValue.HasValue) {
            return stop.MaxValue.Value;
        }

        return 0;
    }

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
    public double? MaxValue { get; set; }
    public double? MinValue { get; set; }

    #endregion Public Properties
}
