using MaterialSkin;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

/// <summary>
/// Custom color picker dialog with a color wheel, brightness slider,
/// hex/RGB inputs, and preview. Supports light and dark themes.
/// Uses TableLayoutPanel for correct scaling at any DPI.
/// </summary>
public class ThemedColorPicker : Form {
    #region Private Fields

    private const int BarWidth = 24;
    private readonly Color _borderColor;
    private readonly Color _fg;
    private readonly Color _formBg;
    private readonly Color _inputBg;

    // --- Theme colors (exact same as SettingsForm / DashboardEditorForm) ---
    private readonly bool _isLight;
    private int _barHeight;
    private float _brightness = 1f;
    private Panel _brightnessBar = null!;
    private Bitmap _brightnessBitmap = null!;
    private TrackBar _brightnessSlider = null!;
    private TextBox _hexBox = null!;

    // --- HSV state ---
    private float _hue;
    private NumericUpDown _nudB = null!;
    private NumericUpDown _nudG = null!;
    private NumericUpDown _nudR = null!;
    private Panel _previewNew = null!;
    private Panel _previewOld = null!;

    // 0..360
    private float _saturation;

    // --- Flags ---
    private bool _updatingControls;
    private Bitmap _wheelBitmap = null!;

    // --- Controls ---
    private Panel _wheelPanel = null!;

    // 0..1
    // 0..1 (Value in HSV)
    private int _wheelSize;

    #endregion Private Fields

    #region Private Constructors

