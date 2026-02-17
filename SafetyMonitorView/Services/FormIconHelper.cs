using MaterialSkin;
using System.Runtime.InteropServices;

namespace SafetyMonitorView.Services;

internal static class FormIconHelper {

    #region Private Constants

    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int SmCxIcon = 11;
    private const int SmCyIcon = 12;
    private const int SmCxSmIcon = 49;
    private const int SmCySmIcon = 50;
    private const int WmSetIcon = 0x0080;

    #endregion Private Constants

    #region Public Methods

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

    private static void ApplyForCurrentDpi(Form form, string iconName, Color color) {
        var dpi = (uint)(form.DeviceDpi > 0 ? form.DeviceDpi : 96);
        var smallSize = GetIconSizeForDpi(SmCxSmIcon, SmCySmIcon, dpi, 16);
        var largeSize = GetIconSizeForDpi(SmCxIcon, SmCyIcon, dpi, 32);

        using var smallBitmap = MaterialIcons.GetIcon(iconName, color, smallSize);
        using var largeBitmap = MaterialIcons.GetIcon(iconName, color, largeSize);

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

    private static int GetIconSizeForDpi(int metricX, int metricY, uint dpi, int fallback) {
        var width = GetSystemMetricsForDpi(metricX, dpi);
        var height = GetSystemMetricsForDpi(metricY, dpi);
        var size = Math.Min(width, height);
        return size > 0 ? size : fallback;
    }

    private static Color ResolveThemeIconColor() {
        var isLightTheme = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        return isLightTheme ? Color.FromArgb(33, 33, 33) : Color.White;
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
