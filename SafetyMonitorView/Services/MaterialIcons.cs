using SafetyMonitorView.Models;
using System.Drawing.Drawing2D;

namespace SafetyMonitorView.Services;

/// <summary>
/// High-quality Material Design icons rendered via GDI+ with 2× supersampling.
/// Icons are drawn using vector graphics primitives (Bézier curves, arcs, paths)
/// at double resolution and downscaled for crisp anti-aliased output at any size.
/// </summary>
public static class MaterialIcons {

    #region Private Fields

    // Pen width constant: thin stroke relative to rect width
    private const float PW = 16f;

    // r.Width / PW  — base stroke
    private const float PW2 = 22f;

    // thinner secondary stroke
    private const float PW3 = 28f;

    // Cache for rendered icons (key: "name_size_color")
    private static readonly Dictionary<string, Bitmap> _cache = [];

    // Icon drawing delegates
    private static readonly Dictionary<string, Action<Graphics, RectangleF, Color>> _iconDrawers = new() {
        ["settings"] = DrawSettings,
        ["exit"] = DrawExit,
        ["add"] = DrawAdd,
        ["new"] = DrawAdd,
        ["edit"] = DrawEdit,
        ["copy"] = DrawCopy,
        ["duplicate"] = DrawCopy,
        ["delete"] = DrawDelete,
        ["theme"] = DrawContrast,
        ["palette"] = DrawPalette,
        ["light"] = DrawLightMode,
        ["light_mode"] = DrawLightMode,
        ["dark"] = DrawDarkMode,
        ["dark_mode"] = DrawDarkMode,
        ["info"] = DrawInfo,
        ["about"] = DrawInfo,
        ["help"] = DrawHelp,
        ["refresh"] = DrawRefresh,
        ["schedule"] = DrawSchedule,
        ["timer"] = DrawSchedule,
        ["save"] = DrawSave,
        ["folder"] = DrawFolder,
        ["chart"] = DrawChart,
        ["check"] = DrawCheck,
        ["close"] = DrawClose,
        ["dashboard"] = DrawDashboard,
        // Metric tile icons
        ["temperature"] = DrawTemperature,
        ["humidity"] = DrawHumidity,
        ["pressure"] = DrawPressure,
        ["dew_point"] = DrawDewPoint,
        ["cloud"] = DrawCloud,
        ["cloud_cover"] = DrawCloud,
        ["sky_temperature"] = DrawSkyTemperature,
        ["brightness"] = DrawBrightness,
        ["sky_brightness"] = DrawBrightness,
        ["sky_quality"] = DrawStar,
        ["star"] = DrawStar,
        ["rain"] = DrawRain,
        ["rain_rate"] = DrawRain,
        ["wind"] = DrawWind,
        ["wind_speed"] = DrawWind,
        ["wind_gust"] = DrawWindGust,
        ["compass"] = DrawCompass,
        ["wind_direction"] = DrawCompass,
        ["telescope"] = DrawTelescope,
        ["star_fwhm"] = DrawTelescope,
        ["shield"] = DrawShield,
        ["safety"] = DrawShield,
        ["is_safe"] = DrawShield,
    };

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Clears the icon cache.
    /// </summary>
    public static void ClearCache() {
        foreach (var bitmap in _cache.Values) {
            bitmap.Dispose();
        }

        _cache.Clear();
    }

    /// <summary>
    /// Gets all available icon names.
    /// </summary>
    public static IEnumerable<string> GetAvailableIcons() => _iconDrawers.Keys;

    /// <summary>
    /// Gets an icon by name, rendered at the specified size and color.
    /// </summary>
    public static Bitmap? GetIcon(string name, Color color, int size = 16) {
        var key = $"{name}_{size}_{color.ToArgb()}";

        if (_cache.TryGetValue(key, out var cached)) {
            return (Bitmap)cached.Clone();
        }

        if (!_iconDrawers.TryGetValue(name.ToLowerInvariant(), out var drawer)) {
            return null;
        }

        var bitmap = RenderIcon(drawer, color, size);
        _cache[key] = bitmap;

        return (Bitmap)bitmap.Clone();
    }

    /// <summary>
    /// Gets the icon name for a given MetricType.
    /// </summary>
    public static string GetMetricIconName(MetricType metric) => metric switch {
        MetricType.Temperature => "temperature",
        MetricType.Humidity => "humidity",
        MetricType.Pressure => "pressure",
        MetricType.DewPoint => "dew_point",
        MetricType.CloudCover => "cloud_cover",
        MetricType.SkyTemperature => "sky_temperature",
        MetricType.SkyBrightness => "sky_brightness",
        MetricType.SkyQuality => "sky_quality",
        MetricType.RainRate => "rain_rate",
        MetricType.WindSpeed => "wind_speed",
        MetricType.WindGust => "wind_gust",
        MetricType.WindDirection => "wind_direction",
        MetricType.StarFwhm => "star_fwhm",
        MetricType.IsSafe => "is_safe",
        _ => "chart",
    };

    #endregion Public Methods

    #region Private Methods

    private static void DrawAdd(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / (PW - 2));
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var len = r.Width * 0.32f;

