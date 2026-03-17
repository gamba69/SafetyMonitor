using SafetyMonitor.Models;
using SkiaSharp;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents material icons and encapsulates its related behavior and state.
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
    public const string Refresh = "refresh";
    public const string TrayPip = "pip";
    public const string RuleSettings = "rule_settings";
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
        [Refresh] = "\uE5D5",
        [TrayPip] = "\uF64D",
        [RuleSettings] = "\uF64C",
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
        ["keyboard_double_arrow_left"] = "\uEAC3",
        ["keyboard_double_arrow_right"] = "\uEAC9",
        ["output_circle"] = "\uF70E",
        ["input_circle"] = "\uF71A",
        ["dangerous"] = "\uE99A",
        ["star"] = "\uE838",
        ["star_outline"] = "\uE83A",
        ["open_in_full"] = "\uF1CE",
        ["pause"] = "\uE034",
        ["play_arrow"] = "\uE037",
        ["smooth"] = "\uE155",
    };

    private static readonly HashSet<string> _filledIconNames = [
        MessageBoxInfoFilled,
        MessageBoxWarningFilled,
        MessageBoxErrorFilled,
        MessageBoxQuestionFilled,
        "star",
    ];

    private static readonly string? _materialFontPath = ResolveMaterialFontPath();
    private static readonly SKTypeface? _defaultTypeface = ResolveTypeface();

    #endregion Private Fields

    #region Public Methods

    public static void ClearCache() => HeavyRenderCache.Clear();

    public static IEnumerable<string> GetAvailableIcons() => _fontGlyphs.Keys;

    /// <summary>
    /// Gets the icon for material icons.
    /// </summary>
    /// <param name="name">Input value for name.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="preset">Input value for preset.</param>
    /// <returns>The result of the operation.</returns>
    public static Bitmap? GetIcon(string name, Color color, int size, IconRenderPreset preset) {
        var presetOptions = IconRenderPresetService.Get(preset);
        return GetIcon(name, color, size, presetOptions);
    }

    /// <summary>
    /// Gets the icon for material icons.
    /// </summary>
    /// <param name="name">Input value for name.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="options">Input value for options.</param>
    /// <returns>The result of the operation.</returns>
    public static Bitmap? GetIcon(string name, Color color, int size, IconRenderOptions options) {
        var normalizedName = name.ToLowerInvariant();
        var cacheKey = BuildCacheKey(normalizedName, color, size, options);

        var cached = HeavyRenderCache.GetBitmap(cacheKey);
        if (cached is not null) {
            return cached;
        }

        if (_defaultTypeface is null) {
            return null;
        }

        if (!_fontGlyphs.TryGetValue(normalizedName, out var glyph)) {
            glyph = normalizedName;
        }

        var bitmap = RenderFontIcon(glyph, color, size, options);
        if (bitmap is null) {
            return null;
        }

        HeavyRenderCache.PutBitmap(cacheKey, bitmap);
        return bitmap;
    }

    /// <summary>
    /// Gets the message box icon for material icons.
    /// </summary>
    /// <param name="icon">Input value for icon.</param>
    /// <param name="isLightTheme">Input value for is light theme.</param>
    /// <param name="size">Input value for size.</param>
    /// <returns>The result of the operation.</returns>
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

        var preset = IconRenderPresetService.ResolveThemePreset(isLightTheme, _filledIconNames.Contains(iconName));
        return GetIcon(iconName, color, size, preset);
    }

    /// <summary>
    /// Gets the metric icon name for material icons.
    /// </summary>
    /// <param name="metric">Input value for metric.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetMetricIconName(MetricType metric) => metric switch {
        MetricType.Temperature => MetricTemperature,
        MetricType.Apparent => MetricTemperature,
        MetricType.Humidity => MetricHumidity,
        MetricType.Pressure => MetricPressure,
        MetricType.DewPoint => MetricDewPoint,
        MetricType.CloudCover => MetricCloudCover,
        MetricType.SkyTemperature => MetricSkyTemperature,
        MetricType.SkyBrightness => MetricSkyBrightness,
        MetricType.SkyQualitySQM => MetricSkyQuality,
        MetricType.SkyQualityNELM => MetricSkyQuality,
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

    /// <summary>
    /// Renders the font icon for material icons.
    /// </summary>
    /// <param name="glyph">Input value for glyph.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="options">Input value for options.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap? RenderFontIcon(string glyph, Color color, int size, IconRenderOptions options) {
        if (_defaultTypeface is null) {
            return null;
        }

        var isFilled = options.Axes.TryGetValue("FILL", out var fill) && fill > 0.5f;
        if (isFilled && glyph == "\uE838") {
            return RenderFilledStarWithSkia(color, size);
        }

        return RenderWithSkiaPath(glyph, color, size, options, _defaultTypeface);
    }

    /// <summary>
    /// Renders the filled star with skia for material icons.
    /// </summary>
    /// <param name="color">Input value for color.</param>
    /// <param name="size">Input value for size.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap RenderFilledStarWithSkia(Color color, int size) {
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var center = size / 2f;
        var outerRadius = size * 0.38f;
        var innerRadius = size * 0.16f;

        using var path = new SKPath();
        for (int i = 0; i < 10; i++) {
            var angle = (-90f + i * 36f) * (float)Math.PI / 180f;
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            var x = center + MathF.Cos(angle) * radius;
            var y = center + MathF.Sin(angle) * radius;
            if (i == 0) {
                path.MoveTo(x, y);
            } else {
                path.LineTo(x, y);
            }
        }

        path.Close();

        using var paint = new SKPaint {
            IsAntialias = true,
            Color = new SKColor(color.R, color.G, color.B, color.A),
            Style = SKPaintStyle.Fill,
        };

        canvas.DrawPath(path, paint);

        using var image = surface.Snapshot();
        using var skBitmap = SKBitmap.FromImage(image);

        return ToBitmap(skBitmap);
    }

    /// <summary>
    /// Renders the with skia path for material icons.
    /// </summary>
    /// <param name="glyph">Input value for glyph.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="options">Input value for options.</param>
    /// <param name="typeface">Input value for typeface.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap RenderWithSkiaPath(string glyph, Color color, int size, IconRenderOptions options, SKTypeface typeface) {
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var textSize = size * Math.Clamp(options.GlyphScale, 0.1f, 1.5f);
        using var font = CreateVariableSkFont(typeface, textSize, options.Axes);
        using var paint = new SKPaint {
            IsAntialias = true,
            Color = new SKColor(color.R, color.G, color.B, color.A),
            Style = SKPaintStyle.Fill,
        };

        var glyphId = font.GetGlyphs(glyph)[0];
        using var glyphPath = font.GetGlyphPath(glyphId);
        if (glyphPath is null) {
            canvas.DrawText(glyph, size / 2f, size / 2f, SKTextAlign.Center, font, paint);
        } else {
            var bounds = glyphPath.Bounds;
            var tx = (size - bounds.Width) / 2f - bounds.Left;
            var ty = (size - bounds.Height) / 2f - bounds.Top;
            glyphPath.Transform(SKMatrix.CreateTranslation(tx, ty));
            canvas.DrawPath(glyphPath, paint);
        }

        using var image = surface.Snapshot();
        using var skBitmap = SKBitmap.FromImage(image);

        return ToBitmap(skBitmap);
    }

    /// <summary>
    /// Executes to bitmap as part of material icons processing.
    /// </summary>
    /// <param name="source">Input value for source.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap ToBitmap(SKBitmap source) {
        using var image = SKImage.FromBitmap(source);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());

        return new Bitmap(stream);
    }

    /// <summary>
    /// Resolves the typeface for material icons.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static SKTypeface? ResolveTypeface() {
        if (_materialFontPath is null) {
            return null;
        }

        return SKTypeface.FromFile(_materialFontPath);
    }

    /// <summary>
    /// Creates the variable sk font for material icons.
    /// </summary>
    /// <param name="typeface">Input value for typeface.</param>
    /// <param name="textSize">Input value for text size.</param>
    /// <param name="axes">Input value for axes.</param>
    /// <returns>The result of the operation.</returns>
    private static SKFont CreateVariableSkFont(SKTypeface typeface, float textSize, IReadOnlyDictionary<string, float> axes) {
        _ = axes;

        return new SKFont(typeface, textSize, 1f, 0f) {
            Edging = SKFontEdging.Antialias,
            Subpixel = true,
        };
    }

    /// <summary>
    /// Builds the cache key for material icons.
    /// </summary>
    /// <param name="normalizedName">Input value for normalized name.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="options">Input value for options.</param>
    /// <returns>The resulting string value.</returns>
    private static string BuildCacheKey(string normalizedName, Color color, int size, IconRenderOptions options) {
        var axisToken = options.Axes.Count == 0
            ? "none"
            : string.Join(",", options.Axes.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase).Select(static x => $"{x.Key}:{x.Value:F2}"));

        return $"icon::{normalizedName}::{size}::{options.GlyphScale:F2}::{color.ToArgb()}::{axisToken}";
    }

    /// <summary>
    /// Resolves the material font path for material icons.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    private static string? ResolveMaterialFontPath() {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf");
        return File.Exists(fontPath) ? fontPath : null;
    }

    #endregion Private Methods
}
