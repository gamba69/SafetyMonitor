using MaterialSkin;
using SafetyMonitor.Services;

namespace SafetyMonitor.Forms;

/// <summary>
/// Custom MessageBox with theme support (light/dark).
/// DPI-aware layout that works correctly at 100%, 125%, 150% and other scaling levels.
/// Properly compensates for ThemedCaptionForm's custom title bar on Win10.
/// </summary>
public class ThemedMessageBox : ThemedCaptionForm {
    #region Private Fields

    private readonly MessageBoxButtons _buttons;
    private readonly string _caption;
    private readonly MessageBoxIcon _icon;
    private readonly string _message;

    private FlowLayoutPanel _buttonPanel = null!;
    private PictureBox _iconPicture = null!;
    private Label _messageLabel = null!;

    /// <summary>
    /// The content area size calculated during InitializeComponent (physical pixels).
    /// Used to recalculate ClientSize after ThemedCaptionForm installs its custom title bar.
    /// </summary>
    private Size _contentAreaSize;

    #endregion Private Fields

    #region Private Constructors

    private ThemedMessageBox(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) {
        _message = message;
        _caption = caption;
        _buttons = buttons;
        _icon = icon;

        InitializeComponent();

        var iconName = _icon switch {
            MessageBoxIcon.Warning => MaterialIcons.MessageBoxWarningFilled,
            MessageBoxIcon.Error => MaterialIcons.MessageBoxErrorFilled,
            MessageBoxIcon.Question => MaterialIcons.MessageBoxQuestionFilled,
            _ => MaterialIcons.MessageBoxInfoFilled,
        };
        FormIconHelper.Apply(this, iconName);
        ApplyTheme();
    }

    #endregion Private Constructors

    #region Public Methods

