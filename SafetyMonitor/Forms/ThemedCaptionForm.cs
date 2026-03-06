using System.Runtime.InteropServices;
using SafetyMonitor.Services;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents themed caption form and encapsulates its related behavior and state.
/// </summary>
public class ThemedCaptionForm : Form {
    private const int CsDropshadow = 0x20000;
    private readonly ThemeWindowControlPalette _windowControlPalette = AppColorizationService.Instance.GetWindowControlPalette();
    private bool _useCustomTitleBar;
    private bool _isCloseButtonHovered;
    private bool _isCloseButtonPressed;
    private Panel? _framePanel;
    private TableLayoutPanel? _rootLayoutPanel;
    private Panel? _titleBarPanel;
    private TableLayoutPanel? _titleBarLayoutPanel;
    private Panel? _contentHostPanel;
    private PictureBox? _titleBarIcon;
    private Label? _titleBarLabel;
    private Button? _titleBarCloseButton;
    private Padding _originalFormPadding;

    protected override CreateParams CreateParams {
        get {
            var createParams = base.CreateParams;
            createParams.ClassStyle |= CsDropshadow;
            return createParams;
        }
    }

    /// <summary>
    /// Executes on handle created as part of themed caption form processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        ApplyWindowCaptionTheme();
    }

    /// <summary>
    /// Executes on shown as part of themed caption form processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnShown(EventArgs e) {
        base.OnShown(e);
        ApplyWindowCaptionTheme();
    }

    /// <summary>
    /// Executes on back color changed as part of themed caption form processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnBackColorChanged(EventArgs e) {
        base.OnBackColorChanged(e);
        ApplyWindowCaptionTheme();
    }

    /// <summary>
    /// Executes on text changed as part of themed caption form processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnTextChanged(EventArgs e) {
        base.OnTextChanged(e);
        if (_titleBarLabel != null) {
            _titleBarLabel.Text = Text;
        }
    }

    /// <summary>
    /// Executes on dpi changed as part of themed caption form processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnDpiChanged(DpiChangedEventArgs e) {
        base.OnDpiChanged(e);
        UpdateCustomTitleBarScaling();
        UpdateContentHostPadding();
    }

    /// <summary>
    /// Applies the window caption theme for themed caption form.
    /// </summary>
    protected void ApplyWindowCaptionTheme() {
        if (!IsHandleCreated || IsDisposed) {
            return;
        }

        var captionColor = GetPrimaryCaptionColor();
        var isDarkTheme = IsDarkColor(captionColor);
        var applied = WindowCaptionThemeService.TryApplyWin11Theme(Handle, captionColor, isDarkTheme);

        if (applied) {
            return;
        }

        EnsureCustomTitleBar();
        UpdateFallbackBackgroundColors();
        UpdateCustomTitleBarTheme(captionColor);
    }

    /// <summary>
    /// Ensures the custom title bar for themed caption form.
    /// </summary>
    private void EnsureCustomTitleBar() {
        if (_useCustomTitleBar) {
            return;
        }

        _useCustomTitleBar = true;
        _originalFormPadding = Padding;
        FormBorderStyle = FormBorderStyle.None;
        Padding = Padding.Empty;

        var existingControls = Controls.Cast<Control>().ToArray();

        _framePanel = new Panel {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(1),
            BackColor = GetFrameBorderColor()
        };

        _rootLayoutPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = BackColor
        };
        _rootLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
        _rootLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _contentHostPanel = new Panel {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = _originalFormPadding,
            BackColor = BackColor
        };

        Controls.Add(_rootLayoutPanel);
        _rootLayoutPanel.Controls.Add(_framePanel, 0, 1);
        _framePanel.Controls.Add(_contentHostPanel);

        foreach (var control in existingControls) {
            Controls.Remove(control);
            _contentHostPanel.Controls.Add(control);
        }

        _titleBarPanel = new Panel {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(10, 0, 0, 0)
        };

        _titleBarLayoutPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            ColumnCount = 3,
            RowCount = 1
        };
        _titleBarLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18f));
        _titleBarLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _titleBarLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
        _titleBarLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _titleBarIcon = new PictureBox {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _titleBarLabel = new Label {
            Dock = DockStyle.Fill,
            Text = Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            AutoSize = false,
            Margin = new Padding(6, 0, 0, 0),
            Padding = Padding.Empty,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _titleBarCloseButton = new Button {
            Dock = DockStyle.Right,
            Text = "✕",
            FlatStyle = FlatStyle.Flat,
            Width = 42,
            Cursor = Cursors.Hand,
            TabStop = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            UseVisualStyleBackColor = false
        };
        _titleBarCloseButton.FlatAppearance.BorderSize = 0;
        _titleBarCloseButton.MouseEnter += (_, _) => {
            _isCloseButtonHovered = true;
            UpdateCloseButtonVisualState();
        };
        _titleBarCloseButton.MouseLeave += (_, _) => {
            _isCloseButtonHovered = false;
            _isCloseButtonPressed = false;
            UpdateCloseButtonVisualState();
        };
        _titleBarCloseButton.MouseDown += (_, e) => {
            if (e.Button != MouseButtons.Left) {
                return;
            }

            _isCloseButtonPressed = true;
            UpdateCloseButtonVisualState();
        };
        _titleBarCloseButton.MouseUp += (_, e) => {
            if (e.Button != MouseButtons.Left) {
                return;
            }

            _isCloseButtonPressed = false;
            UpdateCloseButtonVisualState();
        };
        _titleBarCloseButton.Click += (_, _) => Close();

        _titleBarLayoutPanel.Controls.Add(_titleBarIcon, 0, 0);
        _titleBarLayoutPanel.Controls.Add(_titleBarLabel, 1, 0);
        _titleBarLayoutPanel.Controls.Add(_titleBarCloseButton, 2, 0);
        _titleBarPanel.Controls.Add(_titleBarLayoutPanel);

        _rootLayoutPanel.Controls.Add(_titleBarPanel, 0, 0);

        AttachDragHandlers(_titleBarPanel);
        AttachDragHandlers(_titleBarIcon);
        AttachDragHandlers(_titleBarLabel);

        if (Icon != null) {
            _titleBarIcon.Image = Icon.ToBitmap();
        }

        UpdateCustomTitleBarScaling();
        UpdateFallbackBackgroundColors();
        UpdateCustomTitleBarTheme(GetPrimaryCaptionColor());
    }

    /// <summary>
    /// Updates the fallback background colors for themed caption form.
    /// </summary>
    private void UpdateFallbackBackgroundColors() {
        if (!_useCustomTitleBar) {
            return;
        }

        if (_rootLayoutPanel != null) {
            _rootLayoutPanel.BackColor = BackColor;
        }

        if (_framePanel != null) {
            _framePanel.BackColor = GetFrameBorderColor();
        }

        if (_contentHostPanel != null) {
            _contentHostPanel.BackColor = BackColor;
        }
    }

    /// <summary>
    /// Updates the custom title bar theme for themed caption form.
    /// </summary>
    /// <param name="panelBackColor">Input value for panel back color.</param>
    private void UpdateCustomTitleBarTheme(Color panelBackColor) {
        if (_titleBarPanel == null || _titleBarLabel == null || _titleBarCloseButton == null) {
            return;
        }

        var textColor = GetPrimaryCaptionTextColor();

        _titleBarPanel.BackColor = panelBackColor;
        _titleBarLabel.ForeColor = textColor;
        _titleBarCloseButton.FlatAppearance.MouseOverBackColor = _windowControlPalette.CloseButtonHoverBackground;
        _titleBarCloseButton.FlatAppearance.MouseDownBackColor = _windowControlPalette.CloseButtonPressedBackground;
        UpdateCloseButtonVisualState();

        _framePanel?.Invalidate();
    }

    /// <summary>
    /// Updates the close button visual state for themed caption form.
    /// </summary>
    private void UpdateCloseButtonVisualState() {
        if (_titleBarCloseButton == null || _titleBarPanel == null) {
            return;
        }

        if (_isCloseButtonPressed) {
            _titleBarCloseButton.BackColor = _windowControlPalette.CloseButtonPressedBackground;
            _titleBarCloseButton.ForeColor = _windowControlPalette.CloseButtonActiveForeground;
            return;
        }

        if (_isCloseButtonHovered) {
            _titleBarCloseButton.BackColor = _windowControlPalette.CloseButtonHoverBackground;
            _titleBarCloseButton.ForeColor = _windowControlPalette.CloseButtonActiveForeground;
            return;
        }

        _titleBarCloseButton.BackColor = _titleBarPanel.BackColor;
        _titleBarCloseButton.ForeColor = GetPrimaryCaptionTextColor();
    }

    /// <summary>
    /// Updates the custom title bar scaling for themed caption form.
    /// </summary>
    private void UpdateCustomTitleBarScaling() {
        if (!_useCustomTitleBar || _titleBarPanel == null || _titleBarIcon == null || _titleBarCloseButton == null || _titleBarLabel == null) {
            return;
        }

        var scale = DeviceDpi / 96f;
        var shouldReduceHeaderVisuals = scale > 1.0f;
        var iconBaseSize = shouldReduceHeaderVisuals ? 15f : 18f;
        var titleFontSize = shouldReduceHeaderVisuals ? 8.5f : 9f;
        var titleBarHeight = (int)Math.Round(34 * scale);
        if (_rootLayoutPanel != null && _rootLayoutPanel.RowStyles.Count > 0) {
            _rootLayoutPanel.RowStyles[0].Height = titleBarHeight;
        }
        _titleBarPanel.Height = titleBarHeight;
        var iconSize = (int)Math.Round(iconBaseSize * scale);
        var closeButtonWidth = (int)Math.Round(42 * scale);
        if (_titleBarLayoutPanel != null && _titleBarLayoutPanel.ColumnStyles.Count >= 3) {
            _titleBarLayoutPanel.ColumnStyles[0].Width = iconSize;
            _titleBarLayoutPanel.ColumnStyles[2].Width = closeButtonWidth;
        }
        _titleBarIcon.Width = iconSize;
        _titleBarCloseButton.Width = closeButtonWidth;
        _titleBarLabel.Font = new Font("Segoe UI", titleFontSize * scale, FontStyle.Bold);
        _titleBarIcon.Padding = Padding.Empty;
        _titleBarLabel.Padding = Padding.Empty;
    }

    /// <summary>
    /// Updates the content host padding for themed caption form.
    /// </summary>
    private void UpdateContentHostPadding() {
        if (!_useCustomTitleBar || _contentHostPanel == null) {
            return;
        }

        _contentHostPanel.Padding = _originalFormPadding;
    }

    /// <summary>
    /// Executes attach drag handlers as part of themed caption form processing.
    /// </summary>
    /// <param name="control">Input value for control.</param>
    private void AttachDragHandlers(Control? control) {
        if (control == null) {
            return;
        }

        control.MouseDown += (_, e) => {
            if (e.Button != MouseButtons.Left) {
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WmNclbuttondown, Htcaption, 0);
        };
    }

    /// <summary>
    /// Determines whether is dark color for themed caption form.
    /// </summary>
    /// <param name="color">Input value for color.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool IsDarkColor(Color color) {
        var luminance = (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
        return luminance < 140;
    }

    /// <summary>
    /// Gets the primary caption color for themed caption form.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Color GetPrimaryCaptionColor() {
        var appSettings = new AppSettingsService().LoadSettings();
        var schemeName = AppColorizationService.Instance.NormalizeMaterialSchemeName(appSettings.MaterialColorScheme);
        return AppColorizationService.Instance.GetPrimaryActionColor(schemeName);
    }

    /// <summary>
    /// Gets the primary caption text color for themed caption form.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Color GetPrimaryCaptionTextColor() {
        // Primary action buttons use white text for Confirm/Save in ThemedButtonStyler.
        // Keep caption text/close glyph aligned with that visual contract.
        return Color.White;
    }

    /// <summary>
    /// Gets the frame border color for themed caption form.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Color GetFrameBorderColor() {
        var appSettings = new AppSettingsService().LoadSettings();
        var neutral = AppColorizationService.Instance.GetNeutralPalette(!appSettings.IsDarkTheme);
        return appSettings.IsDarkTheme
            ? ControlPaint.Light(neutral.Border, 0.15f)
            : ControlPaint.Dark(neutral.Border, 0.08f);
    }

    private const int WmNclbuttondown = 0xA1;
    private const int Htcaption = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