        g.DrawLine(pen, cx - len, cy, cx + len, cy);
        g.DrawLine(pen, cx, cy - len, cx, cy + len);
    }

    private static void DrawBrightness(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;

        // Central circle
        var cR = r.Width * 0.15f;
        g.DrawEllipse(pen, cx - cR, cy - cR, cR * 2, cR * 2);

        // Rays
        var rayInner = r.Width * 0.24f;
        var rayOuter = r.Width * 0.42f;
        var rayOuterShort = r.Width * 0.34f;
        for (int i = 0; i < 8; i++) {
            var angle = i * 45 * Math.PI / 180;
            var outer = (i % 2 == 0) ? rayOuter : rayOuterShort;
            g.DrawLine(pen,
                cx + (float)(rayInner * Math.Cos(angle)), cy + (float)(rayInner * Math.Sin(angle)),
                cx + (float)(outer * Math.Cos(angle)), cy + (float)(outer * Math.Sin(angle)));
        }
    }

    private static void DrawChart(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Smooth chart line using bezier curve
        var points = new PointF[] {
            new(r.X + r.Width * 0.1f, r.Y + r.Height * 0.75f),
            new(r.X + r.Width * 0.35f, r.Y + r.Height * 0.45f),
            new(r.X + r.Width * 0.55f, r.Y + r.Height * 0.55f),
            new(r.X + r.Width * 0.9f, r.Y + r.Height * 0.2f),
        };
        g.DrawCurve(pen, points, 0.3f);
    }

    private static void DrawCheck(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / (PW - 4));

        g.DrawLines(pen, [
            new PointF(r.X + r.Width * 0.15f, r.Y + r.Height * 0.5f),
            new PointF(r.X + r.Width * 0.4f, r.Y + r.Height * 0.75f),
            new PointF(r.X + r.Width * 0.85f, r.Y + r.Height * 0.25f),
        ]);
    }

    private static void DrawClose(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / (PW - 4));

        g.DrawLine(pen, r.X + r.Width * 0.2f, r.Y + r.Height * 0.2f,
                       r.X + r.Width * 0.8f, r.Y + r.Height * 0.8f);
        g.DrawLine(pen, r.X + r.Width * 0.8f, r.Y + r.Height * 0.2f,
                       r.X + r.Width * 0.2f, r.Y + r.Height * 0.8f);
    }

    private static void DrawCloud(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Cloud body entirely from bezier curves for smooth shape
        using var path = new GraphicsPath();
        // Start bottom-left, go clockwise
        path.AddBezier(
            new PointF(r.X + r.Width * 0.15f, r.Y + r.Height * 0.68f),
            new PointF(r.X + r.Width * 0.02f, r.Y + r.Height * 0.68f),
            new PointF(r.X + r.Width * 0.02f, r.Y + r.Height * 0.35f),
            new PointF(r.X + r.Width * 0.22f, r.Y + r.Height * 0.32f));
        path.AddBezier(
            new PointF(r.X + r.Width * 0.22f, r.Y + r.Height * 0.32f),
            new PointF(r.X + r.Width * 0.26f, r.Y + r.Height * 0.12f),
            new PointF(r.X + r.Width * 0.58f, r.Y + r.Height * 0.08f),
            new PointF(r.X + r.Width * 0.62f, r.Y + r.Height * 0.25f));
        path.AddBezier(
            new PointF(r.X + r.Width * 0.62f, r.Y + r.Height * 0.25f),
            new PointF(r.X + r.Width * 0.72f, r.Y + r.Height * 0.18f),
            new PointF(r.X + r.Width * 0.96f, r.Y + r.Height * 0.28f),
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.52f));
        path.AddBezier(
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.52f),
            new PointF(r.X + r.Width * 0.96f, r.Y + r.Height * 0.62f),
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.68f),
            new PointF(r.X + r.Width * 0.82f, r.Y + r.Height * 0.68f));
        path.AddLine(
            new PointF(r.X + r.Width * 0.82f, r.Y + r.Height * 0.68f),
            new PointF(r.X + r.Width * 0.15f, r.Y + r.Height * 0.68f));
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    private static void DrawCompass(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW2, false);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var outerR = r.Width * 0.42f;

        // Outer circle
        g.DrawEllipse(pen, cx - outerR, cy - outerR, outerR * 2, outerR * 2);

        // Compass needle
        using var brush = new SolidBrush(c);
        var needleLen = outerR * 0.7f;
        var needleW = outerR * 0.18f;

        // North half (filled)
        g.FillPolygon(brush, [
            new PointF(cx, cy - needleLen),
            new PointF(cx - needleW, cy),
            new PointF(cx + needleW, cy),
        ]);

        // South half (outline)
        g.DrawPolygon(pen, [
            new PointF(cx, cy + needleLen),
            new PointF(cx - needleW, cy),
            new PointF(cx + needleW, cy),
        ]);

        // Cardinal tick marks
        using var tickPen = MakePen(c, r.Width / PW3);
        float[][] cardinals = [[0, -1], [1, 0], [0, 1], [-1, 0]];
        foreach (var dir in cardinals) {
            var inner = outerR * 0.85f;
            var outer2 = outerR;
            g.DrawLine(tickPen,
                cx + dir[0] * inner, cy + dir[1] * inner,
                cx + dir[0] * outer2, cy + dir[1] * outer2);
        }
    }

    private static void DrawContrast(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        using var brush = new SolidBrush(c);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var radius = r.Width * 0.38f;

        // Full circle outline
        g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);

        // Fill left half using a clipped region
        var state = g.Save();
        g.SetClip(new RectangleF(r.X, r.Y, r.Width / 2, r.Height));
        g.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);
        g.Restore(state);
    }

    private static void DrawCopy(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW, false);

        var backRect = new RectangleF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.1f,
                                       r.Width * 0.55f, r.Height * 0.55f);
        g.DrawRectangle(pen, backRect.X, backRect.Y, backRect.Width, backRect.Height);

        var frontRect = new RectangleF(r.X + r.Width * 0.1f, r.Y + r.Height * 0.35f,
                                        r.Width * 0.55f, r.Height * 0.55f);
        g.DrawRectangle(pen, frontRect.X, frontRect.Y, frontRect.Width, frontRect.Height);
    }

    private static void DrawDarkMode(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var moonR = r.Width * 0.35f;

        using var path = new GraphicsPath();
        path.AddArc(cx - moonR, cy - moonR, moonR * 2, moonR * 2, -45, 270);
        path.AddArc(cx - moonR * 0.3f, cy - moonR * 0.8f, moonR * 1.4f, moonR * 1.6f, 225, -270);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    private static void DrawDashboard(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW2, false);
        var gap = r.Width * 0.08f;
        var halfW = (r.Width - gap) / 2;
        var halfH = (r.Height - gap) / 2;

        g.DrawRectangle(pen, r.X, r.Y, halfW, halfH);
        g.DrawRectangle(pen, r.X + halfW + gap, r.Y, halfW, halfH);
        g.DrawRectangle(pen, r.X, r.Y + halfH + gap, halfW, halfH);
        g.DrawRectangle(pen, r.X + halfW + gap, r.Y + halfH + gap, halfW, halfH);
    }

    private static void DrawDelete(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Trash can lid
        g.DrawLine(pen, r.X + r.Width * 0.2f, r.Y + r.Height * 0.25f,
                       r.X + r.Width * 0.8f, r.Y + r.Height * 0.25f);
        // Handle
        using var handlePath = new GraphicsPath();
        handlePath.AddArc(r.X + r.Width * 0.36f, r.Y + r.Height * 0.1f,
                          r.Width * 0.28f, r.Height * 0.16f, 180, 180);
        g.DrawPath(pen, handlePath);

        // Trash can body with slight taper via bezier
        using var bodyPath = new GraphicsPath();
        bodyPath.AddLine(r.X + r.Width * 0.25f, r.Y + r.Height * 0.3f,
                         r.X + r.Width * 0.28f, r.Y + r.Height * 0.85f);
        bodyPath.AddLine(r.X + r.Width * 0.28f, r.Y + r.Height * 0.85f,
                         r.X + r.Width * 0.72f, r.Y + r.Height * 0.85f);
        bodyPath.AddLine(r.X + r.Width * 0.72f, r.Y + r.Height * 0.85f,
                         r.X + r.Width * 0.75f, r.Y + r.Height * 0.3f);
        g.DrawPath(pen, bodyPath);
    }

    private static void DrawDewPoint(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width * 0.38f;

        // Small drop with bezier
        using var path = new GraphicsPath();
        path.AddBezier(
            new PointF(cx, r.Y + r.Height * 0.12f),
            new PointF(cx - r.Width * 0.03f, r.Y + r.Height * 0.28f),
            new PointF(cx - r.Width * 0.24f, r.Y + r.Height * 0.42f),
            new PointF(cx - r.Width * 0.22f, r.Y + r.Height * 0.58f));
        path.AddBezier(
            new PointF(cx - r.Width * 0.22f, r.Y + r.Height * 0.58f),
            new PointF(cx - r.Width * 0.18f, r.Y + r.Height * 0.76f),
            new PointF(cx + r.Width * 0.18f, r.Y + r.Height * 0.76f),
            new PointF(cx + r.Width * 0.22f, r.Y + r.Height * 0.58f));
        path.AddBezier(
            new PointF(cx + r.Width * 0.22f, r.Y + r.Height * 0.58f),
            new PointF(cx + r.Width * 0.24f, r.Y + r.Height * 0.42f),
            new PointF(cx + r.Width * 0.03f, r.Y + r.Height * 0.28f),
            new PointF(cx, r.Y + r.Height * 0.12f));
        path.CloseFigure();
        g.DrawPath(pen, path);

        // Small condensation dots
        using var brush = new SolidBrush(c);
        var dotR = r.Width * 0.035f;
        g.FillEllipse(brush, r.X + r.Width * 0.66f, r.Y + r.Height * 0.3f, dotR * 2, dotR * 2);
        g.FillEllipse(brush, r.X + r.Width * 0.73f, r.Y + r.Height * 0.48f, dotR * 2, dotR * 2);
        g.FillEllipse(brush, r.X + r.Width * 0.62f, r.Y + r.Height * 0.62f, dotR * 2, dotR * 2);

        // Temperature line
        using var linePen = MakePen(c, r.Width / PW2);
        g.DrawLine(linePen, r.X + r.Width * 0.76f, r.Y + r.Height * 0.7f, r.X + r.Width * 0.76f, r.Y + r.Height * 0.88f);
        g.DrawLine(linePen, r.X + r.Width * 0.69f, r.Y + r.Height * 0.81f, r.X + r.Width * 0.83f, r.Y + r.Height * 0.81f);
    }

    private static void DrawEdit(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Pencil body
        var points = new PointF[] {
            new(r.X + r.Width * 0.15f, r.Y + r.Height * 0.85f),
            new(r.X + r.Width * 0.15f, r.Y + r.Height * 0.65f),
            new(r.X + r.Width * 0.65f, r.Y + r.Height * 0.15f),
            new(r.X + r.Width * 0.85f, r.Y + r.Height * 0.35f),
            new(r.X + r.Width * 0.35f, r.Y + r.Height * 0.85f),
        };
        g.DrawPolygon(pen, points);

        // Edit line at tip
        g.DrawLine(pen, r.X + r.Width * 0.55f, r.Y + r.Height * 0.25f,
                       r.X + r.Width * 0.75f, r.Y + r.Height * 0.45f);
    }

    private static void DrawExit(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Door frame (3 sides with rounded corners via path)
        var doorX = r.X + r.Width * 0.15f;
        var doorRight = r.X + r.Width * 0.55f;
        using var doorPath = new GraphicsPath();
        doorPath.AddLine(doorRight, r.Y + r.Height * 0.15f, doorX, r.Y + r.Height * 0.15f);
        doorPath.AddLine(doorX, r.Y + r.Height * 0.15f, doorX, r.Y + r.Height * 0.85f);
        doorPath.AddLine(doorX, r.Y + r.Height * 0.85f, doorRight, r.Y + r.Height * 0.85f);
        g.DrawPath(pen, doorPath);

        // Arrow with smooth head
        var arrowY = r.Y + r.Height / 2;
        var arrowStart = r.X + r.Width * 0.4f;
        var arrowEnd = r.X + r.Width * 0.88f;
        g.DrawLine(pen, arrowStart, arrowY, arrowEnd, arrowY);
        g.DrawLine(pen, arrowEnd - r.Width * 0.14f, arrowY - r.Height * 0.13f, arrowEnd, arrowY);
        g.DrawLine(pen, arrowEnd - r.Width * 0.14f, arrowY + r.Height * 0.13f, arrowEnd, arrowY);
    }

    private static void DrawFolder(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Folder with smooth tab via bezier
        using var path = new GraphicsPath();
        path.AddLine(r.X + r.Width * 0.1f, r.Y + r.Height * 0.85f,
                     r.X + r.Width * 0.1f, r.Y + r.Height * 0.22f);
        path.AddLine(r.X + r.Width * 0.1f, r.Y + r.Height * 0.22f,
                     r.X + r.Width * 0.35f, r.Y + r.Height * 0.22f);
        path.AddBezier(
            new PointF(r.X + r.Width * 0.35f, r.Y + r.Height * 0.22f),
            new PointF(r.X + r.Width * 0.40f, r.Y + r.Height * 0.22f),
            new PointF(r.X + r.Width * 0.40f, r.Y + r.Height * 0.32f),
            new PointF(r.X + r.Width * 0.45f, r.Y + r.Height * 0.32f));
        path.AddLine(r.X + r.Width * 0.45f, r.Y + r.Height * 0.32f,
                     r.X + r.Width * 0.9f, r.Y + r.Height * 0.32f);
        path.AddLine(r.X + r.Width * 0.9f, r.Y + r.Height * 0.32f,
                     r.X + r.Width * 0.9f, r.Y + r.Height * 0.85f);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }

    private static void DrawHelp(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        using var brush = new SolidBrush(c);
        var cx = r.X + r.Width / 2;

        g.DrawEllipse(pen, r.X + r.Width * 0.1f, r.Y + r.Height * 0.1f,
                          r.Width * 0.8f, r.Height * 0.8f);

        g.DrawArc(pen, cx - r.Width * 0.15f, r.Y + r.Height * 0.24f,
                      r.Width * 0.3f, r.Height * 0.24f, 180, 270);
        g.DrawLine(pen, cx, r.Y + r.Height * 0.48f, cx, r.Y + r.Height * 0.57f);

        g.FillEllipse(brush, cx - r.Width * 0.04f, r.Y + r.Height * 0.66f,
                            r.Width * 0.08f, r.Height * 0.08f);
    }

    private static void DrawHumidity(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;

        // Smooth water drop using bezier curves
        using var path = new GraphicsPath();
        path.AddBezier(
            new PointF(cx, r.Y + r.Height * 0.1f),
            new PointF(cx - r.Width * 0.04f, r.Y + r.Height * 0.25f),
            new PointF(cx - r.Width * 0.34f, r.Y + r.Height * 0.45f),
            new PointF(cx - r.Width * 0.3f, r.Y + r.Height * 0.65f));
        path.AddBezier(
            new PointF(cx - r.Width * 0.3f, r.Y + r.Height * 0.65f),
            new PointF(cx - r.Width * 0.26f, r.Y + r.Height * 0.88f),
            new PointF(cx + r.Width * 0.26f, r.Y + r.Height * 0.88f),
            new PointF(cx + r.Width * 0.3f, r.Y + r.Height * 0.65f));
        path.AddBezier(
            new PointF(cx + r.Width * 0.3f, r.Y + r.Height * 0.65f),
            new PointF(cx + r.Width * 0.34f, r.Y + r.Height * 0.45f),
            new PointF(cx + r.Width * 0.04f, r.Y + r.Height * 0.25f),
            new PointF(cx, r.Y + r.Height * 0.1f));
        path.CloseFigure();
        g.DrawPath(pen, path);

        // Inner wave line
        using var wavePen = MakePen(c, r.Width / PW2);
        var wy = r.Y + r.Height * 0.6f;
        using var wavePath = new GraphicsPath();
        wavePath.AddBezier(
            new PointF(cx - r.Width * 0.12f, wy),
            new PointF(cx - r.Width * 0.06f, wy - r.Height * 0.06f),
            new PointF(cx - r.Width * 0.06f, wy + r.Height * 0.06f),
            new PointF(cx, wy));
        wavePath.AddBezier(
            new PointF(cx, wy),
            new PointF(cx + r.Width * 0.06f, wy - r.Height * 0.06f),
            new PointF(cx + r.Width * 0.06f, wy + r.Height * 0.06f),
            new PointF(cx + r.Width * 0.12f, wy));
        g.DrawPath(wavePen, wavePath);
    }

    private static void DrawInfo(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        using var brush = new SolidBrush(c);
        var cx = r.X + r.Width / 2;

        g.DrawEllipse(pen, r.X + r.Width * 0.1f, r.Y + r.Height * 0.1f,
                          r.Width * 0.8f, r.Height * 0.8f);

        g.FillEllipse(brush, cx - r.Width * 0.05f, r.Y + r.Height * 0.26f,
                            r.Width * 0.1f, r.Height * 0.1f);

        g.DrawLine(pen, cx, r.Y + r.Height * 0.45f, cx, r.Y + r.Height * 0.72f);
    }

    private static void DrawLightMode(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;

        // Sun center
        var sunR = r.Width * 0.18f;
        g.DrawEllipse(pen, cx - sunR, cy - sunR, sunR * 2, sunR * 2);

        // Sun rays
        var rayInner = r.Width * 0.28f;
        var rayOuter = r.Width * 0.43f;
        for (int i = 0; i < 8; i++) {
            var angle = i * 45 * Math.PI / 180;
            g.DrawLine(pen,
                cx + (float)(rayInner * Math.Cos(angle)), cy + (float)(rayInner * Math.Sin(angle)),
                cx + (float)(rayOuter * Math.Cos(angle)), cy + (float)(rayOuter * Math.Sin(angle)));
        }
    }

    private static void DrawPalette(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Palette shape using bezier for organic form
        using var path = new GraphicsPath();
        path.AddBezier(
            new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.12f),
            new PointF(r.X + r.Width * 0.9f, r.Y + r.Height * 0.12f),
            new PointF(r.X + r.Width * 0.95f, r.Y + r.Height * 0.85f),
            new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.85f));
        path.AddBezier(
            new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.85f),
            new PointF(r.X + r.Width * 0.05f, r.Y + r.Height * 0.85f),
            new PointF(r.X + r.Width * 0.05f, r.Y + r.Height * 0.12f),
            new PointF(r.X + r.Width * 0.5f, r.Y + r.Height * 0.12f));
        g.DrawPath(pen, path);

        // Color dots
        using var brush = new SolidBrush(c);
        var dotSize = r.Width * 0.1f;
        g.FillEllipse(brush, r.X + r.Width * 0.27f, r.Y + r.Height * 0.35f, dotSize, dotSize);
        g.FillEllipse(brush, r.X + r.Width * 0.47f, r.Y + r.Height * 0.28f, dotSize, dotSize);
        g.FillEllipse(brush, r.X + r.Width * 0.64f, r.Y + r.Height * 0.38f, dotSize, dotSize);
        g.FillEllipse(brush, r.X + r.Width * 0.55f, r.Y + r.Height * 0.58f, dotSize, dotSize);
    }

    private static void DrawPressure(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height * 0.55f;
        var gaugeR = r.Width * 0.4f;

        // Gauge arc
        g.DrawArc(pen, cx - gaugeR, cy - gaugeR, gaugeR * 2, gaugeR * 2, 200, 140);

        // Base line
        g.DrawLine(pen, cx - gaugeR * 0.85f, cy + gaugeR * 0.15f, cx + gaugeR * 0.85f, cy + gaugeR * 0.15f);

        // Needle
        using var needlePen = MakePen(c, r.Width / PW);
        var needleAngle = 250 * Math.PI / 180;
        g.DrawLine(needlePen, cx, cy,
            cx + (float)(gaugeR * 0.58f * Math.Cos(needleAngle)),
            cy + (float)(gaugeR * 0.58f * Math.Sin(needleAngle)));

        // Center dot
        using var brush = new SolidBrush(c);
        var dotR = r.Width * 0.04f;
        g.FillEllipse(brush, cx - dotR, cy - dotR, dotR * 2, dotR * 2);

        // Tick marks
        using var tickPen = MakePen(c, r.Width / PW3);
        for (int i = 0; i <= 4; i++) {
            var angle = (200 + i * 35) * Math.PI / 180;
            var inner = gaugeR * 0.82f;
            var outer = gaugeR;
            g.DrawLine(tickPen,
                cx + (float)(inner * Math.Cos(angle)), cy + (float)(inner * Math.Sin(angle)),
                cx + (float)(outer * Math.Cos(angle)), cy + (float)(outer * Math.Sin(angle)));
        }
    }

    private static void DrawRain(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Cloud at top using bezier
        using var cloudPath = new GraphicsPath();
        var baseY = r.Y + r.Height * 0.38f;
        cloudPath.AddBezier(
            new PointF(r.X + r.Width * 0.12f, baseY),
            new PointF(r.X + r.Width * 0.02f, baseY),
            new PointF(r.X + r.Width * 0.02f, r.Y + r.Height * 0.12f),
            new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.12f));
        cloudPath.AddBezier(
            new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.12f),
            new PointF(r.X + r.Width * 0.32f, r.Y + r.Height * 0.02f),
            new PointF(r.X + r.Width * 0.60f, r.Y + r.Height * 0.02f),
            new PointF(r.X + r.Width * 0.65f, r.Y + r.Height * 0.14f));
        cloudPath.AddBezier(
            new PointF(r.X + r.Width * 0.65f, r.Y + r.Height * 0.14f),
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.08f),
            new PointF(r.X + r.Width * 0.96f, r.Y + r.Height * 0.3f),
            new PointF(r.X + r.Width * 0.88f, baseY));
        cloudPath.AddLine(new PointF(r.X + r.Width * 0.88f, baseY),
                          new PointF(r.X + r.Width * 0.12f, baseY));
        cloudPath.CloseFigure();
        g.DrawPath(pen, cloudPath);

        // Rain drops using small bezier curves (teardrop shapes)
        using var dropPen = MakePen(c, r.Width / PW2);
        float[][] drops = [
            [0.25f, 0.50f, 0.13f], [0.5f, 0.50f, 0.13f], [0.75f, 0.50f, 0.13f],
            [0.35f, 0.70f, 0.12f], [0.6f, 0.70f, 0.12f]
        ];
        foreach (var d in drops) {
            var dx = r.X + r.Width * d[0];
            var dy = r.Y + r.Height * d[1];
            var len = r.Height * d[2];
            g.DrawLine(dropPen, dx, dy, dx - len * 0.25f, dy + len);
        }
    }

    private static void DrawRefresh(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var arcR = r.Width * 0.35f;

        g.DrawArc(pen, cx - arcR, cy - arcR, arcR * 2, arcR * 2, -60, 300);

        // Arrow head
        var arrowX = cx + arcR * (float)Math.Cos(-60 * Math.PI / 180);
        var arrowY = cy + arcR * (float)Math.Sin(-60 * Math.PI / 180);
        g.DrawLine(pen, arrowX, arrowY, arrowX - r.Width * 0.13f, arrowY);
        g.DrawLine(pen, arrowX, arrowY, arrowX - r.Width * 0.07f, arrowY + r.Height * 0.1f);
    }

    private static void DrawSchedule(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        var cx = r.X + r.Width / 2f;
        var cy = r.Y + r.Height / 2f;
        var radius = r.Width * 0.33f;

        // Clock circle
        g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2f, radius * 2f);

        // Clock hands
        g.DrawLine(pen, cx, cy, cx, cy - radius * 0.5f);
        g.DrawLine(pen, cx, cy, cx + radius * 0.4f, cy);

        // Preset list lines
        g.DrawLine(pen, r.X + r.Width * 0.1f, r.Y + r.Height * 0.2f, r.X + r.Width * 0.38f, r.Y + r.Height * 0.2f);
        g.DrawLine(pen, r.X + r.Width * 0.1f, r.Y + r.Height * 0.8f, r.X + r.Width * 0.38f, r.Y + r.Height * 0.8f);
        g.DrawLine(pen, r.X + r.Width * 0.62f, r.Y + r.Height * 0.2f, r.X + r.Width * 0.9f, r.Y + r.Height * 0.2f);
        g.DrawLine(pen, r.X + r.Width * 0.62f, r.Y + r.Height * 0.8f, r.X + r.Width * 0.9f, r.Y + r.Height * 0.8f);
    }

    private static void DrawSave(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW, false);

        g.DrawRectangle(pen, r.X + r.Width * 0.12f, r.Y + r.Height * 0.12f,
                           r.Width * 0.76f, r.Height * 0.76f);

        g.DrawRectangle(pen, r.X + r.Width * 0.26f, r.Y + r.Height * 0.12f,
                           r.Width * 0.48f, r.Height * 0.26f);

        g.DrawRectangle(pen, r.X + r.Width * 0.55f, r.Y + r.Height * 0.17f,
                           r.Width * 0.1f, r.Height * 0.14f);
    }

    private static void DrawSettings(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var outerR = r.Width * 0.42f;
        var toothR = r.Width * 0.34f;

        // Draw gear with smooth teeth using bezier curves
        using var path = new GraphicsPath();
        var teeth = 8;
        for (int i = 0; i < teeth; i++) {
            var a1 = (i * 360f / teeth - 18) * Math.PI / 180;
            var a2 = (i * 360f / teeth - 6) * Math.PI / 180;
            var a3 = (i * 360f / teeth + 6) * Math.PI / 180;
            var a4 = (i * 360f / teeth + 18) * Math.PI / 180;

            var p1 = new PointF(cx + (float)(toothR * Math.Cos(a1)), cy + (float)(toothR * Math.Sin(a1)));
            var p2 = new PointF(cx + (float)(outerR * Math.Cos(a2)), cy + (float)(outerR * Math.Sin(a2)));
            var p3 = new PointF(cx + (float)(outerR * Math.Cos(a3)), cy + (float)(outerR * Math.Sin(a3)));
            var p4 = new PointF(cx + (float)(toothR * Math.Cos(a4)), cy + (float)(toothR * Math.Sin(a4)));

            if (i == 0) {
                path.AddBezier(p1, p2, p2, p2);
            } else {
                path.AddBezier(path.GetLastPoint(), p1, p1, p1);
            }

            path.AddBezier(p1, p2, p2, p2);
            path.AddBezier(p2, p3, p3, p3);
            path.AddBezier(p3, p4, p4, p4);
        }
        path.CloseFigure();
        g.DrawPath(pen, path);

        // Center circle
        var centerR = r.Width * 0.14f;
        g.DrawEllipse(pen, cx - centerR, cy - centerR, centerR * 2, centerR * 2);
    }

    private static void DrawShield(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var cx = r.X + r.Width / 2;

        // Shield outline with smooth bezier curves
        using var path = new GraphicsPath();
        path.AddBezier(
            new PointF(cx, r.Y + r.Height * 0.08f),
            new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.08f),
            new PointF(r.X + r.Width * 0.12f, r.Y + r.Height * 0.15f),
            new PointF(r.X + r.Width * 0.12f, r.Y + r.Height * 0.3f));
        path.AddBezier(
            new PointF(r.X + r.Width * 0.12f, r.Y + r.Height * 0.3f),
            new PointF(r.X + r.Width * 0.12f, r.Y + r.Height * 0.6f),
            new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.82f),
            new PointF(cx, r.Y + r.Height * 0.92f));
        path.AddBezier(
            new PointF(cx, r.Y + r.Height * 0.92f),
            new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.82f),
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.6f),
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.3f));
        path.AddBezier(
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.3f),
            new PointF(r.X + r.Width * 0.88f, r.Y + r.Height * 0.15f),
            new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.08f),
            new PointF(cx, r.Y + r.Height * 0.08f));
        path.CloseFigure();
        g.DrawPath(pen, path);

        // Checkmark inside
        using var checkPen = MakePen(c, r.Width / PW);
        g.DrawLines(checkPen, [
            new PointF(r.X + r.Width * 0.32f, r.Y + r.Height * 0.5f),
            new PointF(r.X + r.Width * 0.45f, r.Y + r.Height * 0.64f),
            new PointF(r.X + r.Width * 0.68f, r.Y + r.Height * 0.36f),
        ]);
    }

    private static void DrawSkyTemperature(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Moon crescent
        var moonCx = r.X + r.Width * 0.35f;
        var moonCy = r.Y + r.Height * 0.35f;
        var moonR = r.Width * 0.22f;

        using var path = new GraphicsPath();
        path.AddArc(moonCx - moonR, moonCy - moonR, moonR * 2, moonR * 2, -45, 270);
        path.AddArc(moonCx - moonR * 0.2f, moonCy - moonR * 0.7f, moonR * 1.2f, moonR * 1.4f, 225, -270);
        path.CloseFigure();
        g.DrawPath(pen, path);

        // Small thermometer
        using var tPen = MakePen(c, r.Width / PW2);
        var tx = r.X + r.Width * 0.72f;
        g.DrawLine(tPen, tx, r.Y + r.Height * 0.38f, tx, r.Y + r.Height * 0.68f);
        using var brush = new SolidBrush(c);
        var bR = r.Width * 0.06f;
        g.FillEllipse(brush, tx - bR, r.Y + r.Height * 0.72f, bR * 2, bR * 2);
    }

    private static void DrawStar(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW2);
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;

        // 5-point star with smoothed points via cardinal spline
        var outerR = r.Width * 0.44f;
        var innerR = r.Width * 0.18f;
        var points = new PointF[10];
        for (int i = 0; i < 10; i++) {
            var angle = (i * 36 - 90) * Math.PI / 180;
            var radius = (i % 2 == 0) ? outerR : innerR;
            points[i] = new PointF(cx + (float)(radius * Math.Cos(angle)), cy + (float)(radius * Math.Sin(angle)));
        }
        using var path = new GraphicsPath();
        path.AddPolygon(points);
        g.DrawPath(pen, path);
    }

    private static void DrawTelescope(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);

        // Telescope tube
        var x1 = r.X + r.Width * 0.12f;
        var y1 = r.Y + r.Height * 0.22f;
        var x2 = r.X + r.Width * 0.72f;
        var y2 = r.Y + r.Height * 0.52f;
        using var tubePen = MakePen(c, r.Width * 0.08f);
        g.DrawLine(tubePen, x1, y1, x2, y2);

        // Lens
        var lensAngle = Math.Atan2(y2 - y1, x2 - x1) + Math.PI / 2;
        var lensLen = r.Width * 0.1f;
        g.DrawLine(pen,
            x1 + (float)(lensLen * Math.Cos(lensAngle)),
            y1 + (float)(lensLen * Math.Sin(lensAngle)),
            x1 - (float)(lensLen * Math.Cos(lensAngle)),
            y1 - (float)(lensLen * Math.Sin(lensAngle)));

        // Tripod legs
        var pivotX = r.X + r.Width * 0.54f;
        var pivotY = r.Y + r.Height * 0.48f;
        g.DrawLine(pen, pivotX, pivotY, r.X + r.Width * 0.35f, r.Y + r.Height * 0.88f);
        g.DrawLine(pen, pivotX, pivotY, r.X + r.Width * 0.73f, r.Y + r.Height * 0.88f);
        g.DrawLine(pen, pivotX, pivotY, r.X + r.Width * 0.54f, r.Y + r.Height * 0.9f);

        // Small star
        using var starBrush = new SolidBrush(c);
        var sr = r.Width * 0.04f;
        g.FillEllipse(starBrush, r.X + r.Width * 0.84f - sr, r.Y + r.Height * 0.12f - sr, sr * 2, sr * 2);
    }

    private static void DrawTemperature(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        using var brush = new SolidBrush(c);
        var cx = r.X + r.Width * 0.45f;

        // Tube with rounded top
        var tubeW = r.Width * 0.13f;
        var tubeTop = r.Y + r.Height * 0.18f;
        var tubeBot = r.Y + r.Height * 0.58f;

        using var tubePath = new GraphicsPath();
        tubePath.AddLine(cx - tubeW, tubeBot, cx - tubeW, tubeTop);
        tubePath.AddArc(cx - tubeW, tubeTop - tubeW, tubeW * 2, tubeW * 2, 180, 180);
        tubePath.AddLine(cx + tubeW, tubeTop, cx + tubeW, tubeBot);
        g.DrawPath(pen, tubePath);

        // Bulb
        var bulbR = r.Width * 0.18f;
        var bulbY = r.Y + r.Height * 0.72f;
        g.DrawEllipse(pen, cx - bulbR, bulbY - bulbR, bulbR * 2, bulbR * 2);
        g.FillEllipse(brush, cx - bulbR * 0.45f, bulbY - bulbR * 0.45f, bulbR * 0.9f, bulbR * 0.9f);

        // Mercury level
        var mercuryTop = r.Y + r.Height * 0.35f;
        using var mercuryPen = MakePen(c, r.Width * 0.06f);
        g.DrawLine(mercuryPen, cx, mercuryTop, cx, tubeBot);

        // Scale ticks
        using var tickPen = MakePen(c, r.Width / PW3);
        for (int i = 0; i < 4; i++) {
            var ty = tubeTop + r.Height * 0.06f + i * r.Height * 0.1f;
            g.DrawLine(tickPen, cx + tubeW, ty, cx + tubeW + r.Width * 0.09f, ty);
        }
    }

    // finest detail stroke
    // ── Metric tile icons ──────────────────────────────────────────
    private static void DrawWind(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var x1 = r.X + r.Width * 0.1f;

        // Three wind lines with smooth curved ends via bezier
        // Top line
        var y1 = r.Y + r.Height * 0.25f;
        g.DrawLine(pen, x1, y1, r.X + r.Width * 0.6f, y1);
        using var arc1 = new GraphicsPath();
        arc1.AddBezier(
            new PointF(r.X + r.Width * 0.6f, y1),
            new PointF(r.X + r.Width * 0.78f, y1),
            new PointF(r.X + r.Width * 0.78f, y1 - r.Height * 0.16f),
            new PointF(r.X + r.Width * 0.65f, y1 - r.Height * 0.16f));
        g.DrawPath(pen, arc1);

        // Middle line (longest)
        var y2 = r.Y + r.Height * 0.5f;
        g.DrawLine(pen, x1, y2, r.X + r.Width * 0.7f, y2);
        using var arc2 = new GraphicsPath();
        arc2.AddBezier(
            new PointF(r.X + r.Width * 0.7f, y2),
            new PointF(r.X + r.Width * 0.9f, y2),
            new PointF(r.X + r.Width * 0.9f, y2 - r.Height * 0.16f),
            new PointF(r.X + r.Width * 0.75f, y2 - r.Height * 0.16f));
        g.DrawPath(pen, arc2);

        // Bottom line (shorter)
        var y3 = r.Y + r.Height * 0.75f;
        g.DrawLine(pen, x1 + r.Width * 0.08f, y3, r.X + r.Width * 0.55f, y3);
        using var arc3 = new GraphicsPath();
        arc3.AddBezier(
            new PointF(r.X + r.Width * 0.55f, y3),
            new PointF(r.X + r.Width * 0.72f, y3),
            new PointF(r.X + r.Width * 0.72f, y3 - r.Height * 0.14f),
            new PointF(r.X + r.Width * 0.58f, y3 - r.Height * 0.14f));
        g.DrawPath(pen, arc3);
    }

    private static void DrawWindGust(Graphics g, RectangleF r, Color c) {
        using var pen = MakePen(c, r.Width / PW);
        var x1 = r.X + r.Width * 0.05f;

        // Top line with bezier curl
        var y1 = r.Y + r.Height * 0.2f;
        g.DrawLine(pen, x1, y1, r.X + r.Width * 0.55f, y1);
        using var a1 = new GraphicsPath();
        a1.AddBezier(
            new PointF(r.X + r.Width * 0.55f, y1),
            new PointF(r.X + r.Width * 0.76f, y1),
            new PointF(r.X + r.Width * 0.76f, y1 - r.Height * 0.2f),
            new PointF(r.X + r.Width * 0.58f, y1 - r.Height * 0.14f));
        g.DrawPath(pen, a1);

        // Middle line
        var y2 = r.Y + r.Height * 0.45f;
        g.DrawLine(pen, x1 + r.Width * 0.05f, y2, r.X + r.Width * 0.65f, y2);
        using var a2 = new GraphicsPath();
        a2.AddBezier(
            new PointF(r.X + r.Width * 0.65f, y2),
            new PointF(r.X + r.Width * 0.88f, y2),
            new PointF(r.X + r.Width * 0.88f, y2 - r.Height * 0.22f),
            new PointF(r.X + r.Width * 0.68f, y2 - r.Height * 0.16f));
        g.DrawPath(pen, a2);

        // Bottom line
        var y3 = r.Y + r.Height * 0.7f;
        g.DrawLine(pen, x1 + r.Width * 0.12f, y3, r.X + r.Width * 0.52f, y3);
        using var a3 = new GraphicsPath();
        a3.AddBezier(
            new PointF(r.X + r.Width * 0.52f, y3),
            new PointF(r.X + r.Width * 0.68f, y3),
            new PointF(r.X + r.Width * 0.68f, y3 - r.Height * 0.16f),
            new PointF(r.X + r.Width * 0.54f, y3 - r.Height * 0.12f));
        g.DrawPath(pen, a3);

        // Extra gust marks
        using var gustPen = MakePen(c, r.Width / PW3);
        g.DrawLine(gustPen, x1 + r.Width * 0.15f, r.Y + r.Height * 0.88f, x1 + r.Width * 0.4f, r.Y + r.Height * 0.88f);
    }

    // ── Helper: standard thin pen ──────────────────────────────────
    private static Pen MakePen(Color c, float w, bool round = true) {
        var pen = new Pen(c, w) { LineJoin = LineJoin.Round };
        if (round) { pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; }
        return pen;
    }

    private static Bitmap RenderIcon(Action<Graphics, RectangleF, Color> drawer, Color color, int size) {
        // 2× supersampling: render at double resolution, then downscale for crisp edges
        var ssScale = 2;
        var ssSize = size * ssScale;

        var hires = new Bitmap(ssSize, ssSize, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(hires)) {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.Clear(Color.Transparent);

            var pad = Math.Max(2f, ssSize * 0.12f);
            var rect = new RectangleF(pad, pad, ssSize - pad * 2, ssSize - pad * 2);
            drawer(g, rect, color);
        }

        // Downscale to target size
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(bitmap)) {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(hires, 0, 0, size, size);
        }
        hires.Dispose();

        return bitmap;
    }

    #endregion Private Methods
}
