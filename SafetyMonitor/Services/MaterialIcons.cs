using SafetyMonitor.Models;
using SkiaSharp;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace SafetyMonitor.Services;

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
    public const string DashboardManage = "dashboard_manage";
    public const string LinkedServices = "linked_services";
    public const string MenuViewAxisRules = "menu_view_axis_rules";
    public const string MenuViewMetricSettings = "menu_view_metric_settings";
    public const string MenuViewChartPeriods = "menu_view_chart_periods";
    public const string MenuViewColorSchemes = "menu_view_color_schemes";
    public const string MenuViewValueSchemes = "menu_view_value_schemes";
    public const string ToolbarChartsLink = "toolbar_charts_link";
    public const string ToolbarChartsGroup = "toolbar_charts_group";
    public const string ToolbarChartsUnlink = "toolbar_charts_unlink";
    public const string ChartModeAuto = "chart_mode_auto";
    public const string ChartModeStatic = "chart_mode_static";
    public const string ChartInspector = "chart_inspector";
    public const string PlotMenuSaveImage = "plot_menu_save_image";
    public const string PlotMenuCopyToClipboard = "plot_menu_copy_to_clipboard";
    public const string PlotMenuAutoscale = "plot_menu_autoscale";
    public const string PlotMenuOpenInWindow = "plot_menu_open_in_window";
    public const string PlotMenuImage = "plot_menu_image";
    public const string PlotMenuTable = "plot_menu_table";
    public const string ValueMenuNorthEast = "value_menu_north_east";
    public const string ValueMenuSouthEast = "value_menu_south_east";
    public const string ValueDisplay123 = "value_display_123";
    public const string ValueDisplayAbc = "value_display_abc";
    public const string ValueDisplayShortText = "value_display_short_text";
    public const string PlotMenuDisplayOption = "plot_menu_display_option";
    public const string PlotMenuConversionPath = "gesture";
    public const string PlotMenuGrid4x4 = "grid_4x4";
    public const string PlotMenuLegendToggle = "legend_toggle";
    public const string PlotMenuStat0 = "stat_0";
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
    public const string SortAscending = "sort_ascending";
    public const string SortDescending = "sort_descending";
    public const string Sort = "sort";
    public const string ColorModeSolid = "color_mode_solid";
    public const string ColorModeGradient = "color_mode_gradient";
    public const string CommonBrowse = "common_browse";
    public const string CommonResize = "common_resize";
    public const string CommonTest = "common_test";
    public const string CommonCalculate = "common_calculate";
    public const string CommonBuild = "common_build";
    public const string CommonDatabase = "common_database";
    public const string CommonAvgTime = "common_avg_time";
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
    public const string WindowTileValue = "window_tile_value";
    public const string WindowTileChart = "window_tile_chart";
    public const string ToolbarExportProgress = "toolbar_export_progress";
    public const string RefreshCircle = "refresh_circle";
    public const string RefreshLoader10 = "refresh_loader_10";
    public const string RefreshLoader20 = "refresh_loader_20";
    public const string RefreshLoader40 = "refresh_loader_40";
    public const string RefreshLoader60 = "refresh_loader_60";
    public const string RefreshLoader80 = "refresh_loader_80";
    public const string RefreshLoader90 = "refresh_loader_90";
    public const string RefreshHourglass = "refresh_hourglass";
    public const string LinkGroupAlpha = "link_group_alpha";
    public const string LinkGroupBravo = "link_group_bravo";
    public const string LinkGroupCharlie = "link_group_charlie";
    public const string LinkGroupDelta = "link_group_delta";
    public const string LinkGroupEcho = "link_group_echo";
    public const string LinkGroupFoxtrot = "link_group_foxtrot";

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
        [DashboardManage] = "\uE871",
        [LinkedServices] = "\uF535",
        [MenuViewTheme] = "\uE6A2", // "\uE3A1",
        [MenuViewColorSchemes] = "\uE40A",
        [MenuViewValueSchemes] = "\uE262",
        [ThemeLightMode] = "\uF157",
        [ThemeDarkMode] = "\uF03D",
        [MenuHelpAbout] = "\uE88E",
        [MessageBoxInfoOutlined] = "\uE88E",
        [MessageBoxInfoFilled] = "\uE88E",
        [MessageBoxWarningOutlined] = "\uE002",
        [MessageBoxWarningFilled] = "\uE002",
        [MessageBoxErrorOutlined] = "\uE001",
        [MessageBoxErrorFilled] = "\uE000",
        [MessageBoxQuestionOutlined] = "\uE887",
        [MessageBoxQuestionFilled] = "\uE887",
        [MenuViewChartPeriods] = "\uF417",
        [PlotMenuAutoscale] = "\uF417",
        [PlotMenuSaveImage] = "\uF17F", // "\uE161",
        [PlotMenuImage] = "\uF3BC", //"\uE3F4",
        [PlotMenuTable] = "\uE6CF", //"\uF191",
        [PlotMenuOpenInWindow] = "\uE89E", // "\uE2C7",
        [ValueMenuNorthEast] = "\uF1E1",
        [ValueMenuSouthEast] = "\uF1E4",
        [ValueDisplay123] = "\uEB8D",
        [ValueDisplayAbc] = "\uEB94",
        [ValueDisplayShortText] = "\uE261",
        [MenuViewAxisRules] = "\uEA9A",
        [MenuViewMetricSettings] = "\uEA49",
        [PlotMenuDisplayOption] = "\uE6E1",
        [PlotMenuConversionPath] = "\uE155",
        [PlotMenuGrid4x4] = "\uF016",
        [PlotMenuLegendToggle] = "\uE267",
        [PlotMenuStat0] = "\uE697",
        [CommonCheck] = "\uE5CA",
        [CommonClose] = "\uE5CD",
        [CommonSave] = "\uE5CA", // "\uE161",
        [CommonDelete] = "\uE872",
        [CommonAdd] = "\uE145",
        [CommonEdit] = "\uE3C9",
        [CommonDuplicate] = "\uE14D",
        [CommonMoveUp] = "\uE5D8",
        [CommonMoveDown] = "\uE5DB",
        [SortAscending] = "\uE5D8",
        [SortDescending] = "\uE5DB",
        [Sort] = "\uE164",
        [ColorModeSolid] = "\uE40A",
        [ColorModeGradient] = "\uE3E9",
        [CommonBrowse] = "\uE2C8",
        [CommonResize] = "\uE8CE",
        [CommonTest] = "\uE86C",
        [CommonCalculate] = "\uEA5F",
        [CommonBuild] = "\uE869",
        [CommonDatabase] = "\uF20E",
        [CommonAvgTime] = "\uF813",
        [DashboardTab] = "\uE871",
        [ToolbarChartsLink] = "\uE157",
        [ToolbarChartsGroup] = "\uF8EF",
        [ToolbarChartsUnlink] = "\uE16F",
        [ChartModeStatic] = "\uF71E",
        [ChartModeAuto] = "\uF417",
        [ChartInspector] = "\uE1D2",
        [MetricTemperature] = "\uF076",
        [MetricHumidity] = "\uE798",
        [MetricPressure] = "\uE69f",
        [MetricDewPoint] = "\uF879",
        [MetricCloudCover] = "\uF174",
        [MetricSkyTemperature] = "\uEB5A",
        [MetricSkyBrightness] = "\uE3A9",
        [MetricSkyQuality] = "\uF34f",
        [MetricRainRate] = "\uF176",
        [MetricWindSpeed] = "\uEFD8",
        [MetricWindGust] = "\uE915",
        [MetricWindDirection] = "\uE989", // "\uE87A",
        [MetricStarFwhm] = "\uF31C",
        [MetricIsSafe] = "\uEAA9",
        [WindowTileValue] = "\uE400",
        [WindowTileChart] = "\uE667", //"\uE6E1",
        [ToolbarExportProgress] = "\uF398",
        [RefreshCircle] = "\uEF4A",
        [RefreshLoader10] = "\uF726",
        [RefreshLoader20] = "\uF725",
        [RefreshLoader40] = "\uF724",
        [RefreshLoader60] = "\uF723",
        [RefreshLoader80] = "\uF722",
        [RefreshLoader90] = "\uF721",
        [RefreshHourglass] = "\uEBFF",
        [LinkGroupAlpha] = "\uF784",
        [LinkGroupBravo] = "\uF783",
        [LinkGroupCharlie] = "\uF782",
        [LinkGroupDelta] = "\uF781",
        [LinkGroupEcho] = "\uF780",
        [LinkGroupFoxtrot] = "\uF77F",
        ["keyboard_double_arrow_up"] = "\uEACF",
        ["keyboard_double_arrow_down"] = "\uEAD0",
        ["output_circle"] = "\uF70E",
        ["input_circle"] = "\uF71A",
        ["dangerous"] = "\uE99A",
        ["star"] = "\uE838",
        ["star_outline"] = "\uE838",
        ["open_in_full"] = "\uF1CE",
        ["pause"] = "\uE034",
        ["smooth"] = "\uE155",
    };

    private static readonly HashSet<string> _filledIconNames = [
        MessageBoxInfoFilled,
        MessageBoxWarningFilled,
        MessageBoxErrorFilled,
        MessageBoxQuestionFilled,
        "star",
    ];

    private static readonly string[] _fontCandidates = [
        "Material Symbols Outlined",
        "Material Symbols Rounded",
        "Material Icons",
    ];

    private static readonly string? _materialFontFamily = ResolveInstalledMaterialFont();
    private static readonly string? _materialFontPath = ResolveMaterialFontPath();
    private static readonly SKTypeface? _outlinedTypeface = ResolveTypeface(0f);
    private static readonly SKTypeface? _filledTypeface = ResolveTypeface(1f);

    #endregion Private Fields

    #region Public Methods

    public static void ClearCache() {
        foreach (var bitmap in _cache.Values) {
            bitmap.Dispose();
        }

        _cache.Clear();
    }

    public static IEnumerable<string> GetAvailableIcons() => _fontGlyphs.Keys;

    public static Bitmap? GetIcon(string name, Color color, int size = 16, float glyphScale = 0.78f) {
        var normalizedName = name.ToLowerInvariant();
        var key = $"{normalizedName}_{size}_{glyphScale:F2}_{color.ToArgb()}";

        if (_cache.TryGetValue(key, out var cached)) {
            return (Bitmap)cached.Clone();
        }

        if (_outlinedTypeface is null && _materialFontFamily is null) {
            return null;
        }

        if (!_fontGlyphs.TryGetValue(normalizedName, out var glyph)) {
            return null;
        }

        var isFilled = _filledIconNames.Contains(normalizedName);
        var bitmap = RenderFontIcon(glyph, color, size, glyphScale, isFilled);
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
            : Color.White;

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

    private static Bitmap RenderFontIcon(string glyph, Color color, int size, float glyphScale, bool isFilled) {
        var typeface = isFilled ? _filledTypeface ?? _outlinedTypeface : _outlinedTypeface;
        if (typeface is not null) {
            return RenderWithSkia(glyph, color, size, glyphScale, typeface);
        }

        return RenderWithSystemDrawing(glyph, color, size, glyphScale);
    }

    private static Bitmap RenderWithSkia(string glyph, Color color, int size, float glyphScale, SKTypeface typeface) {
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint {
            IsAntialias = true,
            Typeface = typeface,
            Color = new SKColor(color.R, color.G, color.B, color.A),
            TextSize = size * Math.Clamp(glyphScale, 0.1f, 1.5f),
            TextAlign = SKTextAlign.Center,
            IsStroke = false,
        };

        var metrics = paint.FontMetrics;
        var textY = (size / 2f) - ((metrics.Ascent + metrics.Descent) / 2f);
        canvas.DrawText(glyph, size / 2f, textY, paint);

        using var image = surface.Snapshot();
        using var skBitmap = SKBitmap.FromImage(image);

        return ToBitmap(skBitmap);
    }

    private static Bitmap RenderWithSystemDrawing(string glyph, Color color, int size, float glyphScale) {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
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

        var fontSize = size * Math.Clamp(glyphScale, 0.1f, 1.5f);
        using var font = new Font(_materialFontFamily!, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        g.DrawString(glyph, font, brush, new RectangleF(0, 0, size, size), sf);

        return bitmap;
    }

    private static Bitmap ToBitmap(SKBitmap source) {
        using var image = SKImage.FromBitmap(source);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());

        return new Bitmap(stream);
    }

    private static SKTypeface? ResolveTypeface(float fillValue) {
        if (_materialFontPath is null) {
            return null;
        }

        var baseTypeface = SKTypeface.FromFile(_materialFontPath);
        if (baseTypeface is null) {
            return null;
        }

        var fontArguments = BuildFontArgumentsObject(fillValue);
        if (fontArguments is null) {
            return baseTypeface;
        }

        try {
            var withFontArgumentsMethod = typeof(SKTypeface).GetMethod("WithFontArguments");
            if (withFontArgumentsMethod?.Invoke(baseTypeface, [fontArguments]) is SKTypeface variedTypeface) {
                return variedTypeface;
            }
        }
        catch {
            // Ignore and use base typeface.
        }

        return baseTypeface;
    }

    private static object? BuildFontArgumentsObject(float fillValue) {
        try {
            var argumentsType = typeof(SKTypeface).Assembly.GetType("SkiaSharp.SKFontArguments");
            if (argumentsType is null) {
                return null;
            }

            var arguments = Activator.CreateInstance(argumentsType);
            if (arguments is null) {
                return null;
            }

            var variationPositionType = argumentsType.GetNestedType("VariationPosition");
            var coordinateType = variationPositionType?.GetNestedType("Coordinate");
            if (variationPositionType is null || coordinateType is null) {
                return null;
            }

            var coordinates = Array.CreateInstance(coordinateType, 4);
            coordinates.SetValue(Activator.CreateInstance(coordinateType, MakeAxisTag('F', 'I', 'L', 'L'), fillValue), 0);
            coordinates.SetValue(Activator.CreateInstance(coordinateType, MakeAxisTag('w', 'g', 'h', 't'), 400f), 1);
            coordinates.SetValue(Activator.CreateInstance(coordinateType, MakeAxisTag('o', 'p', 's', 'z'), 24f), 2);
            coordinates.SetValue(Activator.CreateInstance(coordinateType, MakeAxisTag('G', 'R', 'A', 'D'), 0f), 3);

            var variationPosition = Activator.CreateInstance(variationPositionType, coordinates);
            if (variationPosition is null) {
                return null;
            }

            var setVariationMethod = argumentsType.GetMethod("SetVariationDesignPosition", [variationPositionType]);
            setVariationMethod?.Invoke(arguments, [variationPosition]);

            return arguments;
        }
        catch {
            return null;
        }
    }

    private static uint MakeAxisTag(char c1, char c2, char c3, char c4) {
        return ((uint)c1 << 24) | ((uint)c2 << 16) | ((uint)c3 << 8) | c4;
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

    private static string? ResolveMaterialFontPath() {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf");
        return File.Exists(fontPath) ? fontPath : null;
    }

    #endregion Private Methods
}
