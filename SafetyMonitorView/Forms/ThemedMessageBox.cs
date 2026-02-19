using MaterialSkin;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

/// <summary>
/// Custom MessageBox with theme support (light/dark)
/// </summary>
public class ThemedMessageBox : Form {
    #region Private Fields

    private readonly MessageBoxButtons _buttons;
    private readonly string _caption;
    private readonly MessageBoxIcon _icon;
    private readonly string _message;
    private FlowLayoutPanel _buttonPanel = null!;
    private PictureBox _iconPicture = null!;
    private Label _messageLabel = null!;

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
        return owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
    }

    #endregion Public Methods

    #region Private Methods

    private void AddButton(string text, DialogResult result, bool isPrimary, Font font) {
        var button = new Button {
            Text = text,
            Width = 110,
            Height = 35,
            Font = isPrimary ? new Font(font, FontStyle.Bold) : font,
            Margin = new Padding(0, 0, 10, 0),
            Tag = isPrimary ? "primary" : "secondary"
        };
        button.Click += (s, e) => {
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

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        _messageLabel.ForeColor = isLight ? Color.Black : Color.White;
        _iconPicture.Image?.Dispose();
        _iconPicture.Image = MaterialIcons.GetMessageBoxIcon(_icon, isLight, 72);
        _iconPicture.Visible = _iconPicture.Image != null;

        foreach (Control control in _buttonPanel.Controls) {
            if (control is Button btn) {
                ThemedButtonStyler.Apply(btn, isLight);
            }
        }
    }

    private void CreateButtons(Font font) {
        switch (_buttons) {
            case MessageBoxButtons.OK:
                AddButton("OK", DialogResult.OK, true, font);
                break;
            case MessageBoxButtons.OKCancel:
                AddButton("Cancel", DialogResult.Cancel, false, font);
                AddButton("OK", DialogResult.OK, true, font);
                break;
            case MessageBoxButtons.YesNo:
                AddButton("No", DialogResult.No, false, font);
                AddButton("Yes", DialogResult.Yes, true, font);
                break;
            case MessageBoxButtons.YesNoCancel:
                AddButton("Cancel", DialogResult.Cancel, false, font);
                AddButton("No", DialogResult.No, false, font);
                AddButton("Yes", DialogResult.Yes, true, font);
                break;
            case MessageBoxButtons.RetryCancel:
                AddButton("Cancel", DialogResult.Cancel, false, font);
                AddButton("Retry", DialogResult.Retry, true, font);
                break;
            case MessageBoxButtons.AbortRetryIgnore:
                AddButton("Ignore", DialogResult.Ignore, false, font);
                AddButton("Retry", DialogResult.Retry, false, font);
                AddButton("Abort", DialogResult.Abort, true, font);
                break;
        }
    }

    private void InitializeComponent() {
        Text = _caption;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;
        Padding = new Padding(20);

        var normalFont = new Font("Segoe UI", 10f);

        // Main layout
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));  // Icon column
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Message column
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons row

        // Icon
        _iconPicture = new PictureBox {
            Size = new Size(72, 72),
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, 4, 15, 0)
        };
        mainLayout.Controls.Add(_iconPicture, 0, 0);

        // Message
        _messageLabel = new Label {
            Text = _message,
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(350, 0),
            Margin = new Padding(0, 8, 0, 20)
        };
        mainLayout.Controls.Add(_messageLabel, 1, 0);

        // Buttons
        _buttonPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 10, 0, 0)
        };
        CreateButtons(normalFont);
        mainLayout.Controls.Add(_buttonPanel, 0, 1);
        mainLayout.SetColumnSpan(_buttonPanel, 2);

        Controls.Add(mainLayout);

        // Calculate form size based on content so long messages are never clipped.
        var preferredWidth = Math.Max(320, mainLayout.GetPreferredSize(new Size(500, 0)).Width + Padding.Horizontal);
        var contentWidth = Math.Max(0, preferredWidth - Padding.Horizontal);
        var preferredHeight = Math.Max(150, mainLayout.GetPreferredSize(new Size(contentWidth, 0)).Height + Padding.Vertical);

        var maxClientHeight = (int)Math.Round(Screen.FromControl(this).WorkingArea.Height * 0.85f);
        ClientSize = new Size(preferredWidth, Math.Min(preferredHeight, maxClientHeight));
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _iconPicture?.Image?.Dispose();
        }

        base.Dispose(disposing);
    }


    #endregion Private Methods
}