    public static DialogResult Show(string message) {
        return Show(message, "", MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    public static DialogResult Show(string message, string caption) {
        return Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    public static DialogResult Show(string message, string caption, MessageBoxButtons buttons) {
        return Show(message, caption, buttons, MessageBoxIcon.None);
    }

    public static DialogResult Show(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) {
        using var dialog = new ThemedMessageBox(message, caption, buttons, icon);
        dialog.StartPosition = FormStartPosition.CenterScreen;
        return dialog.ShowDialog();
    }

    public static DialogResult Show(IWin32Window? owner, string message) {
        return Show(owner, message, "", MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    public static DialogResult Show(IWin32Window? owner, string message, string caption) {
        return Show(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
    }

    public static DialogResult Show(IWin32Window? owner, string message, string caption, MessageBoxButtons buttons) {
        return Show(owner, message, caption, buttons, MessageBoxIcon.None);
    }

    public static DialogResult Show(IWin32Window? owner, string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) {
        using var dialog = new ThemedMessageBox(message, caption, buttons, icon);
        if (owner == null) {
            dialog.StartPosition = FormStartPosition.CenterScreen;
            return dialog.ShowDialog();
        }

        return dialog.ShowDialog(owner);
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        AdjustSizeForTitleBar();
    }

    protected override void OnShown(EventArgs e) {
        base.OnShown(e);
        RenderIcon();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _iconPicture?.Image?.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion Protected Methods

    #region Private Methods

    /// <summary>
    /// Scale a logical pixel value (designed at 96 DPI) to physical pixels.
    /// Uses the desktop DC because DeviceDpi is not available before handle creation.
    /// </summary>
    private static int Dpi(int logicalPixels) {
        using var g = Graphics.FromHwnd(IntPtr.Zero);
        return Math.Max(1, (int)Math.Round(logicalPixels * g.DpiX / 96.0));
    }

    /// <summary>
    /// After ThemedCaptionForm's base.OnHandleCreated installs the custom title bar
    /// (Win10 fallback), the FormBorderStyle changes to None. The window shrinks because
    /// OS chrome is removed, but the custom title bar still needs space inside the
    /// client area. This method compensates by enlarging the form.
    /// On Win11 (native DWM theming), FormBorderStyle stays FixedDialog and no adjustment
    /// is needed — the OS title bar lives outside the client area.
    /// </summary>
    private void AdjustSizeForTitleBar() {
        if (FormBorderStyle != FormBorderStyle.None) {
            return;
        }

        var scale = DeviceDpi / 96.0;
        var titleBarHeight = (int)Math.Round(34 * scale);
        var frameBorder = 2;

        ClientSize = new Size(
            _contentAreaSize.Width + frameBorder,
            _contentAreaSize.Height + titleBarHeight + frameBorder);
    }

    /// <summary>
    /// Render the message box icon at the actual physical size of the PictureBox.
    /// Called in OnShown when auto-scaling and layout are complete.
    /// </summary>
    private void RenderIcon() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        _iconPicture.Image?.Dispose();
        _iconPicture.Image = MaterialIcons.GetMessageBoxIcon(_icon, isLight, _iconPicture.Width);
        _iconPicture.Visible = _iconPicture.Image != null;
    }

    private void InitializeComponent() {
        Text = _caption;
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;

        var pad = Dpi(20);
        Padding = new Padding(pad);

        var normalFont = new Font("Segoe UI", 10f);
        var hasIcon = _icon != MessageBoxIcon.None;
        var iconSize = Dpi(48);
        var iconGap = Dpi(14);
        var buttonHeight = Dpi(35);
        var buttonMinWidth = Dpi(110);
        var buttonSpacing = Dpi(8);

        // --- Main TableLayoutPanel (Dock.Fill works with ThemedCaptionForm's _contentHostPanel) ---
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        var iconColumnWidth = hasIcon ? iconSize + iconGap : 0;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, iconColumnWidth));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // --- Icon ---
        _iconPicture = new PictureBox {
            Size = new Size(iconSize, iconSize),
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, Dpi(2), 0, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Visible = hasIcon,
        };
        mainLayout.Controls.Add(_iconPicture, 0, 0);

        // --- Measure message text ---
        var workingArea = (Screen.PrimaryScreen ?? Screen.AllScreens[0]).WorkingArea;
        var maxLabelWidth = Math.Max(Dpi(180), Math.Min(Dpi(420), (int)(workingArea.Width * 0.35) - pad * 2 - iconColumnWidth));

        var textSize = TextRenderer.MeasureText(
            _message, normalFont,
            new Size(maxLabelWidth, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        var labelWidth = Math.Min(textSize.Width + Dpi(4), maxLabelWidth);
        var labelHeight = textSize.Height + Dpi(4);

        _messageLabel = new Label {
            Text = _message,
            Font = normalFont,
            AutoSize = false,
            Size = new Size(labelWidth, labelHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = new Padding(0, Dpi(6), 0, Dpi(10)),
        };
        mainLayout.Controls.Add(_messageLabel, 1, 0);

        // --- Buttons ---
        _buttonPanel = new FlowLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, Dpi(6), 0, 0),
        };
        CreateButtons(normalFont, buttonHeight, buttonMinWidth, buttonSpacing);
        mainLayout.Controls.Add(_buttonPanel, 0, 1);
        mainLayout.SetColumnSpan(_buttonPanel, 2);

        Controls.Add(mainLayout);

        // --- Calculate the content area size (excluding OS chrome and custom title bar) ---
        var buttonsWidth = 0;
        foreach (Control c in _buttonPanel.Controls) {
            buttonsWidth += c.Width + c.Margin.Horizontal;
        }

        var contentWidth = Math.Max(iconColumnWidth + labelWidth, buttonsWidth);
        var clientWidth = contentWidth + pad * 2;
        clientWidth = Math.Max(Dpi(280), Math.Min(clientWidth, (int)(workingArea.Width * 0.45)));

        var iconRowHeight = hasIcon
            ? iconSize + _iconPicture.Margin.Vertical
            : 0;
        var labelRowHeight = labelHeight + _messageLabel.Margin.Vertical;
        var contentRowHeight = Math.Max(iconRowHeight, labelRowHeight);
        var buttonsRowHeight = buttonHeight + _buttonPanel.Margin.Vertical;
        var clientHeight = contentRowHeight + buttonsRowHeight + pad * 2;
        clientHeight = Math.Max(Dpi(100), Math.Min(clientHeight, (int)(workingArea.Height * 0.75)));

        _contentAreaSize = new Size(clientWidth, clientHeight);
        ClientSize = _contentAreaSize;
    }

    private void AddButton(string text, DialogResult result, bool isPrimary, Font font, int height, int minWidth, int spacing) {
        var textWidth = TextRenderer.MeasureText(text, new Font(font, FontStyle.Bold)).Width;
        var btnWidth = Math.Max(minWidth, textWidth + Dpi(50));

        var button = new Button {
            Text = text,
            Width = btnWidth,
            Height = height,
            Font = isPrimary ? new Font(font, FontStyle.Bold) : font,
            Margin = new Padding(0, 0, spacing, 0),
            Tag = isPrimary ? "primary" : "secondary",
        };
        button.Click += (_, _) => {
            DialogResult = result;
            Close();
        };
        _buttonPanel.Controls.Add(button);

        if (isPrimary) {
            AcceptButton = button;
        } else if (result == DialogResult.Cancel) {
            CancelButton = button;
        }
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        var palette = AppColorizationService.Instance.GetNeutralPalette(isLight);
        BackColor = palette.FormBackground;
        ForeColor = palette.StrongText;
        _messageLabel.ForeColor = palette.StrongText;

        // Icon is rendered later in OnShown at the correct physical size.
        // Set a placeholder here so it's visible during initial layout.
        _iconPicture.Image?.Dispose();
        _iconPicture.Image = MaterialIcons.GetMessageBoxIcon(_icon, isLight, _iconPicture.Width);
        _iconPicture.Visible = _iconPicture.Image != null;

        foreach (Control control in _buttonPanel.Controls) {
            if (control is Button btn) {
                ThemedButtonStyler.Apply(btn, isLight);
            }
        }
    }

    private void CreateButtons(Font font, int height, int minWidth, int spacing) {
        switch (_buttons) {
            case MessageBoxButtons.OK:
                AddButton("OK", DialogResult.OK, true, font, height, minWidth, spacing);
                break;
            case MessageBoxButtons.OKCancel:
                AddButton("Cancel", DialogResult.Cancel, false, font, height, minWidth, spacing);
                AddButton("OK", DialogResult.OK, true, font, height, minWidth, spacing);
                break;
            case MessageBoxButtons.YesNo:
                AddButton("No", DialogResult.No, false, font, height, minWidth, spacing);
                AddButton("Yes", DialogResult.Yes, true, font, height, minWidth, spacing);
                break;
            case MessageBoxButtons.YesNoCancel:
                AddButton("Cancel", DialogResult.Cancel, false, font, height, minWidth, spacing);
                AddButton("No", DialogResult.No, false, font, height, minWidth, spacing);
                AddButton("Yes", DialogResult.Yes, true, font, height, minWidth, spacing);
                break;
            case MessageBoxButtons.RetryCancel:
                AddButton("Cancel", DialogResult.Cancel, false, font, height, minWidth, spacing);
                AddButton("Retry", DialogResult.Retry, true, font, height, minWidth, spacing);
                break;
            case MessageBoxButtons.AbortRetryIgnore:
                AddButton("Ignore", DialogResult.Ignore, false, font, height, minWidth, spacing);
                AddButton("Retry", DialogResult.Retry, false, font, height, minWidth, spacing);
                AddButton("Abort", DialogResult.Abort, true, font, height, minWidth, spacing);
                break;
        }
    }

    #endregion Private Methods
}
