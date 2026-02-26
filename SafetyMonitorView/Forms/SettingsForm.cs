using FirebirdSql.Data.FirebirdClient;
using MaterialSkin;
using SafetyMonitorView.Services;
using System.Runtime.InteropServices;

namespace SafetyMonitorView.Forms;

public class SettingsForm : Form {
    #region Private Fields

    private Button _browseButton = null!;
    private Button _cancelButton = null!;
    private Label _connectionStatusLabel = null!;
    private CheckBox _showRefreshIndicatorSwitch = null!;
    private CheckBox _minimizeToTraySwitch = null!;
    private CheckBox _startMinimizedSwitch = null!;
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
    private TableLayoutPanel _tabButtonsLayout = null!;
    private readonly List<RadioButton> _tabButtons = new();
    private readonly List<Panel> _tabPages = new();
    private int _selectedTabIndex;

    private static readonly string[] TabNames = { "Refresh", "Tray", "Database", "Tiles", "Aggregation", "Config" };
    private const int SettingValueColumnWidth = 120;
    private const int SettingUnitColumnWidth = 90;
    private const int EmSetMargins = 0x00D3;
    private const int EcLeftMargin = 0x0001;
    private const int EcRightMargin = 0x0002;
    private const int NumericTextPaddingPx = 6;
    private const int TabIconSize = 22;
    private const int TabButtonHorizontalPadding = 14;

    private static readonly string[] TabIcons = {
        "refresh",
        "pip",
        MaterialIcons.CommonDatabase,
        MaterialIcons.DashboardTab,
        MaterialIcons.CommonAvgTime,
        "rule_settings"
    };

    #endregion Private Fields

    #region Public Constructors

