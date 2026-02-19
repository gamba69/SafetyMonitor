using MaterialSkin;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

public class SettingsForm : Form {
    #region Private Fields

    private Button _browseButton = null!;
    private Button _cancelButton = null!;
    private Label _connectionStatusLabel = null!;
    private NumericUpDown _chartStaticTimeoutNumeric = null!;
    private NumericUpDown _chartStaticAggregationPresetMatchToleranceNumeric = null!;
    private NumericUpDown _chartStaticAggregationTargetPointsNumeric = null!;
    private NumericUpDown _chartAggregationRoundingSecondsNumeric = null!;
    private NumericUpDown _refreshIntervalNumeric = null!;
    private NumericUpDown _valueTileLookbackMinutesNumeric = null!;
    private Button _saveButton = null!;
    private TextBox _storagePathTextBox = null!;
    private Button _testConnectionButton = null!;

    #endregion Private Fields

    #region Public Constructors

    public SettingsForm(string currentStoragePath, int currentRefreshInterval, int currentValueTileLookbackMinutes, int currentChartStaticTimeoutSeconds, double currentChartStaticAggregationPresetMatchTolerancePercent, int currentChartStaticAggregationTargetPointCount, int currentChartAggregationRoundingSeconds) {
        StoragePath = currentStoragePath;
        RefreshInterval = currentRefreshInterval;
        ValueTileLookbackMinutes = Math.Max(1, currentValueTileLookbackMinutes);
        ChartStaticTimeoutSeconds = currentChartStaticTimeoutSeconds;
        ChartStaticAggregationPresetMatchTolerancePercent = Math.Clamp(currentChartStaticAggregationPresetMatchTolerancePercent, 0, 100);
        ChartStaticAggregationTargetPointCount = Math.Max(2, currentChartStaticAggregationTargetPointCount);
        ChartAggregationRoundingSeconds = Math.Max(1, currentChartAggregationRoundingSeconds);

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuFileSettings);
        ApplyTheme();
        LoadSettings();
    }

    #endregion Public Constructors

    #region Public Properties

    public int RefreshInterval { get; private set; } = 5;
    public int ValueTileLookbackMinutes { get; private set; } = 60;
    public int ChartStaticTimeoutSeconds { get; private set; } = 120;
    public double ChartStaticAggregationPresetMatchTolerancePercent { get; private set; } = 10;
    public int ChartStaticAggregationTargetPointCount { get; private set; } = 300;
    public int ChartAggregationRoundingSeconds { get; private set; } = 1;
    public string StoragePath { get; private set; } = "";

    #endregion Public Properties

    #region Private Methods

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);
            if (control == _connectionStatusLabel) {
                ApplyThemeRecursive(control, isLight);
                continue;
            }

            switch (control) {
                case Label lbl:
                    lbl.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case Button btn:
                    ThemedButtonStyler.Apply(btn, isLight);
                    break;
                case TextBox txt:
                    txt.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    txt.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case NumericUpDown num:
                    num.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    num.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case CheckBox chk:
                    chk.ForeColor = isLight ? Color.Black : Color.White;
                    break;
            }

            ApplyThemeRecursive(control, isLight);
        }
    }

    private void BrowseButton_Click(object? sender, EventArgs e) {
        using var dialog = new FolderBrowserDialog {
            Description = "Select Data Storage root folder",
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrEmpty(_storagePathTextBox.Text)) {
            dialog.SelectedPath = _storagePathTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK) {
            _storagePathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void CancelButton_Click(object? sender, EventArgs e) {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void InitializeComponent() {
        Text = "Settings";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Padding = new Padding(20);

        var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        var normalFont = new Font("Segoe UI", 10f);

        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 17,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 15; i++) {
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var storagePathLabel = new Label {
            Text = "Data Storage Path:",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainLayout.Controls.Add(storagePathLabel, 0, 0);

        var pathPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10)
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _storagePathTextBox = new TextBox {
            Font = normalFont,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0)
        };
        pathPanel.Controls.Add(_storagePathTextBox, 0, 0);

        _browseButton = new Button {
            Text = "Browse...",
            Width = 110,
            Height = 30,
            Font = normalFont
        };
        _browseButton.Click += BrowseButton_Click;
        pathPanel.Controls.Add(_browseButton, 1, 0);
        mainLayout.Controls.Add(pathPanel, 0, 1);

        var testPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 20)
        };

        _testConnectionButton = new Button {
            Text = "Test Connection",
            Width = 150,
            Height = 30,
            Font = normalFont,
            Margin = new Padding(0, 0, 10, 0)
        };
        _testConnectionButton.Click += TestConnectionButton_Click;
        testPanel.Controls.Add(_testConnectionButton);

        _connectionStatusLabel = new Label {
            Text = "",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 7, 0, 0)
        };
        testPanel.Controls.Add(_connectionStatusLabel);
        mainLayout.Controls.Add(testPanel, 0, 2);

        mainLayout.Controls.Add(CreateLabel("Refresh Interval (seconds):", titleFont), 0, 3);
        _refreshIntervalNumeric = CreateNumeric(1, 60, 5, normalFont);
        mainLayout.Controls.Add(_refreshIntervalNumeric, 0, 4);

        mainLayout.Controls.Add(CreateLabel("Value tile lookback window (minutes):", titleFont), 0, 5);
        _valueTileLookbackMinutesNumeric = CreateNumeric(1, 43200, 60, normalFont);
        mainLayout.Controls.Add(_valueTileLookbackMinutesNumeric, 0, 6);

        mainLayout.Controls.Add(CreateLabel("Chart static mode timeout (seconds):", titleFont), 0, 7);
        _chartStaticTimeoutNumeric = CreateNumeric(10, 3600, 120, normalFont);
        mainLayout.Controls.Add(_chartStaticTimeoutNumeric, 0, 8);

        mainLayout.Controls.Add(CreateLabel("Static mode preset match tolerance (%):", titleFont), 0, 9);
        _chartStaticAggregationPresetMatchToleranceNumeric = new NumericUpDown {
            Width = 100,
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 10,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 20)
        };
        mainLayout.Controls.Add(_chartStaticAggregationPresetMatchToleranceNumeric, 0, 10);

        mainLayout.Controls.Add(CreateLabel("Static mode target chart points:", titleFont), 0, 11);
        _chartStaticAggregationTargetPointsNumeric = CreateNumeric(2, 5000, 300, normalFont);
        mainLayout.Controls.Add(_chartStaticAggregationTargetPointsNumeric, 0, 12);

        mainLayout.Controls.Add(CreateLabel("Aggregation rounding step (seconds):", titleFont), 0, 13);
        _chartAggregationRoundingSecondsNumeric = CreateNumeric(1, 3600, 1, normalFont);
        mainLayout.Controls.Add(_chartAggregationRoundingSecondsNumeric, 0, 14);

        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 15);

        var buttonPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 10, 0, 0)
        };

        _cancelButton = new Button {
            Text = "Cancel",
            Width = 110,
            Height = 35,
            Font = normalFont,
            Margin = new Padding(0)
        };
        _cancelButton.Click += CancelButton_Click;
        buttonPanel.Controls.Add(_cancelButton);

        _saveButton = new Button {
            Text = "Save",
            Width = 110,
            Height = 35,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);

        mainLayout.Controls.Add(buttonPanel, 0, 16);

        Controls.Add(mainLayout);
        ClientSize = new Size(550, 540);
    }

    private static Label CreateLabel(string text, Font font) => new() {
        Text = text,
        Font = font,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 5)
    };

    private static NumericUpDown CreateNumeric(int min, int max, int value, Font font) => new() {
        Width = 100,
        Minimum = min,
        Maximum = max,
        Value = value,
        Font = font,
        Margin = new Padding(0, 0, 0, 20)
    };

    private void LoadSettings() {
        _storagePathTextBox.Text = StoragePath;
        _refreshIntervalNumeric.Value = RefreshInterval;
        _valueTileLookbackMinutesNumeric.Value = Math.Clamp(ValueTileLookbackMinutes, 1, 43200);
        _chartStaticTimeoutNumeric.Value = ChartStaticTimeoutSeconds;
        _chartStaticAggregationPresetMatchToleranceNumeric.Value = Math.Clamp((decimal)ChartStaticAggregationPresetMatchTolerancePercent, 0m, 100m);
        _chartStaticAggregationTargetPointsNumeric.Value = Math.Clamp(ChartStaticAggregationTargetPointCount, 2, 5000);
        _chartAggregationRoundingSecondsNumeric.Value = Math.Clamp(ChartAggregationRoundingSeconds, 1, 3600);
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        StoragePath = _storagePathTextBox.Text;
        RefreshInterval = (int)_refreshIntervalNumeric.Value;
        ValueTileLookbackMinutes = (int)_valueTileLookbackMinutesNumeric.Value;
        ChartStaticTimeoutSeconds = (int)_chartStaticTimeoutNumeric.Value;
        ChartStaticAggregationPresetMatchTolerancePercent = (double)_chartStaticAggregationPresetMatchToleranceNumeric.Value;
        ChartStaticAggregationTargetPointCount = (int)_chartStaticAggregationTargetPointsNumeric.Value;
        ChartAggregationRoundingSeconds = (int)_chartAggregationRoundingSecondsNumeric.Value;

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Color GetConnectionStatusColor(bool isSuccess) {
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        if (isSuccess) {
            return isLight ? Color.FromArgb(0, 121, 107) : Color.FromArgb(77, 208, 182);
        }

        return isLight ? Color.FromArgb(198, 40, 40) : Color.FromArgb(239, 154, 154);
    }

    private void TestConnectionButton_Click(object? sender, EventArgs e) {
        var path = _storagePathTextBox.Text;

        if (string.IsNullOrWhiteSpace(path)) {
            _connectionStatusLabel.Text = "❌ Please specify path";
            _connectionStatusLabel.ForeColor = GetConnectionStatusColor(isSuccess: false);
            return;
        }

        if (!Directory.Exists(path)) {
            _connectionStatusLabel.Text = "❌ Folder does not exist";
            _connectionStatusLabel.ForeColor = GetConnectionStatusColor(isSuccess: false);
            return;
        }

        try {
            var storage = new DataStorage.DataStorage(path);
            _connectionStatusLabel.Text = "✅ Connection successful";
            _connectionStatusLabel.ForeColor = GetConnectionStatusColor(isSuccess: true);
        } catch (Exception ex) {
            _connectionStatusLabel.Text = $"❌ Error: {ex.Message}";
            _connectionStatusLabel.ForeColor = GetConnectionStatusColor(isSuccess: false);
        }
    }

    #endregion Private Methods
}
