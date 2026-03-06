using System.Drawing.Drawing2D;
using MaterialSkin;
using SafetyMonitor.Forms;
using SafetyMonitor.Models;
using SafetyMonitor.Services;
using ColorScheme = SafetyMonitor.Models.ColorScheme;

namespace SafetyMonitor.Controls;

/// <summary>
/// Represents value tile and encapsulates its related behavior and state.
/// </summary>
public class ValueTile : Panel {

    #region Private Fields

    private readonly ColorSchemeService _colorSchemeService;
    private readonly ValueSchemeService _valueSchemeService;
    private readonly ValueTileConfig _config;
    private readonly DataService _dataService;
    private readonly ThemedMenuRenderer _contextMenuRenderer = new();
    private const int MenuIconSize = 22;
    private const float ContextMenuFontSize = 10f;
    private ContextMenuStrip? _contextMenu;
    private ColorScheme? _colorScheme;
    private ColorScheme? _iconColorScheme;
    private ColorScheme? _textColorScheme;
    private ValueScheme? _valueScheme;
    private Color _currentIconColor = Color.Transparent;
    private double? _currentValue;
    private Bitmap? _iconImage;
    private Rectangle _iconBounds = Rectangle.Empty;
    private bool _initialized;
    private Label? _titleLabel;
    private Label? _textLabel;
    private Label? _unitLabel;
    private Label? _valueLabel;
    private const float MinTitleFontSize = 10f;
    private const float MinTextFontSize = 11f;
    private const float MinValueFontSize = 10f;
    private const float MaxValueFontSize = 33.6f;
    private const int HorizontalFitPadding = 8;
    private const float TopGradientHeightRatio = 0.50f;
    private static readonly Color LightThemePrimaryColor = Color.FromArgb(66, 66, 66);
    private static readonly Color LightThemeBorderColor = Color.FromArgb(52, 52, 52);
    private static readonly Color DarkThemeBorderColor = Color.FromArgb(53, 70, 76);
    private static readonly Color LightThemeGradientFallbackColor = Color.FromArgb(176, 130, 138, 145);
    private static readonly Color DarkThemeGradientFallbackColor = Color.FromArgb(168, 112, 122, 128);
    private readonly Dictionary<(string Family, float Size, FontStyle Style), Font> _fontCache = [];

    #endregion Private Fields

    #region Public Events

    public event Action<ValueTile>? EditRequested;
    public event Action<ValueTile>? EditDashboardRequested;
    public event Action<ValueTile>? ViewSettingsChanged;

    #endregion Public Events

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueTile"/> class.
    /// </summary>
    /// <param name="config">Input value for config.</param>
    /// <param name="dataService">Input value for data service.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public ValueTile(ValueTileConfig config, DataService dataService) {
        _config = config;
        _dataService = dataService;
        _colorSchemeService = new ColorSchemeService();
        _valueSchemeService = new ValueSchemeService();

