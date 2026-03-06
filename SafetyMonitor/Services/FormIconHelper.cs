using SafetyMonitor.Models;
using System.Runtime.InteropServices;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents form icon helper and encapsulates its related behavior and state.
/// </summary>
internal static class FormIconHelper {

    #region Private Constants

    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int SmCxIcon = 11;
    private const int SmCyIcon = 12;
    private const int SmCxSmIcon = 49;
    private const int SmCySmIcon = 50;
    private const int WmSetIcon = 0x0080;
    private const float WindowIconGlyphScale = 1.08f;

    #endregion Private Constants

    #region Public Methods

    /// <summary>
    /// Applies the state for form icon helper.
    /// </summary>
    /// <param name="form">Input value for form.</param>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="color">Input value for color.</param>
    public static void Apply(Form form, string iconName, Color? color = null) {
        var resolvedColor = color ?? ResolveThemeIconColor();

        if (form.IsHandleCreated) {
            ApplyForCurrentDpi(form, iconName, resolvedColor);
        } else {
            form.HandleCreated += (_, _) => ApplyForCurrentDpi(form, iconName, resolvedColor);
        }
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Applies the for current dpi for form icon helper.
    /// </summary>
    /// <param name="form">Input value for form.</param>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="color">Input value for color.</param>
    private static void ApplyForCurrentDpi(Form form, string iconName, Color color) {
        var dpi = (uint)(form.DeviceDpi > 0 ? form.DeviceDpi : 96);
        var smallSize = GetIconSizeForDpi(SmCxSmIcon, SmCySmIcon, dpi, 16);
        var largeSize = GetIconSizeForDpi(SmCxIcon, SmCyIcon, dpi, 32);

        using var smallBitmap = MaterialIcons.GetIcon(iconName, color, smallSize, new IconRenderOptions { GlyphScale = WindowIconGlyphScale, Axes = IconRenderPresetService.Get(IconRenderPreset.DarkOutlined).Axes });
        using var largeBitmap = MaterialIcons.GetIcon(iconName, color, largeSize, new IconRenderOptions { GlyphScale = WindowIconGlyphScale, Axes = IconRenderPresetService.Get(IconRenderPreset.DarkOutlined).Axes });

        if (smallBitmap is null || largeBitmap is null) {
            return;
        }

        var hSmall = smallBitmap.GetHicon();
        var hLarge = largeBitmap.GetHicon();
        try {
            SendMessage(form.Handle, WmSetIcon, (IntPtr)IconSmall, hSmall);
            SendMessage(form.Handle, WmSetIcon, (IntPtr)IconBig, hLarge);

            using var managedLarge = Icon.FromHandle(hLarge);
            form.Icon = (Icon)managedLarge.Clone();
        } finally {
            DestroyIcon(hSmall);
            DestroyIcon(hLarge);
        }
    }

    /// <summary>
    /// Gets the icon size for dpi for form icon helper.
    /// </summary>
    /// <param name="metricX">Input value for metric x.</param>
    /// <param name="metricY">Input value for metric y.</param>
    /// <param name="dpi">Input value for dpi.</param>
    /// <param name="fallback">Input value for fallback.</param>
    /// <returns>The result of the operation.</returns>
    private static int GetIconSizeForDpi(int metricX, int metricY, uint dpi, int fallback) {
        var width = GetSystemMetricsForDpi(metricX, dpi);
        var height = GetSystemMetricsForDpi(metricY, dpi);
        var size = Math.Min(width, height);
        return size > 0 ? size : fallback;
    }

    /// <summary>
    /// Resolves the theme icon color for form icon helper.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Color ResolveThemeIconColor() {
        return Color.White;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    #endregion Private Methods
}
