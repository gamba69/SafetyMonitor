using MaterialSkin;
using SafetyMonitorView.Forms;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ColorScheme = SafetyMonitorView.Models.ColorScheme;

namespace SafetyMonitorView.Controls;

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
    private ValueScheme? _valueScheme;
    private Color _currentIconColor = Color.Transparent;
    private double? _currentValue;
    private PictureBox? _iconBox;
    private bool _initialized;
    private Label? _titleLabel;
    private Label? _unitLabel;
    private Label? _valueLabel;
    private const float MinTitleFontSize = 10f;
    private const float MinValueFontSize = 10f;
    private const int HorizontalFitPadding = 8;

    #endregion Private Fields

    #region Public Events

    public event Action<ValueTile>? EditRequested;
    public event Action<ValueTile>? ViewSettingsChanged;

    #endregion Public Events

    #region Public Constructors

    public ValueTile(ValueTileConfig config, DataService dataService) {
        _config = config;
        _dataService = dataService;
        _colorSchemeService = new ColorSchemeService();
        _valueSchemeService = new ValueSchemeService();

        Dock = DockStyle.Fill;
        Padding = new Padding(8);
        BorderStyle = BorderStyle.FixedSingle;
        DoubleBuffered = true;

        // Set a valid font to prevent GDI+ errors during auto-scaling
        Font = SystemFonts.DefaultFont;
    }

    #endregion Public Constructors

    #region Public Methods

    public void RefreshData() {
        if (_valueLabel == null) {
            return;
        }

        var latestData = _dataService.GetLatestData();
        if (latestData != null) {
            _currentValue = _config.Metric.GetValue(latestData);
            if (_currentValue.HasValue) {
                var transformedText = _valueScheme?.GetText(_currentValue.Value);
                if (transformedText != null) {
                    _valueLabel.Text = transformedText;
                } else {
                    _valueLabel.Text = MetricDisplaySettingsStore.FormatMetricValue(_config.Metric, _currentValue.Value);
                }
                ApplyColorScheme();
            } else {
                _valueLabel.Text = " ?";
                ResetColors();
            }
        } else {
            _valueLabel.Text = " ?";
            ResetColors();
        }

        // Value text can change on every refresh; recalculate layout/font fitting to prevent clipping.
        UpdateLayout();
    }

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
    /// Prevents MaterialSkinManager font propagation from overwriting tile fonts.
    /// </summary>
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        if (_initialized) {
            UpdateLayout();
        }
    }

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

    #region Private Methods

    /// <summary>
    /// Safely creates a font with fallback to system default if the requested font is not available.
    /// </summary>
    private static Font CreateSafeFont(string familyName, float emSize, FontStyle style = FontStyle.Regular) {
        try {
            var font = new Font(familyName, emSize, style);
            return font;
        } catch {
            // Fallback to system default font
            return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
        }
    }

    private static void UpdateFont(Label label, string fontFamily, float size, FontStyle style) {
        var newFont = CreateSafeFont(fontFamily, size, style);
        var oldFont = label.Font;
        label.Font = newFont;
        if (oldFont != null && oldFont != newFont) {
            oldFont.Dispose();
        }
    }

    /// <summary>
    /// Applies theme-based background and secondary element colors (title, unit, icon).
    /// These colors are always the same regardless of whether data is present.
    /// </summary>
    private void ApplyThemeColors() {
        if (_titleLabel == null || _valueLabel == null || _unitLabel == null || _iconBox == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        _titleLabel.BackColor = Color.Transparent;
        _valueLabel.BackColor = Color.Transparent;
        _unitLabel.BackColor = Color.Transparent;
        _iconBox.BackColor = Color.Transparent;

        var primaryColor = isLight ? Color.Black : Color.White;
        _titleLabel.ForeColor = primaryColor;
        _unitLabel.ForeColor = primaryColor;

        if (_currentValue.HasValue && _iconColorScheme != null) {
            SetIconColor(_iconColorScheme.GetColor(_currentValue.Value));
        } else {
            SetIconColor(primaryColor);
        }
    }

    private void ApplyColorScheme() {
        if (_titleLabel == null || _valueLabel == null || _unitLabel == null || _iconBox == null) {
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
            _valueLabel.ForeColor = isLight ? Color.Black : Color.White;
        }
    }

    private static Color GetContrastColor(Color bg) {
        var brightness = (bg.R * 299 + bg.G * 587 + bg.B * 114) / 1000;
        return brightness > 128 ? Color.Black : Color.White;
    }

    private void InitializeUI() {
        // Title: small, top-left
        _titleLabel = new Label {
            Text = string.IsNullOrEmpty(_config.Title) ? _config.Metric.GetDisplayName() : _config.Title,
            AutoSize = false,
            TextAlign = ContentAlignment.TopLeft,
            BackColor = Color.Transparent
        };

        // Icon: Material Design vector icon, top-right
        _iconBox = new PictureBox {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Visible = _config.ShowIcon
        };

        // Value: large, bottom-left
        _valueLabel = new Label {
            Text = " ?",
            AutoSize = false,
            TextAlign = ContentAlignment.BottomLeft,
            BackColor = Color.Transparent
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
        Controls.Add(_iconBox);
        Controls.Add(_valueLabel);
        Controls.Add(_unitLabel);

        _contextMenu = CreateContextMenu();
        ContextMenuStrip = _contextMenu;

        // Subscribe to resize for dynamic font scaling
        Resize += OnTileResize;
    }

    private void LoadColorScheme() {
        var schemes = _colorSchemeService.LoadSchemes();
        _colorScheme = string.IsNullOrEmpty(_config.ColorSchemeName)
            ? null
            : schemes.FirstOrDefault(s => s.Name == _config.ColorSchemeName);
        _iconColorScheme = string.IsNullOrEmpty(_config.IconColorSchemeName)
            ? null
            : schemes.FirstOrDefault(s => s.Name == _config.IconColorSchemeName);
        var valueSchemes = _valueSchemeService.LoadSchemes();
        _valueScheme = string.IsNullOrEmpty(_config.ValueSchemeName)
            ? null
            : valueSchemes.FirstOrDefault(s => s.Name == _config.ValueSchemeName);
    }

    private void OnTileResize(object? sender, EventArgs e) {
        UpdateLayout();
    }

    private void ResetColors() {
        if (_titleLabel == null || _valueLabel == null || _unitLabel == null || _iconBox == null) {
            return;
        }

        // Apply consistent theme colors for all elements
        ApplyThemeColors();

        // Value label uses primary theme color when no data / no scheme
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        _valueLabel.ForeColor = isLight ? Color.Black : Color.White;
    }

    private void SetIconColor(Color color) {
        if (_iconBox == null || color == _currentIconColor) {
            return;
        }

        _currentIconColor = color;
        var iconSize = Math.Max(16, _iconBox.Width);
        UpdateIcon(iconSize);
    }

    private void UpdateIcon(int logicalSize) {
        if (_iconBox == null) {
            return;
        }

        // Render at physical pixel size for crisp display at any DPI scaling.
        // DeviceDpi is the actual monitor DPI (96 = 100%, 120 = 125%, 144 = 150%).
        var scaleFactor = DeviceDpi / 96f;
        var renderSize = Math.Max(16, (int)(logicalSize * scaleFactor));

        var iconName = MaterialIcons.GetMetricIconName(_config.Metric);
        var oldImage = _iconBox.Image;
        _iconBox.Image = MaterialIcons.GetIcon(iconName, _currentIconColor, renderSize);
        oldImage?.Dispose();
    }

    private static int MeasureTextWidth(string text, Font font) {
        return TextRenderer.MeasureText(text, font).Width;
    }

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
            var truncated = fullTitle[..i] + "...";
            if (MeasureTextWidth(truncated, _titleLabel.Font) <= availableWidth) {
                _titleLabel.Text = truncated;
                return;
            }
        }

        _titleLabel.Text = "...";
    }

    private List<string> GetValueTextsForSizing() {
        if (_valueLabel == null) {
            return [" ?"];
        }

        var texts = new List<string> { _valueLabel.Text };
        if (_valueScheme != null && _valueScheme.Stops.Count > 0) {
            texts.AddRange(_valueScheme.Stops
                .Select(s => s.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.Ordinal));
        }

        return texts;
    }

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

    private void UpdateLayout() {
        if (!_initialized || Width <= 0 || Height <= 0) {
            return;
        }

        if (_titleLabel == null || _valueLabel == null || _unitLabel == null || _iconBox == null) {
            return;
        }

        var contentWidth = Width - Padding.Horizontal;
        var contentHeight = Height - Padding.Vertical;

        // Calculate font sizes based on tile dimensions
        var minDimension = Math.Min(contentWidth, contentHeight);

        // Font sizes proportional to tile size
        var titleFontSize = Math.Max(MinTitleFontSize, Math.Min(14f, minDimension * 0.09f));
        var valueFontSize = Math.Max(14f, Math.Min(48f, minDimension * 0.28f));
        var unitFontSize = Math.Max(8f, Math.Min(14f, minDimension * 0.09f));

        // Update fonts
        UpdateFont(_titleLabel, "Segoe UI", titleFontSize, FontStyle.Bold);
        UpdateFont(_valueLabel, "Segoe UI", valueFontSize, FontStyle.Bold);
        UpdateFont(_unitLabel, "Segoe UI", unitFontSize, FontStyle.Regular);

        // Layout: divide into quadrants with overlap allowed
        var halfWidth = contentWidth / 2;
        var halfHeight = contentHeight / 2;

        // Title: top-left quadrant
        _titleLabel.SetBounds(Padding.Left, Padding.Top, halfWidth + 20, halfHeight);
        FitTitleFontAndTruncateWithEllipsis(titleFontSize);

        // Icon: top-right area — large, proportional to tile
        var iconLogicalSize = Math.Max(24, Math.Min(102, (int)(minDimension * 0.44f)));
        var iconX = Padding.Left + contentWidth - iconLogicalSize - 2;
        var iconY = Padding.Top + 2;
        _iconBox.SetBounds(iconX, iconY, iconLogicalSize, iconLogicalSize);
        UpdateIcon(iconLogicalSize);

        // Value: bottom-left, takes more space
        var valueWidth = _config.ShowUnit ? (int)(contentWidth * 0.84) : contentWidth;
        _valueLabel.SetBounds(Padding.Left, Padding.Top + halfHeight - 10, valueWidth, halfHeight + 10);
        FitValueFontToWidth(valueWidth, valueFontSize);

        // Unit: bottom-right quadrant
        var unitStartX = Padding.Left + valueWidth;
        var unitWidth = Math.Max(0, contentWidth - valueWidth);
        _unitLabel.SetBounds(unitStartX, Padding.Top + halfHeight, unitWidth, halfHeight);
        _unitLabel.Visible = _config.ShowUnit;
    }

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

    private void RebuildContextMenu(ContextMenuStrip contextMenu) {
        contextMenu.Items.Clear();

        contextMenu.Items.Add(CreateMenuItem("Edit Tile...", MaterialIcons.CommonEdit, (_, _) => EditRequested?.Invoke(this)));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(CreateToggleMenuItem("Show Icon", MaterialIcons.ValueMenuNorthEast, _config.ShowIcon, (_, _) => {
            _config.ShowIcon = !_config.ShowIcon;
            _iconBox?.Visible = _config.ShowIcon;
            ViewSettingsChanged?.Invoke(this);
        }));
        contextMenu.Items.Add(CreateToggleMenuItem("Show Unit", MaterialIcons.ValueMenuSouthEast, _config.ShowUnit, (_, _) => {
            _config.ShowUnit = !_config.ShowUnit;
            UpdateLayout();
            ViewSettingsChanged?.Invoke(this);
        }));

        InteractiveCursorStyler.Apply(contextMenu.Items);
    }

    private void ApplyContextMenuTheme() {
        if (_contextMenu == null) {
            return;
        }

        ApplyContextMenuTheme(_contextMenu);
    }

    private void ApplyContextMenuTheme(ContextMenuStrip contextMenu) {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var menuBackground = isLight ? Color.FromArgb(255, 255, 255) : Color.FromArgb(38, 52, 57);
        var menuText = isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);
        var menuIconColor = isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);

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

    private static void ApplyContextMenuItemColors(ToolStripItemCollection items, Color backColor, Color foreColor) {
        foreach (ToolStripItem item in items) {
            item.BackColor = backColor;
            item.ForeColor = foreColor;
            if (item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0) {
                ApplyContextMenuItemColors(menuItem.DropDownItems, backColor, foreColor);
            }
        }
    }

    private static void UpdateContextMenuIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is not ToolStripMenuItem menuItem) {
                continue;
            }
            if (menuItem.Tag is string iconName) {
                menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize);
            }
            if (menuItem.DropDownItems.Count > 0) {
                UpdateContextMenuIcons(menuItem.DropDownItems, iconColor);
            }
        }
    }

    private static ToolStripMenuItem CreateMenuItem(string text, string iconName, EventHandler onClick) {
        var iconColor = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(33, 33, 33)
            : Color.FromArgb(240, 240, 240);

        var item = new ToolStripMenuItem(text) {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize),
            ImageScaling = ToolStripItemImageScaling.None,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = iconName
        };
        item.Click += onClick;
        return item;
    }

    private static ToolStripMenuItem CreateToggleMenuItem(string text, string iconName, bool isChecked, EventHandler onClick) {
        var item = CreateMenuItem(text, iconName, onClick);
        item.Checked = isChecked;
        return item;
    }

    #endregion Private Methods

}