        Dock = DockStyle.Fill;
        Padding = new Padding(8);
        BorderStyle = BorderStyle.None;
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);

        // Set a valid font to prevent GDI+ errors during auto-scaling
        Font = SystemFonts.DefaultFont;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Refreshes the data for value tile.
    /// </summary>
    public void RefreshData() {
        if (_valueLabel == null || _textLabel == null) {
            return;
        }

        var latestData = _dataService.GetLatestData();
        if (latestData != null) {
            _currentValue = _config.Metric.GetValue(latestData);
            if (_currentValue.HasValue) {
                var transformedText = _valueScheme?.GetText(_currentValue.Value);
                var formattedValue = MetricDisplaySettingsStore.FormatMetricValue(_config.Metric, _currentValue.Value);
                UpdateDisplayedTexts(formattedValue, transformedText);
                ApplyColorScheme();
            } else {
                _valueLabel.Text = " ?";
                _textLabel.Text = "";
                ResetColors();
            }
        } else {
            _valueLabel.Text = " ?";
            _textLabel.Text = "";
            ResetColors();
        }

        // Value text can change on every refresh; recalculate layout/font fitting to prevent clipping.
        UpdateLayout();
    }

    /// <summary>
    /// Updates the theme for value tile.
    /// </summary>
    public void UpdateTheme() {
        UpdateLayout();
        ApplyContextMenuTheme();
        if (_currentValue.HasValue) {
            ApplyColorScheme();
        } else {
            ResetColors();
        }
        Invalidate(true);
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Executes on font changed as part of value tile processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        if (_initialized) {
            UpdateLayout();
        }
    }

    /// <summary>
    /// Executes on handle created as part of value tile processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);

        if (!_initialized) {
            _initialized = true;
            InitializeUI();
            LoadColorScheme();
            UpdateLayout();
            ResetColors();
        }
    }

    #endregion Protected Methods

    /// <summary>
    /// Executes on paint as part of value tile processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);

        DrawTopValueGradient(e.Graphics);

        if (_iconImage != null && !_iconBounds.IsEmpty && _config.ShowIcon) {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(_iconImage, _iconBounds);
        }

        DrawTileBorder(e.Graphics);
    }

    /// <summary>
    /// Executes dispose as part of value tile processing.
    /// </summary>
    /// <param name="disposing">Input value for disposing.</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _iconImage?.Dispose();
            _iconImage = null;

            foreach (var cachedFont in _fontCache.Values) {
                cachedFont.Dispose();
            }
            _fontCache.Clear();
        }

        base.Dispose(disposing);
    }

    #region Private Methods

    /// <summary>
    /// Creates the safe font for value tile.
    /// </summary>
    /// <param name="familyName">Input value for family name.</param>
    /// <param name="emSize">Input value for em size.</param>
    /// <param name="style">Input value for style.</param>
    /// <returns>The result of the operation.</returns>
    private static Font CreateSafeFont(string familyName, float emSize, FontStyle style = FontStyle.Regular) {
        try {
            var font = new Font(familyName, emSize, style);
            return font;
        } catch {
            // Fallback to system default font
            return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
        }
    }

    /// <summary>
    /// Gets the cached font for value tile.
    /// </summary>
    /// <param name="fontFamily">Input value for font family.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="style">Input value for style.</param>
    /// <returns>The result of the operation.</returns>
    private Font GetCachedFont(string fontFamily, float size, FontStyle style) {
        var normalizedSize = MathF.Round(size, 1);
        var key = (fontFamily, normalizedSize, style);
        if (_fontCache.TryGetValue(key, out var cachedFont)) {
            return cachedFont;
        }

        var createdFont = CreateSafeFont(fontFamily, normalizedSize, style);
        _fontCache[key] = createdFont;
        return createdFont;
    }

    /// <summary>
    /// Updates the font for value tile.
    /// </summary>
    /// <param name="label">Input value for label.</param>
    /// <param name="fontFamily">Input value for font family.</param>
    /// <param name="size">Input value for size.</param>
    /// <param name="style">Input value for style.</param>
    private void UpdateFont(Label label, string fontFamily, float size, FontStyle style) {
        label.Font = GetCachedFont(fontFamily, size, style);
    }

    /// <summary>
    /// Applies the theme colors for value tile.
    /// </summary>
    private void ApplyThemeColors() {
        if (_titleLabel == null || _textLabel == null || _valueLabel == null || _unitLabel == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        ApplyTransparentChildBackgrounds();

        var primaryColor = isLight ? LightThemePrimaryColor : Color.White;
        _titleLabel.ForeColor = primaryColor;
        _textLabel.ForeColor = primaryColor;
        _unitLabel.ForeColor = primaryColor;

        if (_currentValue.HasValue && _iconColorScheme != null) {
            SetIconColor(_iconColorScheme.GetColor(_currentValue.Value));
        } else {
            SetIconColor(primaryColor);
        }
    }

    /// <summary>
    /// Applies the transparent child backgrounds for value tile.
    /// </summary>
    private void ApplyTransparentChildBackgrounds() {
        foreach (Control child in Controls) {
            child.BackColor = Color.Transparent;
        }
    }

    /// <summary>
    /// Applies the color scheme for value tile.
    /// </summary>
    private void ApplyColorScheme() {
        if (_titleLabel == null || _textLabel == null || _valueLabel == null || _unitLabel == null) {
            return;
        }

        // Apply consistent theme colors for all elements except value
        ApplyThemeColors();

        // Value label gets color from the color scheme
        if (_currentValue.HasValue && _colorScheme != null) {
            _valueLabel.ForeColor = _colorScheme.GetColor(_currentValue.Value);
        } else {
            var skinManager = MaterialSkinManager.Instance;
            var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
            _valueLabel.ForeColor = isLight ? LightThemePrimaryColor : Color.White;
        }

        if (_currentValue.HasValue && _textColorScheme != null && HasTransformedTextForCurrentValue()) {
            var textColor = _textColorScheme.GetColor(_currentValue.Value);
            // Color must be assigned even before layout toggles text visibility.
            // Otherwise Text+Value mode can show an uncolored text badge on first render
            // and only pick up scheme color on the next refresh cycle.
            _textLabel.ForeColor = textColor;

            if (_config.DisplayMode == ValueTileDisplayMode.TextOnly) {
                _valueLabel.ForeColor = textColor;
            }
        }
    }

    /// <summary>
    /// Gets the contrast color for value tile.
    /// </summary>
    /// <param name="bg">Input value for bg.</param>
    /// <returns>The result of the operation.</returns>
    private static Color GetContrastColor(Color bg) {
        var brightness = (bg.R * 299 + bg.G * 587 + bg.B * 114) / 1000;
        return brightness > 128 ? Color.Black : Color.White;
    }

    /// <summary>
    /// Executes draw top value gradient as part of value tile processing.
    /// </summary>
    /// <param name="graphics">Input value for graphics.</param>
    private void DrawTopValueGradient(Graphics graphics) {
        if (!_config.ShowTopValueGradient || !_currentValue.HasValue || ClientSize.Width <= 0 || ClientSize.Height <= 0) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var gradientColor = _colorScheme != null
            ? _colorScheme.GetColor(_currentValue.Value)
            : (isLight ? LightThemeGradientFallbackColor : DarkThemeGradientFallbackColor);
        var gradientHeight = Math.Max(1, (int)Math.Round(ClientSize.Height * TopGradientHeightRatio));
        var gradientRect = new Rectangle(0, 0, ClientSize.Width, gradientHeight);

        using var brush = new LinearGradientBrush(
            gradientRect,
            gradientColor,
            Color.FromArgb(0, gradientColor),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(brush, gradientRect);
    }

    /// <summary>
    /// Initializes value tile state and required resources.
    /// </summary>
    private void InitializeUI() {
        // Title: small, top-left
        _titleLabel = new Label {
            Text = string.IsNullOrEmpty(_config.Title) ? _config.Metric.GetDisplayName() : _config.Title,
            AutoSize = false,
            TextAlign = ContentAlignment.TopLeft,
            BackColor = Color.Transparent,
            UseCompatibleTextRendering = true
        };


        // Value: large, bottom-left
        _valueLabel = new Label {
            Text = " ?",
            AutoSize = false,
            TextAlign = ContentAlignment.BottomLeft,
            BackColor = Color.Transparent
        };

        _textLabel = new Label {
            Text = "",
            AutoSize = false,
            TextAlign = ContentAlignment.BottomLeft,
            BackColor = Color.Transparent,
            Visible = false
        };

        // Unit: small, bottom-right
        _unitLabel = new Label {
            Text = _config.Metric.GetUnit(),
            AutoSize = false,
            TextAlign = ContentAlignment.BottomRight,
            BackColor = Color.Transparent,
            Visible = _config.ShowUnit
        };

        Controls.Add(_titleLabel);
        Controls.Add(_textLabel);
        Controls.Add(_valueLabel);
        Controls.Add(_unitLabel);

        _contextMenu = CreateContextMenu();
        ContextMenuStrip = _contextMenu;

        // Subscribe to resize for dynamic font scaling
        Resize += OnTileResize;
    }

    /// <summary>
    /// Loads the color scheme for value tile.
    /// </summary>
    private void LoadColorScheme() {
        var schemes = _colorSchemeService.LoadSchemes();
        _colorScheme = string.IsNullOrEmpty(_config.ColorSchemeName)
            ? null
            : schemes.FirstOrDefault(s => s.Name == _config.ColorSchemeName);
        _iconColorScheme = string.IsNullOrEmpty(_config.IconColorSchemeName)
            ? null
            : schemes.FirstOrDefault(s => s.Name == _config.IconColorSchemeName);
        _textColorScheme = string.IsNullOrEmpty(_config.TextColorSchemeName)
            ? null
            : schemes.FirstOrDefault(s => s.Name == _config.TextColorSchemeName);
        var valueSchemes = _valueSchemeService.LoadSchemes();
        _valueScheme = string.IsNullOrEmpty(_config.ValueSchemeName)
            ? null
            : valueSchemes.FirstOrDefault(s => s.Name == _config.ValueSchemeName);
    }

    /// <summary>
    /// Updates the displayed texts for value tile.
    /// </summary>
    /// <param name="formattedValue">Input value for formatted value.</param>
    /// <param name="transformedText">Input value for transformed text.</param>
    private void UpdateDisplayedTexts(string formattedValue, string? transformedText) {
        if (_textLabel == null || _valueLabel == null) {
            return;
        }

        var hasTransformedText = !string.IsNullOrWhiteSpace(transformedText);

        switch (_config.DisplayMode) {
            case ValueTileDisplayMode.ValueOnly:
                _textLabel.Text = "";
                _textLabel.Visible = false;
                _valueLabel.Text = formattedValue;
                break;
            case ValueTileDisplayMode.TextAndValue:
                _textLabel.Text = hasTransformedText ? transformedText! : "";
                _textLabel.Visible = hasTransformedText;
                _valueLabel.Text = formattedValue;
                break;
            case ValueTileDisplayMode.TextOnly:
            default:
                _textLabel.Text = "";
                _textLabel.Visible = false;
                _valueLabel.Text = hasTransformedText ? transformedText! : formattedValue;
                break;
        }
    }

    /// <summary>
    /// Determines whether has transformed text for current value for value tile.
    /// </summary>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private bool HasTransformedTextForCurrentValue() {
        if (!_currentValue.HasValue || _valueScheme == null) {
            return false;
        }

        var transformedText = _valueScheme.GetText(_currentValue.Value);
        return !string.IsNullOrWhiteSpace(transformedText);
    }

    /// <summary>
    /// Executes on tile resize as part of value tile processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void OnTileResize(object? sender, EventArgs e) {
        UpdateLayout();
        Invalidate();
    }

    /// <summary>
    /// Resets the colors for value tile.
    /// </summary>
    private void ResetColors() {
        if (_titleLabel == null || _textLabel == null || _valueLabel == null || _unitLabel == null) {
            return;
        }

        // Apply consistent theme colors for all elements
        ApplyThemeColors();

        // Value label uses primary theme color when no data / no scheme
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        _valueLabel.ForeColor = isLight ? LightThemePrimaryColor : Color.White;
    }

    /// <summary>
    /// Executes draw tile border as part of value tile processing.
    /// </summary>
    /// <param name="graphics">Input value for graphics.</param>
    private void DrawTileBorder(Graphics graphics) {
        if (ClientSize.Width <= 1 || ClientSize.Height <= 1) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var borderColor = isLight ? LightThemeBorderColor : DarkThemeBorderColor;
        ControlPaint.DrawBorder(graphics, ClientRectangle, borderColor, ButtonBorderStyle.Solid);
    }

    /// <summary>
    /// Sets the icon color for value tile.
    /// </summary>
    /// <param name="color">Input value for color.</param>
    private void SetIconColor(Color color) {
        if (color == _currentIconColor) {
            return;
        }

        _currentIconColor = color;
        var iconSize = Math.Max(16, _iconBounds.Width > 0 ? _iconBounds.Width : 16);
        UpdateIcon(iconSize);
        Invalidate();
    }

    /// <summary>
    /// Updates the icon for value tile.
    /// </summary>
    /// <param name="logicalSize">Input value for logical size.</param>
    private void UpdateIcon(int logicalSize) {
        // Render at physical pixel size for crisp display at any DPI scaling.
        // DeviceDpi is the actual monitor DPI (96 = 100%, 120 = 125%, 144 = 150%).
        var scaleFactor = DeviceDpi / 96f;
        var renderSize = Math.Max(16, (int)(logicalSize * scaleFactor));

        var iconName = MaterialIcons.GetMetricIconName(_config.Metric);
        var oldImage = _iconImage;
        _iconImage = MaterialIcons.GetIcon(iconName, _currentIconColor, renderSize, IconRenderPreset.DarkOutlined);
        oldImage?.Dispose();
    }

    /// <summary>
    /// Executes measure text width as part of value tile processing.
    /// </summary>
    /// <param name="text">Input value for text.</param>
    /// <param name="font">Input value for font.</param>
    /// <returns>The result of the operation.</returns>
    private static int MeasureTextWidth(string text, Font font) {
        return TextRenderer.MeasureText(text, font).Width;
    }

    /// <summary>
    /// Gets the rendered text bounds for value tile.
    /// </summary>
    /// <param name="label">Input value for label.</param>
    /// <returns>The result of the operation.</returns>
    private static Rectangle GetRenderedTextBounds(Label label) {
        if (label.Width <= 0 || label.Height <= 0 || string.IsNullOrWhiteSpace(label.Text)) {
            return Rectangle.Empty;
        }

        var measured = TextRenderer.MeasureText(label.Text, label.Font, new Size(label.Width, label.Height), TextFormatFlags.NoPadding);
        var textWidth = Math.Min(label.Width, Math.Max(1, measured.Width));
        var textHeight = Math.Min(label.Height, Math.Max(1, measured.Height));

        var x = label.Left;
        var y = label.Top;

        switch (label.TextAlign) {
            case ContentAlignment.TopRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.BottomRight:
                x = label.Right - textWidth;
                break;
            case ContentAlignment.TopLeft:
                break;
            case ContentAlignment.TopCenter:
                break;
            case ContentAlignment.MiddleLeft:
                break;
            case ContentAlignment.MiddleCenter:
                break;
            case ContentAlignment.BottomLeft:
                break;
            case ContentAlignment.BottomCenter:
                break;
            default:
                x = label.Left;
                break;
        }

        switch (label.TextAlign) {
            case ContentAlignment.BottomLeft:
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomRight:
                y = label.Bottom - textHeight;
                break;
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.MiddleRight:
                y = label.Top + (label.Height - textHeight) / 2;
                break;
            case ContentAlignment.TopLeft:
                break;
            case ContentAlignment.TopCenter:
                break;
            case ContentAlignment.TopRight:
                break;
            default:
                y = label.Top;
                break;
        }

        return new Rectangle(x, y, textWidth, textHeight);
    }

    /// <summary>
    /// Executes fit title font and truncate with ellipsis as part of value tile processing.
    /// </summary>
    /// <param name="maxFontSize">Input value for max font size.</param>
    private void FitTitleFontAndTruncateWithEllipsis(float maxFontSize) {
        if (_titleLabel == null) {
            return;
        }

        var fullTitle = string.IsNullOrEmpty(_config.Title) ? _config.Metric.GetDisplayName() : _config.Title;
        var availableWidth = Math.Max(0, _titleLabel.Width - HorizontalFitPadding);
        var currentSize = maxFontSize;

        while (currentSize >= MinTitleFontSize) {
            UpdateFont(_titleLabel, "Segoe UI", currentSize, FontStyle.Bold);
            if (MeasureTextWidth(fullTitle, _titleLabel.Font) <= availableWidth) {
                _titleLabel.Text = fullTitle;
                return;
            }

            currentSize -= 1f;
        }

        // At minimum font size, truncate with ellipsis.
        for (var i = fullTitle.Length - 1; i > 0; i--) {
            var truncated = fullTitle[..i] + "…";
            if (MeasureTextWidth(truncated, _titleLabel.Font) <= availableWidth) {
                _titleLabel.Text = truncated;
                return;
            }
        }

        _titleLabel.Text = "…";
    }

    /// <summary>
    /// Gets the value texts for sizing for value tile.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private List<string> GetValueTextsForSizing() {
        if (_valueLabel == null) {
            return [" ?"];
        }

        var texts = new List<string> { _valueLabel.Text };

        // Only TextOnly mode renders transformed labels inside the value area.
        // In TextAndValue mode transformed labels are shown by _textLabel, and pre-sizing
        // against all scheme labels shrinks numeric values unnecessarily compared to sibling tiles.
        var shouldIncludeTransformedTexts = _config.DisplayMode == ValueTileDisplayMode.TextOnly;
        if (shouldIncludeTransformedTexts && _valueScheme != null && _valueScheme.Stops.Count > 0) {
            texts.AddRange(_valueScheme.Stops
                .Select(s => s.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.Ordinal));
        }

        return texts;
    }

    /// <summary>
    /// Executes fit value font to width as part of value tile processing.
    /// </summary>
    /// <param name="availableWidth">Input value for available width.</param>
    /// <param name="maxFontSize">Input value for max font size.</param>
    private void FitValueFontToWidth(int availableWidth, float maxFontSize) {
        if (_valueLabel == null) {
            return;
        }

        var currentSize = maxFontSize;
        availableWidth = Math.Max(0, availableWidth - HorizontalFitPadding);
        var valueTexts = GetValueTextsForSizing();

        while (currentSize >= MinValueFontSize) {
            var testFont = CreateSafeFont("Segoe UI", currentSize, FontStyle.Bold);
            var maxWidth = valueTexts.Max(text => MeasureTextWidth(text, testFont));
            testFont.Dispose();

            if (maxWidth <= availableWidth) {
                break;
            }

            currentSize -= 1f;
        }

        currentSize = Math.Max(MinValueFontSize, currentSize);
        UpdateFont(_valueLabel, "Segoe UI", currentSize, FontStyle.Bold);
    }


    /// <summary>
    /// Gets the logical font height for value tile.
    /// </summary>
    /// <param name="font">Input value for font.</param>
    /// <returns>The result of the operation.</returns>
    private int GetLogicalFontHeight(Font font) {
        var points = font?.SizeInPoints > 0 ? font.SizeInPoints : SystemFonts.DefaultFont.SizeInPoints;
        var pixels = points * DeviceDpi / 72f;
        return Math.Max(1, (int)Math.Ceiling(pixels));
    }

    /// <summary>
    /// Updates the layout for value tile.
    /// </summary>
    private void UpdateLayout() {
        if (!_initialized || Width <= 0 || Height <= 0) {
            return;
        }

        if (_titleLabel == null || _textLabel == null || _valueLabel == null || _unitLabel == null) {
            return;
        }

        var contentWidth = Width - Padding.Horizontal;
        var contentHeight = Height - Padding.Vertical;

        // Calculate font sizes based on tile dimensions
        var minDimension = Math.Min(contentWidth, contentHeight);

        // Font sizes proportional to tile size
        var titleFontSize = Math.Max(MinTitleFontSize, Math.Min(14f, minDimension * 0.09f));
        var valueFontSize = Math.Max(14f, Math.Min(MaxValueFontSize, minDimension * 0.28f));
        var textFontSize = Math.Max(MinTextFontSize, Math.Min(20f, titleFontSize + 2f));
        var unitFontSize = textFontSize;

        // Update fonts
        UpdateFont(_titleLabel, "Segoe UI", titleFontSize, FontStyle.Bold);
        UpdateFont(_textLabel, "Segoe UI", textFontSize, FontStyle.Bold);
        UpdateFont(_valueLabel, "Segoe UI", valueFontSize, FontStyle.Bold);
        UpdateFont(_unitLabel, "Segoe UI", unitFontSize, FontStyle.Bold);

        // Layout: divide into quadrants with overlap allowed
        var halfWidth = contentWidth / 2;
        var halfHeight = contentHeight / 2;

        // Title: top-left quadrant
        _titleLabel.SetBounds(Padding.Left, Padding.Top, halfWidth + 20, halfHeight);
        FitTitleFontAndTruncateWithEllipsis(titleFontSize);

        // Icon: top-right area — large, proportional to tile
        var iconLogicalSize = Math.Max(36, Math.Min(153, (int)(minDimension * 0.66f)));
        var iconX = Padding.Left + contentWidth - iconLogicalSize - 2;
        var iconY = Padding.Top + 2;
        _iconBounds = _config.ShowIcon
            ? new Rectangle(iconX, iconY, iconLogicalSize, iconLogicalSize)
            : Rectangle.Empty;
        if (_config.ShowIcon) {
            UpdateIcon(iconLogicalSize);
        }

        // Value + Unit: keep enough room for long unit strings (for example "mpsas", "mm/hr")
        var valueWidth = contentWidth;
        var unitWidth = 0;
        if (_config.ShowUnit) {
            var minUnitWidth = Math.Max(40, MeasureTextWidth(_unitLabel.Text, _unitLabel.Font) + 6);
            var preferredUnitWidth = Math.Max((int)(contentWidth * 0.16f), minUnitWidth);
            var minValueWidth = (int)(contentWidth * 0.58f);

            unitWidth = Math.Max(0, Math.Min(preferredUnitWidth, contentWidth - minValueWidth));
            valueWidth = Math.Max(0, contentWidth - unitWidth);
        }
        var showTextAboveValue = _config.DisplayMode == ValueTileDisplayMode.TextAndValue && !string.IsNullOrWhiteSpace(_textLabel.Text);
        var valueTop = Padding.Top + halfHeight - 10;
        var valueHeight = halfHeight + 10;
        const int textToValueGap = 12;

        _valueLabel.SetBounds(Padding.Left, valueTop, valueWidth, valueHeight);
        _valueLabel.TextAlign = ContentAlignment.BottomLeft;
        FitValueFontToWidth(valueWidth, valueFontSize);

        if (showTextAboveValue) {
            var textHeight = Math.Max(20, GetLogicalFontHeight(_textLabel.Font) + 6);
            var valueTextHeight = GetLogicalFontHeight(_valueLabel.Font);
            var valueTextTop = valueTop + valueHeight - valueTextHeight;
            var textTop = Math.Max(Padding.Top, valueTextTop - textHeight - textToValueGap);
            _textLabel.SetBounds(Padding.Left, textTop, valueWidth, textHeight);
            _textLabel.Visible = true;
            _textLabel.BringToFront();
        } else {
            _textLabel.Visible = false;
        }

        // Unit: bottom-right quadrant
        var unitStartX = Padding.Left + valueWidth;
        _unitLabel.SetBounds(unitStartX, Padding.Top + halfHeight, unitWidth, halfHeight);
        _unitLabel.Visible = _config.ShowUnit;

        // Hide unit only when the rendered unit text actually overlaps the icon glyph area.
        var unitTextBounds = GetRenderedTextBounds(_unitLabel);
        if (_unitLabel.Visible && !_iconBounds.IsEmpty && !unitTextBounds.IsEmpty && unitTextBounds.IntersectsWith(_iconBounds)) {
            _unitLabel.Visible = false;
        }

        // Hide title only when rendered title text actually overlaps rendered status/value text.
        _titleLabel.Visible = true;
        var titleTextBounds = GetRenderedTextBounds(_titleLabel);
        var statusTextBounds = _textLabel.Visible ? GetRenderedTextBounds(_textLabel) : Rectangle.Empty;
        var valueTextBounds = GetRenderedTextBounds(_valueLabel);
        var overlapsStatusText = _textLabel.Visible && !titleTextBounds.IsEmpty && !statusTextBounds.IsEmpty && titleTextBounds.IntersectsWith(statusTextBounds);
        var overlapsValueText = !titleTextBounds.IsEmpty && !valueTextBounds.IsEmpty && titleTextBounds.IntersectsWith(valueTextBounds);
        if (overlapsStatusText || overlapsValueText) {
            _titleLabel.Visible = false;
        }

        Invalidate();
    }

    /// <summary>
    /// Creates the context menu for value tile.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private ContextMenuStrip CreateContextMenu() {
        var contextMenu = new ContextMenuStrip {
            ShowImageMargin = true,
            ImageScalingSize = new Size(MenuIconSize, MenuIconSize),
            Cursor = Cursors.Hand
        };

        contextMenu.Opening += (_, _) => {
            RebuildContextMenu(contextMenu);
            ApplyContextMenuTheme(contextMenu);
        };

        RebuildContextMenu(contextMenu);
        InteractiveCursorStyler.Apply(contextMenu.Items);
        return contextMenu;
    }

    /// <summary>
    /// Rebuilds the context menu for value tile.
    /// </summary>
    /// <param name="contextMenu">Input value for context menu.</param>
    private void RebuildContextMenu(ContextMenuStrip contextMenu) {
        contextMenu.Items.Clear();

        contextMenu.Items.Add(CreateMenuItem("Edit Dashboard…", MaterialIcons.CommonEdit, (_, _) => EditDashboardRequested?.Invoke(this)));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(CreateMenuItem("Edit Tile…", MaterialIcons.CommonEdit, (_, _) => EditRequested?.Invoke(this)));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(CreateToggleMenuItem("Show Icon", MaterialIcons.ValueMenuNorthEast, _config.ShowIcon, (_, _) => {
            _config.ShowIcon = !_config.ShowIcon;
            UpdateLayout();
            Invalidate();
            ViewSettingsChanged?.Invoke(this);
        }));
        contextMenu.Items.Add(CreateToggleMenuItem("Show Unit", MaterialIcons.ValueMenuSouthEast, _config.ShowUnit, (_, _) => {
            _config.ShowUnit = !_config.ShowUnit;
            UpdateLayout();
            ViewSettingsChanged?.Invoke(this);
        }));
        contextMenu.Items.Add(new ToolStripSeparator());
        var valueOnlyItem = CreateMenuItem("Value Only", MaterialIcons.ValueDisplay123, (_, _) => {
            _config.DisplayMode = ValueTileDisplayMode.ValueOnly;
            RefreshData();
            ViewSettingsChanged?.Invoke(this);
        });
        valueOnlyItem.Checked = _config.DisplayMode == ValueTileDisplayMode.ValueOnly;
        contextMenu.Items.Add(valueOnlyItem);

        var textOnlyItem = CreateMenuItem("Text Only", MaterialIcons.ValueDisplayAbc, (_, _) => {
            _config.DisplayMode = ValueTileDisplayMode.TextOnly;
            RefreshData();
            ViewSettingsChanged?.Invoke(this);
        });
        textOnlyItem.Checked = _config.DisplayMode == ValueTileDisplayMode.TextOnly;
        contextMenu.Items.Add(textOnlyItem);

        var textAndValueItem = CreateMenuItem("Text + Value", MaterialIcons.ValueDisplayShortText, (_, _) => {
            _config.DisplayMode = ValueTileDisplayMode.TextAndValue;
            RefreshData();
            ViewSettingsChanged?.Invoke(this);
        });
        textAndValueItem.Checked = _config.DisplayMode == ValueTileDisplayMode.TextAndValue;
        contextMenu.Items.Add(textAndValueItem);

        InteractiveCursorStyler.Apply(contextMenu.Items);
    }

    /// <summary>
    /// Applies the context menu theme for value tile.
    /// </summary>
    private void ApplyContextMenuTheme() {
        if (_contextMenu == null) {
            return;
        }

        ApplyContextMenuTheme(_contextMenu);
    }

    /// <summary>
    /// Applies the context menu theme for value tile.
    /// </summary>
    /// <param name="contextMenu">Input value for context menu.</param>
    private void ApplyContextMenuTheme(ContextMenuStrip contextMenu) {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var menuBackground = isLight ? Color.FromArgb(255, 255, 255) : Color.FromArgb(38, 52, 57);
        var menuText = isLight ? Color.FromArgb(66, 66, 66) : Color.FromArgb(240, 240, 240);
        var menuIconColor = isLight ? Color.FromArgb(66, 66, 66) : Color.FromArgb(240, 240, 240);

        _contextMenuRenderer.UpdateTheme();

        contextMenu.RenderMode = ToolStripRenderMode.Professional;
        contextMenu.Renderer = _contextMenuRenderer;
        contextMenu.ShowImageMargin = true;
        contextMenu.BackColor = menuBackground;
        contextMenu.ForeColor = menuText;
        contextMenu.Font = CreateSafeFont("Segoe UI", ContextMenuFontSize, System.Drawing.FontStyle.Regular);
        contextMenu.ImageScalingSize = new Size(MenuIconSize, MenuIconSize);

        ApplyContextMenuItemColors(contextMenu.Items, menuBackground, menuText);
        UpdateContextMenuIcons(contextMenu.Items, menuIconColor);
    }

    /// <summary>
    /// Applies the context menu item colors for value tile.
    /// </summary>
    /// <param name="items">Input value for items.</param>
    /// <param name="backColor">Input value for back color.</param>
    /// <param name="foreColor">Input value for fore color.</param>
    private static void ApplyContextMenuItemColors(ToolStripItemCollection items, Color backColor, Color foreColor) {
        foreach (ToolStripItem item in items) {
            item.BackColor = backColor;
            item.ForeColor = foreColor;
            if (item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0) {
                ApplyContextMenuItemColors(menuItem.DropDownItems, backColor, foreColor);
            }
        }
    }

    /// <summary>
    /// Updates the context menu icons for value tile.
    /// </summary>
    /// <param name="items">Input value for items.</param>
    /// <param name="iconColor">Input value for icon color.</param>
    private static void UpdateContextMenuIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is not ToolStripMenuItem menuItem) {
                continue;
            }
            if (menuItem.Tag is string iconName) {
                menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize, IconRenderPreset.DarkOutlined);
            }
            if (menuItem.DropDownItems.Count > 0) {
                UpdateContextMenuIcons(menuItem.DropDownItems, iconColor);
            }
        }
    }

    /// <summary>
    /// Creates the menu item for value tile.
    /// </summary>
    /// <param name="text">Input value for text.</param>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="onClick">Input value for on click.</param>
    /// <returns>The result of the operation.</returns>
    private static ToolStripMenuItem CreateMenuItem(string text, string iconName, EventHandler onClick) {
        var iconColor = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(66, 66, 66)
            : Color.FromArgb(240, 240, 240);

        var item = new ToolStripMenuItem(text) {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize, IconRenderPreset.DarkOutlined),
            ImageScaling = ToolStripItemImageScaling.None,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = iconName
        };
        item.Click += onClick;
        return item;
    }

    /// <summary>
    /// Creates the toggle menu item for value tile.
    /// </summary>
    /// <param name="text">Input value for text.</param>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="isChecked">Input value for is checked.</param>
    /// <param name="onClick">Input value for on click.</param>
    /// <returns>The result of the operation.</returns>
    private static ToolStripMenuItem CreateToggleMenuItem(string text, string iconName, bool isChecked, EventHandler onClick) {
        var item = CreateMenuItem(text, iconName, onClick);
        item.Checked = isChecked;
        return item;
    }

    #endregion Private Methods

}
