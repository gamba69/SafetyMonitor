using MaterialSkin;

namespace SafetyMonitorView.Forms;

public class SettingsForm : Form {
    #region Private Fields

    private Button _browseButton = null!;
    private Button _cancelButton = null!;
    private Label _connectionStatusLabel = null!;
    private NumericUpDown _chartStaticTimeoutNumeric = null!;
    private NumericUpDown _chartStaticAggregationPresetMatchToleranceNumeric = null!;
    private NumericUpDown _chartStaticAggregationTargetPointsNumeric = null!;
    private NumericUpDown _refreshIntervalNumeric = null!;
    private Button _saveButton = null!;
    private TextBox _storagePathTextBox = null!;
    private Button _testConnectionButton = null!;

    #endregion Private Fields

    #region Public Constructors

    public SettingsForm(string currentStoragePath, int currentRefreshInterval, int currentChartStaticTimeoutSeconds, double currentChartStaticAggregationPresetMatchTolerancePercent, int currentChartStaticAggregationTargetPointCount) {
        StoragePath = currentStoragePath;
        RefreshInterval = currentRefreshInterval;
        ChartStaticTimeoutSeconds = currentChartStaticTimeoutSeconds;
        ChartStaticAggregationPresetMatchTolerancePercent = Math.Clamp(currentChartStaticAggregationPresetMatchTolerancePercent, 0, 100);
        ChartStaticAggregationTargetPointCount = Math.Max(2, currentChartStaticAggregationTargetPointCount);

        InitializeComponent();
        ApplyTheme();
        LoadSettings();
    }

    #endregion Public Constructors

    #region Public Properties

    public int RefreshInterval { get; private set; } = 5;
    public int ChartStaticTimeoutSeconds { get; private set; } = 120;
    public double ChartStaticAggregationPresetMatchTolerancePercent { get; private set; } = 10;
    public int ChartStaticAggregationTargetPointCount { get; private set; } = 300;
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
            // Skip connection status label - it has its own color logic
            if (control == _connectionStatusLabel) {
                ApplyThemeRecursive(control, isLight);
                continue;
            }

            switch (control) {
                case Label lbl:
                    lbl.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case Button btn:
                    var isPrimary = btn == _saveButton;
                    var isCancel = btn == _cancelButton;
                    btn.BackColor = isPrimary ? Color.FromArgb(0, 121, 107)
                        : (isCancel ? Color.Gray : (isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(53, 70, 76)));
                    btn.ForeColor = (isPrimary || isCancel) ? Color.White : (isLight ? Color.Black : Color.White);
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
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

        // Main layout
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 13,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Storage Path label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Storage Path + Browse
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Test Connection + Status
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Refresh Interval label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Refresh Interval
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: Static timeout label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 6: Static timeout value
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 7: Preset tolerance label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 8: Preset tolerance value
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 9: Target points label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 10: Target points value
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 11: Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 12: Buttons

        // Row 0: Storage Path label
        var storagePathLabel = new Label {
            Text = "Data Storage Path:",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainLayout.Controls.Add(storagePathLabel, 0, 0);

        // Row 1: Storage Path + Browse button
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
            Width = 90,
            Height = 30,
            Font = normalFont
        };
        _browseButton.Click += BrowseButton_Click;
        pathPanel.Controls.Add(_browseButton, 1, 0);

        mainLayout.Controls.Add(pathPanel, 0, 1);

        // Row 2: Test Connection + Status
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

        // Row 3: Refresh Interval label
        var refreshIntervalLabel = new Label {
            Text = "Refresh Interval (seconds):",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainLayout.Controls.Add(refreshIntervalLabel, 0, 3);

        // Row 4: Refresh Interval
        _refreshIntervalNumeric = new NumericUpDown {
            Width = 100,
            Minimum = 1,
            Maximum = 60,
            Value = 5,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 20)
        };
        mainLayout.Controls.Add(_refreshIntervalNumeric, 0, 4);

        var chartStaticTimeoutLabel = new Label {
            Text = "Chart static mode timeout (seconds):",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainLayout.Controls.Add(chartStaticTimeoutLabel, 0, 5);

        _chartStaticTimeoutNumeric = new NumericUpDown {
            Width = 100,
            Minimum = 10,
            Maximum = 3600,
            Value = 120,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 20)
        };
        mainLayout.Controls.Add(_chartStaticTimeoutNumeric, 0, 6);

        var staticAggregationPresetToleranceLabel = new Label {
            Text = "Static mode preset match tolerance (%):",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainLayout.Controls.Add(staticAggregationPresetToleranceLabel, 0, 7);

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
        mainLayout.Controls.Add(_chartStaticAggregationPresetMatchToleranceNumeric, 0, 8);

        var staticAggregationTargetPointsLabel = new Label {
            Text = "Static mode target chart points:",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainLayout.Controls.Add(staticAggregationTargetPointsLabel, 0, 9);

        _chartStaticAggregationTargetPointsNumeric = new NumericUpDown {
            Width = 100,
            Minimum = 2,
            Maximum = 5000,
            Value = 300,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 20)
        };
        mainLayout.Controls.Add(_chartStaticAggregationTargetPointsNumeric, 0, 10);

        // Spacer
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 11);


        // Buttons
        var buttonPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 10, 0, 0)
        };

        _cancelButton = new Button {
            Text = "Cancel",
            Width = 90,
            Height = 35,
            Font = normalFont,
            Margin = new Padding(0)
        };
        _cancelButton.Click += CancelButton_Click;
        buttonPanel.Controls.Add(_cancelButton);

        _saveButton = new Button {
            Text = "Save",
            Width = 100,
            Height = 35,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);

        mainLayout.Controls.Add(buttonPanel, 0, 12);

        Controls.Add(mainLayout);

        // Set form size
        ClientSize = new Size(550, 500);
    }
    private void LoadSettings() {
        _storagePathTextBox.Text = StoragePath;
        _refreshIntervalNumeric.Value = RefreshInterval;
        _chartStaticTimeoutNumeric.Value = ChartStaticTimeoutSeconds;
        _chartStaticAggregationPresetMatchToleranceNumeric.Value = Math.Clamp((decimal)ChartStaticAggregationPresetMatchTolerancePercent, 0m, 100m);
        _chartStaticAggregationTargetPointsNumeric.Value = Math.Clamp(ChartStaticAggregationTargetPointCount, 2, 5000);
    }
    private void SaveButton_Click(object? sender, EventArgs e) {
        StoragePath = _storagePathTextBox.Text;
        RefreshInterval = (int)_refreshIntervalNumeric.Value;
        ChartStaticTimeoutSeconds = (int)_chartStaticTimeoutNumeric.Value;
        ChartStaticAggregationPresetMatchTolerancePercent = (double)_chartStaticAggregationPresetMatchToleranceNumeric.Value;
        ChartStaticAggregationTargetPointCount = (int)_chartStaticAggregationTargetPointsNumeric.Value;

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
