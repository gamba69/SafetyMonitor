namespace SafetyMonitor.Models;

/// <summary>
/// Variable font render options for icon generation.
/// </summary>
public sealed class IconRenderOptions {
    public float GlyphScale { get; init; } = 0.78f;

    /// <summary>
    /// Named variable font axes (e.g. FILL, GRAD, opsz, wght, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, float> Axes { get; init; } = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
}
