using System.Runtime.InteropServices;

namespace SafetyMonitor.Services;

internal static class WindowCaptionThemeService {
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

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
