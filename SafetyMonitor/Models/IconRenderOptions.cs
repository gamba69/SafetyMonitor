namespace SafetyMonitor.Models;

/// <summary>
/// Represents icon render options and encapsulates its related behavior and state.
/// </summary>
public sealed class IconRenderOptions {
    /// <summary>
    /// Gets or sets the glyph scale for icon render options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public float GlyphScale { get; init; } = 0.78f;

    /// <summary>
    /// Gets or sets the axes for icon render options. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public IReadOnlyDictionary<string, float> Axes { get; init; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
}
