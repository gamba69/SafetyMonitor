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

    private readonly Icon _appIcon;

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

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        Width = SplashWidthPx;
        Height = SplashHeightPx;
        MinimumSize = new Size(SplashWidthPx, SplashHeightPx);
        MaximumSize = new Size(SplashWidthPx, SplashHeightPx);
        DoubleBuffered = true;

        InitializeLayout();
    }

    #endregion Public Constructors

    #region Protected Methods

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _appIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    #endregion Protected Methods

    #region Private Methods

    private void InitializeLayout() {
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SplashIconSizePx + SplashIconOuterPaddingLeftPx));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var iconHost = new Panel {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(SplashIconOuterPaddingLeftPx, SplashIconOuterPaddingTopBottomPx, 0, SplashIconOuterPaddingTopBottomPx),
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
            Padding = new Padding(10, 10, 8, 10),
            Margin = new Padding(0)
        };
        textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));
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
            Margin = new Padding(0, 0, 6, 0)
        };

        var versionCaptionLabel = new Label {
            Text = "Version",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(180, ForeColor),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 6, 0)
        };

        var copyrightValueLabel = new Label {
            Text = "DreamSky Observatory\nIgor K. Dulevich (gamba69)",
            AutoSize = true,
            MaximumSize = new Size(150, 0),
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
            MaximumSize = new Size(150, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(180, ForeColor),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 2, 0, 0)
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

        Controls.Add(root);
    }

    #endregion Private Methods
}