    private ThemedColorPicker(Color initialColor) {
        SelectedColor = initialColor;
        ColorToHSV(initialColor, out _hue, out _saturation, out _brightness);

        var sm = MaterialSkinManager.Instance;
        _isLight = sm.Theme == MaterialSkinManager.Themes.LIGHT;

        _formBg = _isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        _inputBg = _isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _fg = _isLight ? Color.Black : Color.White;
        _borderColor = _isLight ? Color.FromArgb(200, 200, 200) : Color.FromArgb(60, 75, 80);

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewColorSchemes);
        GenerateWheelBitmap();
        GenerateBrightnessBitmap();
        UpdateControlsFromHSV();
    }

    #endregion Private Constructors

    #region Public Properties

    // --- Result ---
    public Color SelectedColor { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Shows the themed color picker and returns DialogResult.
    /// </summary>
    public static DialogResult ShowPicker(Color initialColor, out Color resultColor) {
        using var picker = new ThemedColorPicker(initialColor);
        var result = picker.ShowDialog();
        resultColor = picker.SelectedColor;
        return result;
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _wheelBitmap?.Dispose();
            _brightnessBitmap?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion Protected Methods

    #region Private Methods

    private static void ColorToHSV(Color c, out float h, out float s, out float v) {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;

        if (delta == 0) { h = 0; } else if (max == r) { h = 60 * ((g - b) / delta % 6); } else if (max == g) { h = 60 * ((b - r) / delta + 2); } else { h = 60 * ((r - g) / delta + 4); }

        if (h < 0) {
            h += 360;
        }
    }

    private static Color HSVToColor(float h, float s, float v) {
        h = ((h % 360) + 360) % 360;
        var c = v * s;
        var x = c * (1 - MathF.Abs(h / 60f % 2 - 1));
        var m = v - c;

        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; } else if (h < 120) { r = x; g = c; b = 0; } else if (h < 180) { r = 0; g = c; b = x; } else if (h < 240) { r = 0; g = x; b = c; } else if (h < 300) { r = x; g = 0; b = c; } else { r = c; g = 0; b = x; }

        return Color.FromArgb(
            Math.Clamp((int)((r + m) * 255), 0, 255),
            Math.Clamp((int)((g + m) * 255), 0, 255),
            Math.Clamp((int)((b + m) * 255), 0, 255)
        );
    }

    private void BrightnessBar_Mouse(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Left) {
            return;
        }

        var clamped = Math.Clamp(e.Y, 0, _barHeight - 1);
        _brightness = 1f - (float)clamped / (_barHeight - 1);

        GenerateWheelBitmap();
        GenerateBrightnessBitmap();
        _wheelPanel.Invalidate();
        _brightnessBar.Invalidate();
        UpdateControlsFromHSV();
    }

    private void BrightnessBar_Paint(object? sender, PaintEventArgs e) {
        var g = e.Graphics;
        g.DrawImage(_brightnessBitmap, 0, 0);

        using var borderPen = new Pen(_borderColor, 1f);
        g.DrawRectangle(borderPen, 0, 0, BarWidth - 1, _barHeight - 1);

        // Position indicator
        var posY = (int)((1f - _brightness) * (_barHeight - 1));
        using var outerPen = new Pen(Color.Black, 2.5f);
        using var innerPen = new Pen(Color.White, 1f);
        g.DrawLine(outerPen, 0, posY, BarWidth, posY);
        g.DrawLine(innerPen, 0, posY, BarWidth, posY);
    }

    private void BrightnessSlider_Changed(object? sender, EventArgs e) {
        if (_updatingControls) {
            return;
        }

        _brightness = _brightnessSlider.Value / 100f;
        GenerateWheelBitmap();
        GenerateBrightnessBitmap();
        _wheelPanel.Invalidate();
        _brightnessBar.Invalidate();
        UpdateControlsFromHSV();
    }

    private FlowLayoutPanel CreateButtonPanel() {
        var panel = new FlowLayoutPanel {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0),
            WrapContents = false
        };

        var btnCancel = new Button {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 110,
            Height = 35,
            Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        ThemedButtonStyler.Apply(btnCancel, _isLight);

        var btnOk = new Button {
            Text = "Save",
            Width = 110,
            Height = 35,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 10, 0)
        };
        ThemedButtonStyler.Apply(btnOk, _isLight);
        btnOk.Click += (s, e) => {
            SelectedColor = HSVToColor(_hue, _saturation, _brightness);
            DialogResult = DialogResult.OK;
            Close();
        };

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        panel.Controls.Add(btnCancel);
        panel.Controls.Add(btnOk);

        return panel;
    }

    private TableLayoutPanel CreateRightPanel() {
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Margin = Padding.Empty,
            AutoSize = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 8; i++) {
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var row = 0;

        // --- Hex ---
        panel.Controls.Add(MakeLabel("Hex:"), 0, row);
        _hexBox = new TextBox {
            Dock = DockStyle.Fill,
            BackColor = _inputBg,
            ForeColor = _fg,
            BorderStyle = BorderStyle.FixedSingle,
            MaxLength = 6,
            CharacterCasing = CharacterCasing.Upper,
            Font = new Font("Consolas", 10f),
            Margin = new Padding(3, 3, 0, 6)
        };
        _hexBox.TextChanged += HexBox_Changed;
        panel.Controls.Add(_hexBox, 1, row);
        row++;

        // --- R ---
        panel.Controls.Add(MakeLabel("R:"), 0, row);
        _nudR = MakeNud();
        _nudR.ValueChanged += Nud_Changed;
        panel.Controls.Add(_nudR, 1, row);
        row++;

        // --- G ---
        panel.Controls.Add(MakeLabel("G:"), 0, row);
        _nudG = MakeNud();
        _nudG.ValueChanged += Nud_Changed;
        panel.Controls.Add(_nudG, 1, row);
        row++;

        // --- B ---
        panel.Controls.Add(MakeLabel("B:"), 0, row);
        _nudB = MakeNud();
        _nudB.ValueChanged += Nud_Changed;
        panel.Controls.Add(_nudB, 1, row);
        row++;

        // --- Brightness slider ---
        panel.Controls.Add(MakeLabel("Val:"), 0, row);
        _brightnessSlider = new TrackBar {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 100,
            Value = (int)(_brightness * 100),
            TickStyle = TickStyle.None,
            BackColor = _formBg,
            Margin = new Padding(0, 3, 0, 6)
        };
        _brightnessSlider.ValueChanged += BrightnessSlider_Changed;
        panel.Controls.Add(_brightnessSlider, 1, row);
        row++;

        // --- Preview heading ---
        var previewLabel = MakeLabel("Preview:");
        previewLabel.Margin = new Padding(0, 8, 0, 3);
        panel.Controls.Add(previewLabel, 0, row);
        panel.SetColumnSpan(previewLabel, 2);
        row++;

        // --- Preview: Old / New ---
        var previewRow = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Height = 36,
            Margin = new Padding(0, 0, 0, 3)
        };
        previewRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        previewRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        previewRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        previewRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        previewRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        previewRow.Controls.Add(MakeLabel("Old:"), 0, 0);
        _previewOld = new Panel {
            Dock = DockStyle.Fill,
            BackColor = SelectedColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3)
        };
        previewRow.Controls.Add(_previewOld, 1, 0);

        previewRow.Controls.Add(MakeLabel("New:"), 2, 0);
        _previewNew = new Panel {
            Dock = DockStyle.Fill,
            BackColor = SelectedColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3)
        };
        previewRow.Controls.Add(_previewNew, 3, 0);

        panel.Controls.Add(previewRow, 0, row);
        panel.SetColumnSpan(previewRow, 2);

        return panel;
    }

    private void GenerateBrightnessBitmap() {
        _brightnessBitmap?.Dispose();
        _brightnessBitmap = new Bitmap(BarWidth, _barHeight);

        using var g = Graphics.FromImage(_brightnessBitmap);
        for (int y = 0; y < _barHeight; y++) {
            var v = 1f - (float)y / (_barHeight - 1);
            var c = HSVToColor(_hue, _saturation, v);
            using var pen = new Pen(c);
            g.DrawLine(pen, 0, y, BarWidth, y);
        }
    }

    private void GenerateWheelBitmap() {
        _wheelBitmap?.Dispose();
        _wheelBitmap = new Bitmap(_wheelSize, _wheelSize);

        var cx = _wheelSize / 2f;
        var cy = _wheelSize / 2f;
        var radius = _wheelSize / 2f - 2;

        for (int y = 0; y < _wheelSize; y++) {
            for (int x = 0; x < _wheelSize; x++) {
                var dx = x - cx;
                var dy = y - cy;
                var dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist <= radius) {
                    var angle = MathF.Atan2(dy, dx) * 180f / MathF.PI;
                    if (angle < 0) {
                        angle += 360;
                    }

                    var sat = dist / radius;
                    _wheelBitmap.SetPixel(x, y, HSVToColor(angle, sat, _brightness));
                } else {
                    _wheelBitmap.SetPixel(x, y, _formBg);
                }
            }
        }
    }

    private void HexBox_Changed(object? sender, EventArgs e) {
        if (_updatingControls) {
            return;
        }

        var hex = _hexBox.Text.Trim();
        if (hex.Length == 6 && int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var val)) {
            var c = Color.FromArgb((val >> 16) & 0xFF, (val >> 8) & 0xFF, val & 0xFF);
            ColorToHSV(c, out _hue, out _saturation, out _brightness);
            GenerateWheelBitmap();
            GenerateBrightnessBitmap();
            _wheelPanel.Invalidate();
            _brightnessBar.Invalidate();
            UpdateControlsFromHSV();
        }
    }

    private void InitializeComponent() {
        SuspendLayout();

        Text = "Color Picker";
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = _formBg;
        ForeColor = _fg;
        Font = new Font("Segoe UI", 9.5f);
        Padding = new Padding(15);

        _wheelSize = 220;
        _barHeight = 220;

        // ============================================================
        //  Root layout: 2 rows — [content] [buttons]
        // ============================================================
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // ============================================================
        //  Content: 3 columns — [wheel] [brightness bar] [controls]
        // ============================================================
        var content = new TableLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // --- Column 0: Color Wheel ---
        _wheelPanel = new Panel {
            Size = new Size(_wheelSize, _wheelSize),
            MinimumSize = new Size(_wheelSize, _wheelSize),
            Margin = new Padding(0, 0, 10, 0),
            Cursor = Cursors.Cross
        };
        _wheelPanel.Paint += WheelPanel_Paint;
        _wheelPanel.MouseDown += WheelPanel_Mouse;
        _wheelPanel.MouseMove += WheelPanel_Mouse;
        content.Controls.Add(_wheelPanel, 0, 0);

        // --- Column 1: Brightness Bar ---
        _brightnessBar = new Panel {
            Size = new Size(BarWidth, _barHeight),
            MinimumSize = new Size(BarWidth, _barHeight),
            Margin = new Padding(0, 0, 15, 0),
            Cursor = Cursors.Hand
        };
        _brightnessBar.Paint += BrightnessBar_Paint;
        _brightnessBar.MouseDown += BrightnessBar_Mouse;
        _brightnessBar.MouseMove += BrightnessBar_Mouse;
        content.Controls.Add(_brightnessBar, 1, 0);

        // --- Column 2: Right panel with controls ---
        content.Controls.Add(CreateRightPanel(), 2, 0);

        root.Controls.Add(content, 0, 0);

        // ============================================================
        //  Buttons row
        // ============================================================
        root.Controls.Add(CreateButtonPanel(), 0, 1);

        Controls.Add(root);

        ResumeLayout(false);
        PerformLayout();
    }
    private Label MakeLabel(string text) {
        return new Label {
            Text = text,
            AutoSize = true,
            ForeColor = _fg,
            Margin = new Padding(0, 6, 6, 3),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private NumericUpDown MakeNud() {
        return new NumericUpDown {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 255,
            BackColor = _inputBg,
            ForeColor = _fg,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(3, 3, 0, 4)
        };
    }

    private void Nud_Changed(object? sender, EventArgs e) {
        if (_updatingControls) {
            return;
        }

        var c = Color.FromArgb((int)_nudR.Value, (int)_nudG.Value, (int)_nudB.Value);
        ColorToHSV(c, out _hue, out _saturation, out _brightness);
        GenerateWheelBitmap();
        GenerateBrightnessBitmap();
        _wheelPanel.Invalidate();
        _brightnessBar.Invalidate();
        UpdateControlsFromHSV();
    }

    private void UpdateControlsFromHSV() {
        _updatingControls = true;
        try {
            var color = HSVToColor(_hue, _saturation, _brightness);
            _previewNew.BackColor = color;
            _hexBox.Text = $"{color.R:X2}{color.G:X2}{color.B:X2}";
            _nudR.Value = color.R;
            _nudG.Value = color.G;
            _nudB.Value = color.B;
            _brightnessSlider.Value = Math.Clamp((int)(_brightness * 100), 0, 100);
        } finally {
            _updatingControls = false;
        }
    }

    private void WheelPanel_Mouse(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Left) {
            return;
        }

        var cx = _wheelSize / 2f;
        var radius = _wheelSize / 2f - 2;
        var dx = e.X - cx;
        var dy = e.Y - cx;
        var dist = MathF.Sqrt(dx * dx + dy * dy);

        _saturation = Math.Min(dist / radius, 1f);
        _hue = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        if (_hue < 0) {
            _hue += 360;
        }

        GenerateBrightnessBitmap();
        _brightnessBar.Invalidate();
        _wheelPanel.Invalidate();
        UpdateControlsFromHSV();
    }

    private void WheelPanel_Paint(object? sender, PaintEventArgs e) {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.DrawImage(_wheelBitmap, 0, 0);

        // Circle border
        using var borderPen = new Pen(_borderColor, 1.5f);
        var cx = _wheelSize / 2f;
        var radius = _wheelSize / 2f - 2;
        g.DrawEllipse(borderPen, cx - radius, cx - radius, radius * 2, radius * 2);

        // Crosshair at current position
        var r = _saturation * radius;
        var angleRad = _hue * MathF.PI / 180f;
        var px = cx + r * MathF.Cos(angleRad);
        var py = cx + r * MathF.Sin(angleRad);
        var cs = 6;

        using var outerPen = new Pen(Color.Black, 2.5f);
        using var innerPen = new Pen(Color.White, 1.5f);
        g.DrawEllipse(outerPen, px - cs, py - cs, cs * 2, cs * 2);
        g.DrawEllipse(innerPen, px - cs, py - cs, cs * 2, cs * 2);
    }

    #endregion Private Methods
}
