using SafetyMonitor.Models;

namespace SafetyMonitor.Services;

public enum IconRenderPreset {
    LightOutlined,
    LightFilled,
    DarkOutlined,
    DarkFilled,
}

/// <summary>
/// Represents icon render preset service and encapsulates its related behavior and state.
/// </summary>
internal static class IconRenderPresetService {
    private static readonly Dictionary<IconRenderPreset, IconRenderOptions> _presets =
        new() {
            [IconRenderPreset.LightOutlined] = new() {
                GlyphScale = 0.78f,
                Axes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) {
                    ["FILL"] = 0f,
                    ["GRAD"] = 0f,
                    ["opsz"] = 24f,
                    ["wght"] = 400f,
                },
            },
            [IconRenderPreset.LightFilled] = new() {
                GlyphScale = 0.78f,
                Axes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) {
                    ["FILL"] = 1f,
                    ["GRAD"] = 0f,
                    ["opsz"] = 24f,
                    ["wght"] = 500f,
                },
            },
            [IconRenderPreset.DarkOutlined] = new() {
                GlyphScale = 0.78f,
                Axes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) {
                    ["FILL"] = 0f,
                    ["GRAD"] = -25f,
                    ["opsz"] = 24f,
                    ["wght"] = 400f,
                },
            },
            [IconRenderPreset.DarkFilled] = new() {
                GlyphScale = 0.78f,
                Axes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) {
                    ["FILL"] = 1f,
                    ["GRAD"] = -25f,
                    ["opsz"] = 24f,
                    ["wght"] = 500f,
                },
            },
        };

    public static IconRenderOptions Get(IconRenderPreset preset) => _presets[preset];

    /// <summary>
    /// Resolves the theme preset for icon render preset service.
    /// </summary>
    /// <param name="isLightTheme">Input value for is light theme.</param>
    /// <param name="filled)">Input value for filled.</param>
    /// <returns>The result of the operation.</returns>
    public static IconRenderPreset ResolveThemePreset(bool isLightTheme, bool filled) => (isLightTheme, filled) switch {
        (true, true) => IconRenderPreset.LightFilled,
        (true, false) => IconRenderPreset.LightOutlined,
        (false, true) => IconRenderPreset.DarkFilled,
        _ => IconRenderPreset.DarkOutlined,
    };
}