    public SettingsForm(string currentStoragePath, int currentRefreshInterval, int currentValueTileLookbackMinutes, int currentChartStaticTimeoutSeconds, double currentChartStaticAggregationPresetMatchTolerancePercent, int currentChartStaticAggregationTargetPointCount, int currentChartAggregationRoundingSeconds, bool currentShowRefreshIndicator, bool currentMinimizeToTray, bool currentStartMinimized) {
        StoragePath = currentStoragePath;
        RefreshInterval = currentRefreshInterval;
        ValueTileLookbackMinutes = Math.Max(1, currentValueTileLookbackMinutes);
        ChartStaticTimeoutSeconds = currentChartStaticTimeoutSeconds;
        ChartStaticAggregationPresetMatchTolerancePercent = Math.Clamp(currentChartStaticAggregationPresetMatchTolerancePercent, 0, 100);
        ChartStaticAggregationTargetPointCount = Math.Max(2, currentChartStaticAggregationTargetPointCount);
        ChartAggregationRoundingSeconds = Math.Max(1, currentChartAggregationRoundingSeconds);
        ShowRefreshIndicator = currentShowRefreshIndicator;
        MinimizeToTray = currentMinimizeToTray;
        StartMinimized = currentStartMinimized;

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
    public bool ShowRefreshIndicator { get; private set; } = true;
    public bool MinimizeToTray { get; private set; } = false;
    public bool StartMinimized { get; private set; } = false;

    #endregion Public Properties

    #region Private Methods

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        ApplyTabSegmentTheme(isLight);
        ApplyThemeRecursive(this, isLight);
        ApplySettingSwitchTheme(isLight);
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
            button.Image = MaterialIcons.GetIcon(TabIcons[i], button.ForeColor, TabIconSize);
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
        var descriptionFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);

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
            Text = "Adjust refresh, storage, and chart behavior settings; changes apply after you click Save. Use these options to balance performance, readability, and trend analysis.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(580, 0),
            Margin = new Padding(0, 0, 0, 14)
        };
        mainLayout.Controls.Add(headerLabel, 0, 0);

        // ── Row 1: Segmented tab strip (same style as quick-access dashboard switcher) ──
        _tabSegmentPanel = new Panel {
            Height = 34,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            Padding = new Padding(1)
        };

        _tabButtonsLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = TabNames.Length,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _tabButtonsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        for (int i = 0; i < TabNames.Length; i++) {
            _tabButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        }

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
                Dock = DockStyle.Fill,
                Checked = i == 0,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty,
                Padding = new Padding(TabButtonHorizontalPadding, 0, TabButtonHorizontalPadding, 0)
            };
            btn.CheckedChanged += (s, e) => {
                if (btn.Checked) {
                    SelectTab(tabIndex);
                }
            };

            var icon = MaterialIcons.GetIcon(TabIcons[i], Color.White, TabIconSize);
            if (icon is not null) {
                btn.Image = icon;
            }

            _tabButtons.Add(btn);
            _tabButtonsLayout.Controls.Add(btn, i, 0);
        }

        _tabSegmentPanel.Controls.Add(_tabButtonsLayout);

        mainLayout.Controls.Add(_tabSegmentPanel, 0, 1);

        // ── Row 2: Stacked tab pages (only one visible at a time) ──
        var contentHost = new Panel {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        _tabPages.Add(CreateGeneralTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateTrayTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateStorageTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateTilesTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateAggregationTab(titleFont, normalFont, descriptionFont));
        _tabPages.Add(CreateEmptyTab());

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
        ClientSize = new Size(682, 594);

        Shown += (_, _) => BeginInvoke(new Action(() => {
            EnsureTabsFitClientWidth();
            headerLabel.MaximumSize = new Size(ClientSize.Width - 60, 0);
        }));

        DpiChanged += (_, _) => BeginInvoke(new Action(() => {
            EnsureTabsFitClientWidth();
            headerLabel.MaximumSize = new Size(ClientSize.Width - 60, 0);
        }));
    }

    private void EnsureTabsFitClientWidth() {
        if (_tabButtons.Count == 0 || _tabButtonsLayout is null) {
            return;
        }

        var requiredTabsWidth = 0;
        _tabButtonsLayout.SuspendLayout();
        try {
            _tabButtonsLayout.ColumnStyles.Clear();
            foreach (var button in _tabButtons) {
                var textWidth = TextRenderer.MeasureText(
                    button.Text,
                    button.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;

                var buttonWidth = textWidth
                                  + TabIconSize
                                  + (TabButtonHorizontalPadding * 2)
                                  + 26;
                buttonWidth = Math.Max(buttonWidth, 88);

                _tabButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, buttonWidth));
                requiredTabsWidth += buttonWidth;
            }
        } finally {
            _tabButtonsLayout.ResumeLayout(true);
        }

        var requiredClientWidth = Math.Max(682, requiredTabsWidth + _tabSegmentPanel.Padding.Horizontal + Padding.Horizontal + 10);
        if (ClientSize.Width < requiredClientWidth) {
            ClientSize = new Size(requiredClientWidth, ClientSize.Height);
        }
    }

    private static Panel CreateEmptyTab() => new() {
        Padding = new Padding(0, 4, 0, 0),
        AutoScroll = true
    };

    // ════════════════════════════════════════════════════════════════
    //  Tab pages
    // ════════════════════════════════════════════════════════════════

    private Panel CreateGeneralTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel {
            Padding = new Padding(0, 4, 0, 0),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _refreshIntervalNumeric = CreateNumeric(1, 60, 5, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Refresh Interval",
            "How often the application polls the data storage for new values. "
                + "All dashboard tiles (both value tiles and charts) are refreshed at this interval. "
                + "Lower values provide more responsive updates but increase disk I/O. "
                + "Recommended range: 3–10 seconds for live monitoring, 30–60 seconds for review.",
            "seconds",
            _refreshIntervalNumeric,
            titleFont,
            descriptionFont), 0, 1);

        _showRefreshIndicatorSwitch = CreateThemeSwitch(normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Show Refresh Indicator",
            "Displays a countdown animation and the last refresh timestamp in the quick-access panel. "
                + "The animated icon fills progressively until the next automatic data refresh occurs.",
            "",
            _showRefreshIndicatorSwitch,
            titleFont,
            descriptionFont), 0, 2);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateTrayTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel {
            Padding = new Padding(0, 4, 0, 0),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _minimizeToTraySwitch = CreateThemeSwitch(normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Minimize to Tray",
            "When enabled, minimizing the application will hide it to the system tray instead of the taskbar. "
                + "The tray icon color reflects the current safety status: green for Safe, red for Unsafe, "
                + "and brown if there is an error or no data available within the lookback window. "
                + "Hover over the tray icon to see metric values configured with a Tray Name in Metric Settings.",
            "",
            _minimizeToTraySwitch,
            titleFont,
            descriptionFont), 0, 1);

        _startMinimizedSwitch = CreateThemeSwitch(normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Start Minimized",
            "When enabled, the application starts minimized. If Minimize to Tray is also enabled, "
                + "the application will start directly in the system tray. "
                + "Otherwise it will start minimized to the taskbar.",
            "",
            _startMinimizedSwitch,
            titleFont,
            descriptionFont), 0, 2);

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
        ApplyTextBoxLeftPadding(_storagePathTextBox);
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

        var description = new ExpandableDescriptionPanel(
            "Data storage location where the collector writes Firebird databases. "
          + "Each month of data is stored in a separate .fdb file inside this folder. "
          + "A full Firebird 5 server must be installed and configured in the operating system. "
          + "Use the Test Connection button to verify the path is valid and Firebird is available.",
            descriptionFont) {
            Margin = new Padding(0, 4, 0, 0),
            Dock = DockStyle.Fill
        };
        pathPanel.Controls.Add(description, 0, 1);
        pathPanel.SetColumnSpan(description, 3);

        layout.Controls.Add(pathPanel, 0, 1);

        _testConnectionButton = new Button {
            Text = "Test Connection",
            Width = 150,
            Height = 30,
            Font = normalFont,
            Margin = new Padding(0, 26, 0, 0),
            Anchor = AnchorStyles.None
        };
        _testConnectionButton.Click += TestConnectionButton_Click;
        layout.Controls.Add(_testConnectionButton, 0, 2);

        _connectionStatusLabel = new Label {
            Text = "",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 22, 0, 0),
            Anchor = AnchorStyles.None,
            TextAlign = ContentAlignment.MiddleCenter
        };
        layout.Controls.Add(_connectionStatusLabel, 0, 3);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateTilesTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel { Padding = new Padding(0, 4, 0, 0) };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _valueTileLookbackMinutesNumeric = CreateNumeric(1, 43200, 60, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Value Tile Lookback Window",
            "Value tiles display the most recent reading for a metric. "
                 + "The lookback window controls how far back (in minutes) the tile searches for the latest data point. "
                 + "If no data is found within this window, the tile shows a \"?\" placeholder instead of a value. "
                 + "Increase this value if your data collector writes infrequently or if tiles show \"?\" unexpectedly. "
                 + "Decrease it if you only want to see very recent readings and prefer a clear \"no data\" indicator for stale values.",
            "minutes",
            _valueTileLookbackMinutesNumeric,
            titleFont,
            descriptionFont), 0, 1);

        _chartStaticTimeoutNumeric = CreateNumeric(10, 3600, 120, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Chart Static Mode Timeout",
            "When you pan or zoom a chart, it enters static mode: auto-refresh is paused and the chart "
                 + "freezes at the range you selected, showing a countdown timer in the corner. "
                 + "This timeout defines how many seconds the chart stays in static mode after your last interaction. "
                 + "Once the timeout expires, the chart automatically returns to auto mode and resumes live updates. "
                 + "Set a longer timeout if you need more time to study historical ranges; set a shorter one to get back to live data quickly.",
            "seconds",
            _chartStaticTimeoutNumeric,
            titleFont,
            descriptionFont), 0, 2);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateAggregationTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel {
            Padding = new Padding(0, 4, 0, 0),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _chartStaticAggregationPresetMatchToleranceNumeric = new NumericUpDown {
            Width = SettingValueColumnWidth,
            Minimum = 0,
            Maximum = 100,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 10,
            TextAlign = HorizontalAlignment.Right,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(CreateSettingRow(
            "Preset Match Tolerance",
            "When you open a historical range, the chart first tries to match it to one of the period presets "
                 + "(for example 1 hour, 24 hours, 7 days) and reuse that preset's recommended aggregation interval. "
                 + "This tolerance defines how much difference between your selected range and the preset range is still considered a match. "
                 + "Higher values make matching more permissive (more preset reuse, more consistent point density). "
                 + "Lower values make matching stricter and force custom interval calculation more often.",
            "%",
            _chartStaticAggregationPresetMatchToleranceNumeric,
            titleFont,
            descriptionFont), 0, 1);

        _chartStaticAggregationTargetPointsNumeric = CreateNumeric(2, 5000, 300, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Target Chart Points",
            "If no preset match is found, the app computes a custom aggregation interval from the selected time span. "
                 + "This value is the target number of points on the chart after aggregation. "
                 + "Smaller numbers reduce rendering load and improve readability for very long periods, but may hide short spikes. "
                 + "Larger numbers preserve more detail but can make dense charts harder to read and slower to draw.",
            "points",
            _chartStaticAggregationTargetPointsNumeric,
            titleFont,
            descriptionFont), 0, 2);

        _chartAggregationRoundingSecondsNumeric = CreateNumeric(1, 3600, 1, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Aggregation Rounding Step",
            "After calculating a custom interval, the app rounds it to a clean multiple of this step (in seconds). "
                 + "Rounding stabilizes interval selection and avoids tiny differences between similar ranges (for example 59s vs 61s). "
                 + "Use smaller steps for finer control and maximum accuracy. "
                 + "Use larger steps for more predictable, stable intervals and easier cross-chart comparison.",
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
        Width = SettingValueColumnWidth,
        Minimum = min,
        Maximum = max,
        Value = value,
        TextAlign = HorizontalAlignment.Right,
        Font = font,
        Margin = new Padding(0, 0, 0, 20)
    };

    private static CheckBox CreateThemeSwitch(Font font) {
        var checkBox = new CheckBox {
            Appearance = Appearance.Button,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 0,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Width = 72,
            Height = 30,
            Font = new Font(font.FontFamily, 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false
        };

        checkBox.CheckedChanged += (_, _) => {
            var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
            StyleSettingSwitch(checkBox, isLight);
        };

        return checkBox;
    }

    private static void ApplyNumericRightPadding(NumericUpDown numeric) {
        void ApplyMargin(TextBox textBox) {
            if (textBox.IsHandleCreated) {
                SendMessage(textBox.Handle, EmSetMargins, (IntPtr)EcRightMargin, (IntPtr)(NumericTextPaddingPx << 16));
            }
        }

        void AttachTextBox(TextBox textBox) {
            textBox.HandleCreated += (_, _) => ApplyMargin(textBox);
            ApplyMargin(textBox);
        }

        var embeddedTextBox = numeric.Controls.OfType<TextBox>().FirstOrDefault();
        if (embeddedTextBox is not null) {
            AttachTextBox(embeddedTextBox);
        }

        numeric.ControlAdded += (_, e) => {
            if (e.Control is TextBox textBox) {
                AttachTextBox(textBox);
            }
        };

        numeric.HandleCreated += (_, _) => {
            var textBox = numeric.Controls.OfType<TextBox>().FirstOrDefault();
            if (textBox is not null) {
                ApplyMargin(textBox);
            }
        };
    }

    private static void ApplyTextBoxLeftPadding(TextBox textBox) {
        void ApplyMargin() {
            if (textBox.IsHandleCreated) {
                SendMessage(textBox.Handle, EmSetMargins, (IntPtr)EcLeftMargin, (IntPtr)NumericTextPaddingPx);
            }
        }

        textBox.HandleCreated += (_, _) => ApplyMargin();
        ApplyMargin();
    }


    private static TableLayoutPanel CreateSettingRow(string title, string description, string units, Control valueControl, Font titleFont, Font descriptionFont) {
        var row = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 16)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SettingValueColumnWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SettingUnitColumnWidth));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = CreateLabel(title, titleFont);
        label.Margin = new Padding(0, 0, 14, 0);
        row.Controls.Add(label, 0, 0);

        valueControl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        valueControl.Margin = new Padding(0);
        valueControl.Dock = DockStyle.Fill;
        if (valueControl is NumericUpDown numericUpDown) {
            ApplyNumericRightPadding(numericUpDown);
        }

        row.Controls.Add(valueControl, 1, 0);

        var unitLabel = new Label {
            Text = units,
            Font = titleFont,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(8, 0, 0, 0)
        };
        row.Controls.Add(unitLabel, 2, 0);

        var descriptionPanel = new ExpandableDescriptionPanel(description, descriptionFont) {
            Margin = new Padding(0, 6, 14, 0),
            Dock = DockStyle.Fill
        };
        row.Controls.Add(descriptionPanel, 0, 1);
        row.SetColumnSpan(descriptionPanel, 3);

        return row;
    }

    private void ApplySettingSwitchTheme(bool isLight) {
        StyleSettingSwitch(_showRefreshIndicatorSwitch, isLight);
        StyleSettingSwitch(_minimizeToTraySwitch, isLight);
        StyleSettingSwitch(_startMinimizedSwitch, isLight);
    }

    private static void StyleSettingSwitch(CheckBox checkBox, bool isLight) {
        if (checkBox == null) {
            return;
        }

        // Keep switch colors exactly aligned with ThemedButtonStyler palette:
        // ON  -> Save (primary)
        // OFF -> Cancel
        var onBg = Color.FromArgb(0, 137, 123);
        var offBg = isLight ? Color.FromArgb(189, 189, 189) : Color.FromArgb(96, 105, 109);
        var activeBg = checkBox.Checked ? onBg : offBg;

        // Add the same subtle hover/pressed feedback users expect from action buttons.
        var hoverBg = BlendWith(activeBg, Color.White, 0.08f);
        var downBg = BlendWith(activeBg, Color.Black, 0.08f);

        checkBox.BackColor = activeBg;
        checkBox.ForeColor = Color.White;
        checkBox.FlatAppearance.BorderSize = 0;
        checkBox.FlatAppearance.BorderColor = activeBg;

        // Appearance.Button uses FlatAppearance.CheckedBackColor in checked state,
        // so we must set it explicitly to avoid color drift versus Save/Cancel.
        checkBox.FlatAppearance.CheckedBackColor = onBg;
        checkBox.FlatAppearance.MouseOverBackColor = hoverBg;
        checkBox.FlatAppearance.MouseDownBackColor = downBg;

        checkBox.Text = checkBox.Checked ? "ON" : "OFF";
    }

    private static Color BlendWith(Color source, Color target, float amount) {
        amount = Math.Clamp(amount, 0f, 1f);

        var r = (int)Math.Round(source.R + ((target.R - source.R) * amount));
        var g = (int)Math.Round(source.G + ((target.G - source.G) * amount));
        var b = (int)Math.Round(source.B + ((target.B - source.B) * amount));

        return Color.FromArgb(source.A, r, g, b);
    }

    private void LoadSettings() {
        _storagePathTextBox.Text = StoragePath;
        _refreshIntervalNumeric.Value = RefreshInterval;
        _showRefreshIndicatorSwitch.Checked = ShowRefreshIndicator;
        _minimizeToTraySwitch.Checked = MinimizeToTray;
        _startMinimizedSwitch.Checked = StartMinimized;
        _valueTileLookbackMinutesNumeric.Value = Math.Clamp(ValueTileLookbackMinutes, 1, 43200);
        _chartStaticTimeoutNumeric.Value = ChartStaticTimeoutSeconds;
        _chartStaticAggregationPresetMatchToleranceNumeric.Value = Math.Clamp((decimal)ChartStaticAggregationPresetMatchTolerancePercent, 0m, 100m);
        _chartStaticAggregationTargetPointsNumeric.Value = Math.Clamp(ChartStaticAggregationTargetPointCount, 2, 5000);
        _chartAggregationRoundingSecondsNumeric.Value = Math.Clamp(ChartAggregationRoundingSeconds, 1, 3600);

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ApplySettingSwitchTheme(isLight);
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        StoragePath = _storagePathTextBox.Text;
        RefreshInterval = (int)_refreshIntervalNumeric.Value;
        ShowRefreshIndicator = _showRefreshIndicatorSwitch.Checked;
        MinimizeToTray = _minimizeToTraySwitch.Checked;
        StartMinimized = _startMinimizedSwitch.Checked;
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
            var info = GetFirebirdConnectionInfo(path);
            _connectionStatusLabel.Text = $"✅ {info}";
            _connectionStatusLabel.ForeColor = GetConnectionStatusColor(isSuccess: true);
        } catch (Exception ex) {
            _connectionStatusLabel.Text = $"❌ Error: {ex.Message}";
            _connectionStatusLabel.ForeColor = GetConnectionStatusColor(isSuccess: false);
        }
    }

    private static string GetFirebirdConnectionInfo(string storageRootPath) {
        var sampleDatabase = Directory
            .EnumerateFiles(storageRootPath, "*.fdb", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (sampleDatabase is null) {
            return "Storage path is available. Firebird probe skipped: no .fdb files found in this folder or its shard subfolders yet.";
        }

        var connectionStringBuilder = new FbConnectionStringBuilder {
            DataSource = "localhost",
            Database = sampleDatabase,
            UserID = "SYSDBA",
            Password = "masterkey",
            Charset = "UTF8",
            ServerType = FbServerType.Default,
            WireCrypt = FbWireCrypt.Enabled,
            Pooling = false
        };

        using var connection = new FbConnection(connectionStringBuilder.ToString());
        connection.Open();

        using var command = new FbCommand("SELECT RDB$GET_CONTEXT('SYSTEM', 'ENGINE_VERSION') FROM RDB$DATABASE", connection);
        var engineVersion = Convert.ToString(command.ExecuteScalar())?.Trim();

        if (string.IsNullOrWhiteSpace(engineVersion)) {
            return $"Connected to Firebird (version not reported) using {Path.GetFileName(sampleDatabase)}.";
        }

        return $"Connected to Firebird {engineVersion} using {Path.GetFileName(sampleDatabase)}.";
    }

    #endregion Private Methods

    #region Native Methods

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    #endregion Native Methods

    private sealed class ExpandableDescriptionPanel : Panel {
        private readonly string _fullText;
        private readonly Label _descriptionLabel;
        private readonly PictureBox _arrowPicture;
        private bool _isExpanded;

        public ExpandableDescriptionPanel(string text, Font font) {
            _fullText = text;
            Cursor = Cursors.Hand;
            AutoSize = false;
            Height = 44;

            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22));

            _descriptionLabel = new Label {
                Font = font,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };

            _arrowPicture = new PictureBox {
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };

            layout.Controls.Add(_descriptionLabel, 0, 0);
            layout.Controls.Add(_arrowPicture, 1, 0);
            Controls.Add(layout);

            Click += ToggleExpand;
            _descriptionLabel.Click += ToggleExpand;
            _arrowPicture.Click += ToggleExpand;
            Resize += (_, _) => UpdateText();
            Layout += (_, _) => UpdateText();
            VisibleChanged += (_, _) => UpdateText();
            ParentChanged += (_, _) => {
                if (Parent is not null) {
                    Parent.Layout += (_, _) => UpdateText();
                    Parent.Resize += (_, _) => UpdateText();
                }
            };
            _descriptionLabel.Resize += (_, _) => UpdateText();
            EnsureInitialMeasure();
        }

        private void EnsureInitialMeasure() {
            if (IsHandleCreated) {
                BeginInvoke(new Action(UpdateText));
            } else {
                HandleCreated += (_, _) => BeginInvoke(new Action(UpdateText));
            }
        }

        private void ToggleExpand(object? sender, EventArgs e) {
            if (!_arrowPicture.Visible) {
                return;
            }

            _isExpanded = !_isExpanded;
            UpdateText();
        }

        private void UpdateText() {
            var width = ClientSize.Width - 22;
            if (width <= 1) {
                if (IsHandleCreated) {
                    BeginInvoke(new Action(UpdateText));
                }

                return;
            }

            var lineHeight = TextRenderer.MeasureText("Ag", _descriptionLabel.Font).Height;
            var maxHeight = lineHeight * 2;
            var fullSize = TextRenderer.MeasureText(_fullText, _descriptionLabel.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak);
            var truncated = fullSize.Height > maxHeight;
            _arrowPicture.Visible = truncated;

            if (_isExpanded || !truncated) {
                _descriptionLabel.Height = fullSize.Height;
                _descriptionLabel.Text = _fullText;
            } else {
                _descriptionLabel.Height = maxHeight;
                _descriptionLabel.Text = TruncateToTwoLines(_fullText, _descriptionLabel.Font, width);
            }

            _arrowPicture.Image?.Dispose();
            _arrowPicture.Image = truncated
                ? MaterialIcons.GetIcon(_isExpanded ? "keyboard_double_arrow_up" : "keyboard_double_arrow_down", _descriptionLabel.ForeColor, 22)
                : null;

            Height = Math.Max(_descriptionLabel.Height, 22);
        }

        private static string TruncateToTwoLines(string text, Font font, int width) {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) {
                return text;
            }

            var targetHeight = TextRenderer.MeasureText("Ag", font).Height * 2;
            var low = 1;
            var high = words.Length;
            var best = words[0] + " ...";

            while (low <= high) {
                var mid = (low + high) / 2;
                var candidate = string.Join(" ", words.Take(mid)) + " ...";
                var size = TextRenderer.MeasureText(candidate, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak);
                if (size.Height <= targetHeight) {
                    best = candidate;
                    low = mid + 1;
                } else {
                    high = mid - 1;
                }
            }

            return best;
        }
    }
}
