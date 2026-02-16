using SafetyMonitorView.Models;
using System.Drawing.Text;

namespace SafetyMonitorView.Services;

/// <summary>
/// Material Design icons rendered from installed Material font families.
/// </summary>
public static class MaterialIcons {

    #region Public Constants

    public const string MenuFileSettings = "menu_file_settings";
    public const string MenuFileExitApp = "menu_file_exit_app";
    public const string MenuViewTheme = "menu_view_theme";
    public const string ThemeLightMode = "theme_light_mode";
    public const string ThemeDarkMode = "theme_dark_mode";
    public const string MenuHelpAbout = "menu_help_about";
    public const string DashboardCreateNew = "dashboard_create_new";
    public const string DashboardEditCurrent = "dashboard_edit_current";
    public const string DashboardDuplicateCurrent = "dashboard_duplicate_current";
    public const string DashboardDeleteCurrent = "dashboard_delete_current";
    public const string MenuViewAxisRules = "menu_view_axis_rules";
    public const string MenuViewChartPeriods = "menu_view_chart_periods";
    public const string MenuViewColorSchemes = "menu_view_color_schemes";
    public const string ToolbarChartsLink = "toolbar_charts_link";
    public const string ToolbarChartsUnlink = "toolbar_charts_unlink";
    public const string ChartModeAuto = "chart_mode_auto";
    public const string ChartModeStatic = "chart_mode_static";
    public const string PlotMenuSaveImage = "plot_menu_save_image";
    public const string PlotMenuCopyToClipboard = "plot_menu_copy_to_clipboard";
    public const string PlotMenuAutoscale = "plot_menu_autoscale";
    public const string PlotMenuOpenInWindow = "plot_menu_open_in_window";
    public const string PlotMenuDisplayOption = "plot_menu_display_option";
    public const string MessageBoxInfoOutlined = "msg_info_outlined";
    public const string MessageBoxInfoFilled = "msg_info_filled";
    public const string MessageBoxWarningOutlined = "msg_warning_outlined";
    public const string MessageBoxWarningFilled = "msg_warning_filled";
    public const string MessageBoxErrorOutlined = "msg_error_outlined";
    public const string MessageBoxErrorFilled = "msg_error_filled";
    public const string MessageBoxQuestionOutlined = "msg_question_outlined";
    public const string MessageBoxQuestionFilled = "msg_question_filled";
    public const string CommonCheck = "common_check";
    public const string CommonClose = "common_close";
    public const string CommonSave = "common_save";
    public const string CommonDelete = "common_delete";
    public const string CommonAdd = "common_add";
    public const string CommonEdit = "common_edit";
    public const string CommonDuplicate = "common_duplicate";
    public const string CommonMoveUp = "common_move_up";
    public const string CommonMoveDown = "common_move_down";
    public const string CommonBrowse = "common_browse";
    public const string CommonResize = "common_resize";
    public const string CommonTest = "common_test";
    public const string DashboardTab = "dashboard_tab";
    public const string MetricTemperature = "metric_temperature";
    public const string MetricHumidity = "metric_humidity";
    public const string MetricPressure = "metric_pressure";
    public const string MetricDewPoint = "metric_dew_point";
    public const string MetricCloudCover = "metric_cloud_cover";
    public const string MetricSkyTemperature = "metric_sky_temperature";
    public const string MetricSkyBrightness = "metric_sky_brightness";
    public const string MetricSkyQuality = "metric_sky_quality";
    public const string MetricRainRate = "metric_rain_rate";
    public const string MetricWindSpeed = "metric_wind_speed";
    public const string MetricWindGust = "metric_wind_gust";
    public const string MetricWindDirection = "metric_wind_direction";
    public const string MetricStarFwhm = "metric_star_fwhm";
    public const string MetricIsSafe = "metric_is_safe";

    #endregion Public Constants

    #region Private Fields

    private static readonly Dictionary<string, Bitmap> _cache = [];

