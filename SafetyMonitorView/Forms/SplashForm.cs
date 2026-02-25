using MaterialSkin;
using SafetyMonitorView.Properties;

namespace SafetyMonitorView.Forms;

internal sealed class SplashForm : Form {

    #region Private Fields

    private const int SplashHeightPx = 128;
    private const int SplashWidthPx = 384;
    private const int SplashIconSizePx = 128;
    private const int SplashIconOuterPaddingLeftPx = 8;
    private const int SplashIconOuterPaddingTopBottomPx = 8;
    private const int SplashTextPaddingLeftPx = 10;
    private const int SplashTextPaddingTopPx = 10;
    private const int SplashTextPaddingRightPx = 8;
    private const int SplashTextPaddingBottomPx = 10;
    private const int SplashSectionGapPx = 10;
    private const int SplashBorderThicknessPx = 2;
    private const int ReferenceDpi = 96;

    private readonly Icon _appIcon;
    private readonly Color _borderColor;
    private readonly TableLayoutPanel _rootLayout;

    #endregion Private Fields

    #region Public Constructors

    public SplashForm(bool isDarkTheme) {
        var theme = isDarkTheme ? MaterialSkinManager.Themes.DARK : MaterialSkinManager.Themes.LIGHT;
        var sourceIcon = theme == MaterialSkinManager.Themes.DARK
            ? Resources.AppIconDark
            : Resources.AppIconLight;
        _appIcon = new Icon(sourceIcon, new Size(SplashIconSizePx, SplashIconSizePx));

        BackColor = theme == MaterialSkinManager.Themes.DARK
            ? Color.FromArgb(25, 36, 40)
            : Color.FromArgb(245, 245, 245);
        ForeColor = theme == MaterialSkinManager.Themes.DARK
            ? Color.FromArgb(235, 235, 235)
            : Color.FromArgb(33, 33, 33);

        // Border color is taken from the same theme palette used in the app UI.
        _borderColor = theme == MaterialSkinManager.Themes.DARK
            ? Color.FromArgb(70, 85, 92)
            : Color.FromArgb(196, 206, 211);

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        Padding = new Padding(ScaleForDpi(SplashBorderThicknessPx));

        var scaledBaseSize = new Size(ScaleForDpi(SplashWidthPx), ScaleForDpi(SplashHeightPx));
        ClientSize = scaledBaseSize;
        MinimumSize = scaledBaseSize;
        DoubleBuffered = true;

        _rootLayout = BuildLayout();
        Controls.Add(_rootLayout);
        EnsureContentFits();
    }

    #endregion Public Constructors

    #region Protected Methods

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _appIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);

        var borderThickness = ScaleForDpi(SplashBorderThicknessPx);
        using var pen = new Pen(_borderColor, borderThickness);
        var inset = Math.Max(1, borderThickness / 2);
        var borderRect = new Rectangle(
            inset,
            inset,
            Math.Max(1, ClientSize.Width - borderThickness),
            Math.Max(1, ClientSize.Height - borderThickness)
        );

        e.Graphics.DrawRectangle(pen, borderRect);
    }

    #endregion Protected Methods

    #region Private Methods

    private TableLayoutPanel BuildLayout() {
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ScaleForDpi(SplashIconSizePx + SplashIconOuterPaddingLeftPx)));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var iconHost = new Panel {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(
                ScaleForDpi(SplashIconOuterPaddingLeftPx),
                ScaleForDpi(SplashIconOuterPaddingTopBottomPx),
                0,
                ScaleForDpi(SplashIconOuterPaddingTopBottomPx)
            ),
            Margin = new Padding(0)
        };

        var iconBox = new PictureBox {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = _appIcon.ToBitmap(),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        var textLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Padding = new Padding(
                ScaleForDpi(SplashTextPaddingLeftPx),
                ScaleForDpi(SplashTextPaddingTopPx),
                ScaleForDpi(SplashTextPaddingRightPx),
                ScaleForDpi(SplashTextPaddingBottomPx)
            ),
            Margin = new Padding(0)
        };
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, ScaleForDpi(SplashSectionGapPx)));
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var titleLabel = new Label {
            Text = "Safety Monitor",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 17f, FontStyle.Bold),
            ForeColor = ForeColor,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        var detailsLayout = new TableLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 2,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var copyrightCaptionLabel = new Label {
            Text = "©2026",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
            ForeColor = Color.FromArgb(200, ForeColor),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, ScaleForDpi(6), 0)
        };

        var versionCaptionLabel = new Label {
            Text = "Version",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(180, ForeColor),
            BackColor = Color.Transparent,
            Margin = new Padding(0, ScaleForDpi(2), ScaleForDpi(6), 0)
        };

        var copyrightValueLabel = new Label {
            Text = "DreamSky Observatory\nIgor K. Dulevich (gamba69)",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
            ForeColor = Color.FromArgb(200, ForeColor),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        var versionValueLabel = new Label {
            Text = "0.9.0-preview",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(180, ForeColor),
            BackColor = Color.Transparent,
            Margin = new Padding(0, ScaleForDpi(2), 0, 0)
        };

        detailsLayout.Controls.Add(copyrightCaptionLabel, 0, 0);
        detailsLayout.Controls.Add(copyrightValueLabel, 1, 0);
        detailsLayout.Controls.Add(versionCaptionLabel, 0, 1);
        detailsLayout.Controls.Add(versionValueLabel, 1, 1);

        textLayout.Controls.Add(titleLabel, 0, 0);
        textLayout.Controls.Add(detailsLayout, 0, 2);

        iconHost.Controls.Add(iconBox);

        root.Controls.Add(iconHost, 0, 0);
        root.Controls.Add(textLayout, 1, 0);

        return root;
    }

    private void EnsureContentFits() {
        SuspendLayout();
        _rootLayout.SuspendLayout();
        _rootLayout.PerformLayout();
        _rootLayout.ResumeLayout(true);

        var iconColumnWidth = ScaleForDpi(SplashIconSizePx + SplashIconOuterPaddingLeftPx);
        var iconHeight = ScaleForDpi(SplashIconSizePx + (2 * SplashIconOuterPaddingTopBottomPx));

        var textControl = _rootLayout.Controls.Count > 1 ? _rootLayout.Controls[1] : null;
        var textPreferredSize = textControl?.GetPreferredSize(Size.Empty) ?? Size.Empty;

        var borderPadding = Padding.Left + Padding.Right;
        var requiredWidth = iconColumnWidth + textPreferredSize.Width + borderPadding;
        var requiredHeight = Math.Max(iconHeight, textPreferredSize.Height) + Padding.Top + Padding.Bottom;

        var minWidth = ScaleForDpi(SplashWidthPx);
        var minHeight = ScaleForDpi(SplashHeightPx);

        ClientSize = new Size(
            Math.Max(minWidth, requiredWidth),
            Math.Max(minHeight, requiredHeight)
        );

        ResumeLayout(true);
    }

    private int ScaleForDpi(int valuePx) {
        var dpi = DeviceDpi > 0 ? DeviceDpi : ReferenceDpi;
        return (int)Math.Ceiling(valuePx * dpi / (double)ReferenceDpi);
    }

    #endregion Private Methods
}
