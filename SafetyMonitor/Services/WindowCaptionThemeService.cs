using System.Runtime.InteropServices;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents window caption theme service and encapsulates its related behavior and state.
/// </summary>
internal static class WindowCaptionThemeService {
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    /// <summary>
    /// Attempts to apply win 11 theme for window caption theme service.
    /// </summary>
    /// <param name="hwnd">Input value for hwnd.</param>
    /// <param name="captionColor">Input value for caption color.</param>
    /// <param name="isDarkTheme">Input value for is dark theme.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    public static bool TryApplyWin11Theme(IntPtr hwnd, Color captionColor, bool isDarkTheme) {
        if (!OperatingSystem.IsWindows()) {
            return false;
        }

        var windowsVersion = Environment.OSVersion.Version;
        var isWin11OrHigher = windowsVersion.Major >= 10 && windowsVersion.Build >= 22000;
        if (!isWin11OrHigher) {
            return false;
        }

        var captionColorRef = ToColorRef(captionColor);
        var setCaptionColorResult = DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref captionColorRef, sizeof(int));

        var textColorRef = ToColorRef(isDarkTheme ? Color.White : Color.Black);
        var setTextColorResult = DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref textColorRef, sizeof(int));

        return setCaptionColorResult == 0 && setTextColorResult == 0;
    }

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
