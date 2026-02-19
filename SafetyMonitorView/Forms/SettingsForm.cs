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

    // Tab infrastructure
    private Panel _tabSegmentPanel = null!;
    private readonly List<RadioButton> _tabButtons = new();
    private readonly List<Panel> _tabPages = new();
    private int _selectedTabIndex;

    private static readonly string[] TabNames = { "General", "Storage", "Value Tiles", "Chart Tiles", "Aggregation" };
    private static readonly string[] TabIcons = {
        MaterialIcons.MenuFileSettings,
        MaterialIcons.CommonBrowse,
        MaterialIcons.WindowTileValue,
        MaterialIcons.WindowTileChart,
        MaterialIcons.CommonCalculate
    };

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

        ApplyTabSegmentTheme(isLight);
        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyTabSegmentTheme(bool isLight) {
        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.FromArgb(21, 28, 31) : Color.White;
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        _tabSegmentPanel.BackColor = borderColor;

        for (int i = 0; i < _tabButtons.Count; i++) {
            var button = _tabButtons[i];
            button.BackColor = button.Checked ? activeBg : segmentBg;
            button.ForeColor = button.Checked ? activeFg : inactiveFg;
            button.FlatAppearance.CheckedBackColor = activeBg;
            button.FlatAppearance.MouseOverBackColor = isLight ? Color.FromArgb(235, 240, 243) : Color.FromArgb(55, 70, 76);
            button.FlatAppearance.MouseDownBackColor = activeBg;

            button.Image?.Dispose();
            button.Image = MaterialIcons.GetIcon(TabIcons[i], button.ForeColor, 22);
        }

        // Tab page backgrounds
        var pageBg = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        foreach (var page in _tabPages) {
            page.BackColor = pageBg;
        }
    }

    private void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);

            if (control == _connectionStatusLabel) {
                ApplyThemeRecursive(control, isLight);
                continue;
            }

            // Skip tab buttons — they are styled separately
            if (_tabButtons.Contains(control)) {
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
        var descriptionFont = new Font("Segoe UI", 9f, FontStyle.Regular);

        // ── Outer layout: header, tabs, tab content, buttons ──
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = false
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 0: Form header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 1: Tab strip
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2: Tab content
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 3: Save / Cancel

        // ── Row 0: Form header description ──
        var headerLabel = new Label {
            Text = "Configure application behavior: data refresh timing, storage connection, "
                 + "value tile lookback windows, chart interaction timeouts, and aggregation calculation parameters. "
                 + "Changes take effect after clicking Save.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(580, 0),
            Margin = new Padding(0, 0, 0, 14)
        };
        mainLayout.Controls.Add(headerLabel, 0, 0);

        // ── Row 1: Segmented tab strip (same style as quick-access dashboard switcher) ──
        _tabSegmentPanel = new Panel {
            Height = 34,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(1)
        };

        var tabButtonWidth = 110;
        var totalTabWidth = tabButtonWidth * TabNames.Length;
        _tabSegmentPanel.Width = totalTabWidth + 2;
        _tabSegmentPanel.MinimumSize = new Size(totalTabWidth + 2, 34);
        _tabSegmentPanel.MaximumSize = new Size(totalTabWidth + 2, 34);

        for (int i = 0; i < TabNames.Length; i++) {
            var tabIndex = i;
            var btn = new RadioButton {
                Text = TabNames[i],
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = {
                    BorderSize = 0,
                    CheckedBackColor = Color.Transparent,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = false,
                Size = new Size(tabButtonWidth, 32),
                Location = new Point(1 + i * tabButtonWidth, 1),
                Checked = i == 0,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            btn.CheckedChanged += (s, e) => {
                if (btn.Checked) {
                    SelectTab(tabIndex);
                }
            };

            var icon = MaterialIcons.GetIcon(TabIcons[i], Color.White, 22);
            if (icon is not null) {
                btn.Image = icon;
            }

            _tabButtons.Add(btn);
            _tabSegmentPanel.Controls.Add(btn);
        }

        mainLayout.Controls.Add(_tabSegmentPanel, 0, 1);

        // ── Row 2: Stacked tab pages (only one visible at a time) ──
        var contentHost = new Panel {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        _tabPages.Add(CreateGeneralTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateStorageTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateValueTilesTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateChartTilesTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateAggregationTab(titleFont, normalFont, descriptionFont));

        foreach (var page in _tabPages) {
            page.Dock = DockStyle.Fill;
            page.Visible = false;
            contentHost.Controls.Add(page);
        }

        _tabPages[0].Visible = true;
        _selectedTabIndex = 0;

        mainLayout.Controls.Add(contentHost, 0, 2);

        // ── Row 3: Save / Cancel buttons ──
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

        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);
        ClientSize = new Size(620, 540);
    }

    // ════════════════════════════════════════════════════════════════
    //  Tab pages
    // ════════════════════════════════════════════════════════════════

    private Panel CreateGeneralTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel { Padding = new Padding(0, 4, 0, 0) };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label {
            Text = "How often the application polls the data storage for new values. "
                 + "All dashboard tiles (both value tiles and charts) are refreshed at this interval. "
                 + "Lower values provide more responsive updates but increase disk I/O. "
                 + "Recommended range: 3–10 seconds for live monitoring, 30–60 seconds for review.",
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(575, 0),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);

        _refreshIntervalNumeric = CreateNumeric(1, 60, 5, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Refresh Interval",
            "How often the app reloads data from storage.",
            "seconds",
            _refreshIntervalNumeric,
            titleFont,
            descriptionFont), 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateStorageTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel { Padding = new Padding(0, 4, 0, 0) };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label {
            Text = "Data storage location where the collector writes Firebird databases. "
                 + "Each month of data is stored in a separate .fdb file inside this folder. "
                 + "Firebird Embedded must be installed separately (fbclient.dll and plugins). "
                 + "Use the Test Connection button to verify the path is valid and Firebird is available.",
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(575, 0),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);

        var pathPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 8)
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var nameLabel = CreateLabel("Data Storage Path", titleFont);
        nameLabel.Margin = new Padding(0, 4, 14, 0);
        pathPanel.Controls.Add(nameLabel, 0, 0);

        _storagePathTextBox = new TextBox {
            Font = normalFont,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        pathPanel.Controls.Add(_storagePathTextBox, 1, 0);

        _browseButton = new Button {
            Text = "Browse...",
            Width = 110,
            Height = _storagePathTextBox.PreferredHeight,
            Font = normalFont,
            Margin = new Padding(0),
            Anchor = AnchorStyles.None
        };
        _browseButton.Click += BrowseButton_Click;
        pathPanel.Controls.Add(_browseButton, 2, 0);

        pathPanel.Controls.Add(new Label {
            Text = "Root folder for monthly Firebird files.",
            Font = descriptionFont,
            AutoSize = true,
            Margin = new Padding(0, 4, 14, 0)
        }, 0, 1);

        pathPanel.Controls.Add(new Label {
            Text = "Choose folder with *.fdb files for data reading and writing.",
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            Margin = new Padding(0, 4, 0, 0)
        }, 1, 1);

        layout.Controls.Add(pathPanel, 0, 1);

        _testConnectionButton = new Button {
            Text = "Test Connection",
            Width = 150,
            Height = 30,
            Font = normalFont,
            Margin = new Padding(0, 18, 0, 0),
            Anchor = AnchorStyles.None
        };
        _testConnectionButton.Click += TestConnectionButton_Click;
        layout.Controls.Add(_testConnectionButton, 0, 2);

        _connectionStatusLabel = new Label {
            Text = "",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 14, 0, 0),
            Anchor = AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleCenter
        };
        layout.Controls.Add(_connectionStatusLabel, 0, 3);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateValueTilesTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel { Padding = new Padding(0, 4, 0, 0) };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label {
            Text = "Value tiles display the most recent reading for a metric. "
                 + "The lookback window controls how far back (in minutes) the tile searches for the latest data point. "
                 + "If no data is found within this window, the tile shows a \"?\" placeholder instead of a value. "
                 + "Increase this value if your data collector writes infrequently or if tiles show \"?\" unexpectedly. "
                 + "Decrease it if you only want to see very recent readings and prefer a clear \"no data\" indicator for stale values.",
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(575, 0),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);

        _valueTileLookbackMinutesNumeric = CreateNumeric(1, 43200, 60, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Value Tile Lookback Window",
            "Time range for searching the last known metric value.",
            "minutes",
            _valueTileLookbackMinutesNumeric,
            titleFont,
            descriptionFont), 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateChartTilesTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel { Padding = new Padding(0, 4, 0, 0) };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label {
            Text = "When you pan or zoom a chart, it enters static mode: auto-refresh is paused and the chart "
                 + "freezes at the range you selected, showing a countdown timer in the corner. "
                 + "This timeout defines how many seconds the chart stays in static mode after your last interaction. "
                 + "Once the timeout expires, the chart automatically returns to auto mode and resumes live updates. "
                 + "Set a longer timeout if you need more time to study historical ranges; set a shorter one to get back to live data quickly.",
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(575, 0),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);

        _chartStaticTimeoutNumeric = CreateNumeric(10, 3600, 120, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Chart Static Mode Timeout",
            "How long chart remains in static mode after pan/zoom interaction.",
            "seconds",
            _chartStaticTimeoutNumeric,
            titleFont,
            descriptionFont), 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateAggregationTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel { Padding = new Padding(0, 4, 0, 0) };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label {
            Text = "These parameters control how chart data is aggregated when the chart enters static mode "
                 + "(pan/zoom). They affect the resolution and alignment of data points on static charts.",
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(575, 0),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);

        _chartStaticAggregationPresetMatchToleranceNumeric = new NumericUpDown {
            Width = 100,
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 10,
            Font = normalFont,
            Margin = new Padding(0, 0, 10, 10)
        };
        layout.Controls.Add(CreateSettingRow(
            "Preset Match Tolerance",
            "Allowed range difference when matching static view to a preset.",
            "%",
            _chartStaticAggregationPresetMatchToleranceNumeric,
            titleFont,
            descriptionFont), 0, 1);

        _chartStaticAggregationTargetPointsNumeric = CreateNumeric(2, 5000, 300, normalFont);
        _chartStaticAggregationTargetPointsNumeric.Margin = new Padding(0, 0, 10, 10);
        layout.Controls.Add(CreateSettingRow(
            "Target Chart Points",
            "Desired amount of points after automatic static aggregation.",
            "points",
            _chartStaticAggregationTargetPointsNumeric,
            titleFont,
            descriptionFont), 0, 2);

        _chartAggregationRoundingSecondsNumeric = CreateNumeric(1, 3600, 1, normalFont);
        _chartAggregationRoundingSecondsNumeric.Margin = new Padding(0, 0, 10, 10);
        layout.Controls.Add(CreateSettingRow(
            "Aggregation Rounding Step",
            "Round computed interval to a multiple of this value.",
            "seconds",
            _chartAggregationRoundingSecondsNumeric,
            titleFont,
            descriptionFont), 0, 3);

        page.Controls.Add(layout);
        return page;
    }

    // ════════════════════════════════════════════════════════════════
    //  Tab switching
    // ════════════════════════════════════════════════════════════════

    private void SelectTab(int index) {
        if (index == _selectedTabIndex) {
            return;
        }

        _tabPages[_selectedTabIndex].Visible = false;
        _tabPages[index].Visible = true;
        _selectedTabIndex = index;

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ApplyTabSegmentTheme(isLight);
    }

    // ════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════

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


    private static TableLayoutPanel CreateSettingRow(string title, string description, string units, Control valueControl, Font titleFont, Font descriptionFont) {
        var row = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 16)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = CreateLabel(title, titleFont);
        label.Margin = new Padding(0, 0, 14, 3);
        row.Controls.Add(label, 0, 0);

        valueControl.Anchor = AnchorStyles.Right;
        row.Controls.Add(valueControl, 1, 0);

        var unitLabel = new Label {
            Text = units,
            Font = titleFont,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 3, 0, 0)
        };
        row.Controls.Add(unitLabel, 2, 0);

        row.Controls.Add(new Label {
            Text = description,
            Font = descriptionFont,
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            Margin = new Padding(0, 0, 14, 0)
        }, 0, 1);

        return row;
    }

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