    private static readonly Dictionary<string, string> _fontGlyphs = new() {
        [MenuFileSettings] = "\uE8B8",
        [MenuFileExitApp] = "\uE879",
        [DashboardCreateNew] = "\uE145",
        [DashboardEditCurrent] = "\uE3C9",
        [DashboardDuplicateCurrent] = "\uE14D",
        [PlotMenuCopyToClipboard] = "\uE14D",
        [DashboardDeleteCurrent] = "\uE872",
        [MenuViewTheme] = "\uE3A1",
        [MenuViewColorSchemes] = "\uE40A",
        [ThemeLightMode] = "\uE430",
        [ThemeDarkMode] = "\uE51C",
        [MenuHelpAbout] = "\uE88E",
        [MessageBoxInfoOutlined] = "\uE88F",
        [MessageBoxInfoFilled] = "\uE88E",
        [MessageBoxWarningOutlined] = "\uE002",
        [MessageBoxWarningFilled] = "\uE002",
        [MessageBoxErrorOutlined] = "\uE001",
        [MessageBoxErrorFilled] = "\uE000",
        [MessageBoxQuestionOutlined] = "\uE8FD",
        [MessageBoxQuestionFilled] = "\uE887",
        [MenuViewChartPeriods] = "\uE8B5",
        [PlotMenuAutoscale] = "\uE5D5",
        [PlotMenuSaveImage] = "\uE161",
        [PlotMenuOpenInWindow] = "\uE2C7",
        [MenuViewAxisRules] = "\uE6E1",
        [PlotMenuDisplayOption] = "\uE6E1",
        [CommonCheck] = "\uE5CA",
        [CommonClose] = "\uE5CD",
        [CommonSave] = "\uE161",
        [CommonDelete] = "\uE872",
        [CommonAdd] = "\uE145",
        [CommonEdit] = "\uE3C9",
        [CommonDuplicate] = "\uE14D",
        [CommonMoveUp] = "\uE5D8",
        [CommonMoveDown] = "\uE5DB",
        [CommonBrowse] = "\uE2C8",
        [CommonResize] = "\uE8CE",
        [CommonTest] = "\uE86C",
        [DashboardTab] = "\uE871",
        [ToolbarChartsLink] = "\uE157",
        [ToolbarChartsUnlink] = "\uE16F",
        [ChartModeStatic] = "\uE925",
        [ChartModeAuto] = "\uE8B5",
        [MetricTemperature] = "\uE1FF",
        [MetricHumidity] = "\uE798",
        [MetricPressure] = "\uE9E4",
        [MetricDewPoint] = "\uE818",
        [MetricCloudCover] = "\uE2BD",
        [MetricSkyTemperature] = "\uEB3B",
        [MetricSkyBrightness] = "\uE430",
        [MetricSkyQuality] = "\uE838",
        [MetricRainRate] = "\uE3AA",
        [MetricWindSpeed] = "\uE9C4",
        [MetricWindGust] = "\uE9C4",
        [MetricWindDirection] = "\uE87A",
        [MetricStarFwhm] = "\uE2DB",
        [MetricIsSafe] = "\uE9E0",
    };

    private static readonly string[] _fontCandidates = [
        "Material Symbols Outlined",
        "Material Symbols Rounded",
        "Material Icons",
    ];

    private static readonly string? _materialFontFamily = ResolveInstalledMaterialFont();

    #endregion Private Fields

    #region Public Methods

    public static void ClearCache() {
        foreach (var bitmap in _cache.Values) {
            bitmap.Dispose();
        }

        _cache.Clear();
    }

    public static IEnumerable<string> GetAvailableIcons() => _fontGlyphs.Keys;

    public static Bitmap? GetIcon(string name, Color color, int size = 16) {
        var normalizedName = name.ToLowerInvariant();
        var key = $"{normalizedName}_{size}_{color.ToArgb()}";

        if (_cache.TryGetValue(key, out var cached)) {
            return (Bitmap)cached.Clone();
        }

        if (_materialFontFamily is null || !_fontGlyphs.TryGetValue(normalizedName, out var glyph)) {
            return null;
        }

        var bitmap = RenderFontIcon(glyph, color, size);
        _cache[key] = bitmap;

        return (Bitmap)bitmap.Clone();
    }

    public static Bitmap? GetMessageBoxIcon(MessageBoxIcon icon, bool isLightTheme, int size = 28) {
        if (icon == MessageBoxIcon.None) {
            return null;
        }

        var iconName = icon switch {
            MessageBoxIcon.Error => isLightTheme ? MessageBoxErrorOutlined : MessageBoxErrorFilled,
            MessageBoxIcon.Warning => isLightTheme ? MessageBoxWarningOutlined : MessageBoxWarningFilled,
            MessageBoxIcon.Information => isLightTheme ? MessageBoxInfoOutlined : MessageBoxInfoFilled,
            MessageBoxIcon.Question => isLightTheme ? MessageBoxQuestionOutlined : MessageBoxQuestionFilled,
            _ => isLightTheme ? MessageBoxInfoOutlined : MessageBoxInfoFilled
        };

        var color = isLightTheme
            ? Color.FromArgb(48, 48, 48)
            : Color.FromArgb(176, 176, 176);

        return GetIcon(iconName, color, size);
    }

    public static string GetMetricIconName(MetricType metric) => metric switch {
        MetricType.Temperature => MetricTemperature,
        MetricType.Humidity => MetricHumidity,
        MetricType.Pressure => MetricPressure,
        MetricType.DewPoint => MetricDewPoint,
        MetricType.CloudCover => MetricCloudCover,
        MetricType.SkyTemperature => MetricSkyTemperature,
        MetricType.SkyBrightness => MetricSkyBrightness,
        MetricType.SkyQuality => MetricSkyQuality,
        MetricType.RainRate => MetricRainRate,
        MetricType.WindSpeed => MetricWindSpeed,
        MetricType.WindGust => MetricWindGust,
        MetricType.WindDirection => MetricWindDirection,
        MetricType.StarFwhm => MetricStarFwhm,
        MetricType.IsSafe => MetricIsSafe,
        _ => MenuViewAxisRules,
    };

    #endregion Public Methods

    #region Private Methods

    private static Bitmap RenderFontIcon(string glyph, Color color, int size) {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(color);
        using var sf = new StringFormat {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoClip,
        };

        g.Clear(Color.Transparent);
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var fontSize = size * 0.78f;
        using var font = new Font(_materialFontFamily!, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        g.DrawString(glyph, font, brush, new RectangleF(0, 0, size, size), sf);

        return bitmap;
    }

    private static string? ResolveInstalledMaterialFont() {
        using var installedFonts = new InstalledFontCollection();
        var installed = installedFonts.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in _fontCandidates) {
            if (installed.Contains(candidate)) {
                return candidate;
            }
        }

        return null;
    }

    #endregion Private Methods
}
