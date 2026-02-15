using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ColorScheme = SafetyMonitorView.Models.ColorScheme;

namespace SafetyMonitorView.Controls;

public class ValueTile : Panel {

    #region Private Fields

    private readonly ColorSchemeService _colorSchemeService;
    private readonly ValueTileConfig _config;
    private readonly DataService _dataService;
    private ColorScheme? _colorScheme;
    private Color _currentIconColor = Color.Transparent;
    private double? _currentValue;
    private PictureBox? _iconBox;
    private bool _initialized;
    private Label? _titleLabel;
    private Label? _unitLabel;
    private Label? _valueLabel;

    #endregion Private Fields

    #region Public Constructors

    public ValueTile(ValueTileConfig config, DataService dataService) {
        _config = config;
        _dataService = dataService;
        _colorSchemeService = new ColorSchemeService();

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
                _valueLabel.Text = _currentValue.Value.ToString($"F{_config.DecimalPlaces}");
                ApplyColorScheme();
            } else {
                _valueLabel.Text = "—";
                ResetColors();
            }
        } else {
            _valueLabel.Text = "N/A";
            ResetColors();
        }
    }

    public void UpdateTheme() {
        UpdateLayout();
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
        var secondaryColor = isLight ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180);

        _titleLabel.ForeColor = secondaryColor;
        _unitLabel.ForeColor = secondaryColor;

        SetIconColor(primaryColor);
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
            Text = "—",
            AutoSize = false,
            TextAlign = ContentAlignment.BottomLeft,
            BackColor = Color.Transparent
        };

        // Unit: small, bottom-right
        _unitLabel = new Label {
            Text = _config.Metric.GetUnit(),
            AutoSize = false,
            TextAlign = ContentAlignment.BottomRight,
            BackColor = Color.Transparent
        };

        Controls.Add(_titleLabel);
        Controls.Add(_iconBox);
        Controls.Add(_valueLabel);
        Controls.Add(_unitLabel);

        // Subscribe to resize for dynamic font scaling
        Resize += OnTileResize;
    }

    private void LoadColorScheme() {
        if (string.IsNullOrEmpty(_config.ColorSchemeName)) {
            _colorScheme = null;
            return;
        }
        var schemes = _colorSchemeService.LoadSchemes();
        _colorScheme = schemes.FirstOrDefault(s => s.Name == _config.ColorSchemeName);
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
        var titleFontSize = Math.Max(8f, Math.Min(14f, minDimension * 0.09f));
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

        // Icon: top-right area — large, proportional to tile
        var iconLogicalSize = Math.Max(24, Math.Min(102, (int)(minDimension * 0.44f)));
        var iconX = Padding.Left + contentWidth - iconLogicalSize - 2;
        var iconY = Padding.Top + 2;
        _iconBox.SetBounds(iconX, iconY, iconLogicalSize, iconLogicalSize);
        UpdateIcon(iconLogicalSize);

        // Value: bottom-left, takes more space
        _valueLabel.SetBounds(Padding.Left, Padding.Top + halfHeight - 10, (int)(contentWidth * 0.75), halfHeight + 10);

        // Unit: bottom-right quadrant
        _unitLabel.SetBounds(Padding.Left + halfWidth, Padding.Top + halfHeight, halfWidth, halfHeight);
    }

    #endregion Private Methods

}
