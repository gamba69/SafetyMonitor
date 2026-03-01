using FirebirdSql.Data.FirebirdClient;
using MaterialSkin;
using SafetyMonitorView.Services;
using System.Runtime.InteropServices;

namespace SafetyMonitorView.Forms;

public enum SettingsMaintenanceAction {
    None,
    Export,
    Import,
    Reset,
}

public class SettingsForm : ThemedCaptionForm {
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
    private NumericUpDown _chartRawDataPointIntervalSecondsNumeric = null!;
    private NumericUpDown _refreshIntervalNumeric = null!;
    private NumericUpDown _valueTileLookbackMinutesNumeric = null!;
    private Button _saveButton = null!;
    private TextBox _storagePathTextBox = null!;
    private Button _testConnectionButton = null!;
    private Button _exportSettingsButton = null!;
    private Button _importSettingsButton = null!;
    private Button _resetSettingsButton = null!;
    private ComboBox _materialColorSchemeComboBox = null!;
    private Panel _configColorSectionSeparator = null!;
    private readonly AppSettingsMaintenanceService _settingsMaintenanceService;

    // Tab infrastructure
    private Panel _tabSegmentPanel = null!;
    private TableLayoutPanel _tabButtonsLayout = null!;
    private readonly List<RadioButton> _tabButtons = new();
    private readonly List<Panel> _tabPages = new();
    private int _selectedTabIndex;

    private static readonly string[] TabNames = { "Refresh", "Tray", "Database", "Tiles", "Aggregation", "Config" };
    private const int SettingValueColumnWidth = 120;
    private const int SettingUnitColumnWidth = 90;
    private const int ConfigSettingValueColumnWidth = 170;
    private const int EmSetMargins = 0x00D3;
    private const int EcLeftMargin = 0x0001;
    private const int EcRightMargin = 0x0002;
    private const int NumericTextPaddingPx = 6;
    private const int TabIconSize = 22;
    private const int TabButtonHorizontalPadding = 14;
    private const int ColorSwatchSize = 12;
    private const int ColorSwatchLeftPadding = 8;
    private const int ColorSwatchTextGap = 8;

    private static readonly string[] TabIcons = {
        "refresh",
        "pip",
        MaterialIcons.CommonDatabase,
        MaterialIcons.DashboardTab,
        MaterialIcons.CommonAvgTime,
        "rule_settings"
    };

    private sealed record MaterialSchemeComboItem(string SchemeName, string DisplayName, Color PrimaryColor) {
        public override string ToString() => DisplayName;
    }

    #endregion Private Fields

    #region Public Constructors

    public SettingsMaintenanceAction SettingsMaintenanceAction { get; private set; }

    public SettingsForm(AppSettingsMaintenanceService settingsMaintenanceService, string currentStoragePath, int currentRefreshInterval, int currentValueTileLookbackMinutes, int currentChartStaticTimeoutSeconds, double currentChartStaticAggregationPresetMatchTolerancePercent, int currentChartStaticAggregationTargetPointCount, int currentChartRawDataPointIntervalSeconds, bool currentShowRefreshIndicator, bool currentMinimizeToTray, bool currentStartMinimized, string currentMaterialColorScheme) {
        _settingsMaintenanceService = settingsMaintenanceService;
        StoragePath = currentStoragePath;
        RefreshInterval = currentRefreshInterval;
        ValueTileLookbackMinutes = Math.Max(1, currentValueTileLookbackMinutes);
        ChartStaticTimeoutSeconds = currentChartStaticTimeoutSeconds;
        ChartStaticAggregationPresetMatchTolerancePercent = Math.Clamp(currentChartStaticAggregationPresetMatchTolerancePercent, 0, 100);
        ChartStaticAggregationTargetPointCount = Math.Max(2, currentChartStaticAggregationTargetPointCount);
        ChartRawDataPointIntervalSeconds = Math.Max(1, currentChartRawDataPointIntervalSeconds);
        ShowRefreshIndicator = currentShowRefreshIndicator;
        MinimizeToTray = currentMinimizeToTray;
        StartMinimized = currentStartMinimized;
        MaterialColorScheme = AppColorizationService.Instance.NormalizeMaterialSchemeName(currentMaterialColorScheme);

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
    public int ChartRawDataPointIntervalSeconds { get; private set; } = 3;
    public string StoragePath { get; private set; } = "";
    public bool ShowRefreshIndicator { get; private set; } = true;
    public bool MinimizeToTray { get; private set; } = false;
    public bool StartMinimized { get; private set; } = false;
    public string MaterialColorScheme { get; private set; } = "Teal";

    #endregion Public Properties

    #region Private Methods

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        var palette = AppColorizationService.Instance.GetNeutralPalette(isLight);
        BackColor = palette.FormBackground;
        ForeColor = palette.StrongText;

        ApplyTabSegmentTheme(isLight);
        ApplyThemeRecursive(this, isLight);
        if (_materialColorSchemeComboBox != null) {
            _materialColorSchemeComboBox.DrawItem -= MaterialColorSchemeComboBox_DrawItem;
            _materialColorSchemeComboBox.DrawItem += MaterialColorSchemeComboBox_DrawItem;
        }
        ApplySettingSwitchTheme(isLight);
        ApplyConfigButtonsTheme(isLight);
        if (_configColorSectionSeparator != null) {
            _configColorSectionSeparator.BackColor = palette.Border;
        }
    }

    private void ApplyConfigButtonsTheme(bool isLight) {
        if (_exportSettingsButton != null) {
            ThemedButtonStyler.Apply(_exportSettingsButton, isLight);
            _exportSettingsButton.Image?.Dispose();
            _exportSettingsButton.Image = MaterialIcons.GetIcon("output_circle", _exportSettingsButton.ForeColor, 24);
        }

        if (_importSettingsButton != null) {
            ThemedButtonStyler.Apply(_importSettingsButton, isLight);
            _importSettingsButton.Image?.Dispose();
            _importSettingsButton.Image = MaterialIcons.GetIcon("input_circle", _importSettingsButton.ForeColor, 24);
        }

        if (_resetSettingsButton != null) {
            ThemedButtonStyler.Apply(_resetSettingsButton, isLight);
            _resetSettingsButton.Image?.Dispose();
            _resetSettingsButton.Image = MaterialIcons.GetIcon("dangerous", _resetSettingsButton.ForeColor, 24);
        }
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

            var palette = AppColorizationService.Instance.GetNeutralPalette(isLight);
            switch (control) {
                case Label lbl:
                    lbl.ForeColor = palette.StrongText;
                    break;
                case Button btn:
                    ThemedButtonStyler.Apply(btn, isLight);
                    break;
                case TextBox txt:
                    txt.BackColor = palette.InputBackground;
                    txt.ForeColor = palette.StrongText;
                    break;
                case NumericUpDown num:
                    num.BackColor = palette.InputBackground;
                    num.ForeColor = palette.StrongText;
                    break;
                case ComboBox comboBox:
                    ThemedComboBoxStyler.Apply(comboBox, isLight);
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
            RowCount = 5,
            AutoSize = false
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 0: ThemedCaptionForm header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 1: Tab strip
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2: Tab content
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));    // 3: Save / Cancel

        // ── Row 0: ThemedCaptionForm header description ──
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
        _tabPages.Add(CreateConfigTab(titleFont, normalFont, descriptionFont));

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

        const int minHorizontalPadding = 6;
        const int iconAndGapWidth = TabIconSize + 12;
        const int extraChromeWidth = 20;

        var preferredWidths = new int[_tabButtons.Count];
        var minimumWidths = new int[_tabButtons.Count];
        var preferredTotalWidth = 0;
        var minimumTotalWidth = 0;

        for (int i = 0; i < _tabButtons.Count; i++) {
            var button = _tabButtons[i];
            var textWidth = TextRenderer.MeasureText(
                button.Text,
                button.Font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.SingleLine).Width;

            var preferredWidth = textWidth + iconAndGapWidth + (TabButtonHorizontalPadding * 2) + extraChromeWidth;
            var minimumWidth = textWidth + iconAndGapWidth + (minHorizontalPadding * 2) + extraChromeWidth;

            preferredWidths[i] = Math.Max(preferredWidth, 96);
            minimumWidths[i] = Math.Max(minimumWidth, 78);
            preferredTotalWidth += preferredWidths[i];
            minimumTotalWidth += minimumWidths[i];
        }

        var tabsHorizontalChrome = _tabSegmentPanel.Padding.Horizontal + Padding.Horizontal + 10;
        var screenWidthLimit = (int)(Screen.FromControl(this).WorkingArea.Width * 0.95);
        var maxClientWidth = Math.Max(682, screenWidthLimit);
        var preferredClientWidth = Math.Min(maxClientWidth, preferredTotalWidth + tabsHorizontalChrome);
        if (ClientSize.Width < preferredClientWidth) {
            ClientSize = new Size(preferredClientWidth, ClientSize.Height);
        }

        var availableTabsWidth = Math.Max(1, _tabSegmentPanel.ClientSize.Width - _tabSegmentPanel.Padding.Horizontal);
        var minimumClientWidth = Math.Max(682, minimumTotalWidth + tabsHorizontalChrome);
        if (ClientSize.Width < minimumClientWidth) {
            ClientSize = new Size(Math.Min(maxClientWidth, minimumClientWidth), ClientSize.Height);
            availableTabsWidth = Math.Max(1, _tabSegmentPanel.ClientSize.Width - _tabSegmentPanel.Padding.Horizontal);
        }

        var finalWidths = new int[_tabButtons.Count];
        if (preferredTotalWidth <= availableTabsWidth) {
            Array.Copy(preferredWidths, finalWidths, finalWidths.Length);
        } else if (minimumTotalWidth >= availableTabsWidth) {
            Array.Copy(minimumWidths, finalWidths, finalWidths.Length);
        } else {
            var shrinkBudget = preferredTotalWidth - availableTabsWidth;
            var totalShrinkable = preferredTotalWidth - minimumTotalWidth;

            for (int i = 0; i < _tabButtons.Count; i++) {
                var shrinkable = preferredWidths[i] - minimumWidths[i];
                var proportionalShrink = totalShrinkable > 0
                    ? (int)Math.Round((double)shrinkBudget * shrinkable / totalShrinkable)
                    : 0;
                finalWidths[i] = preferredWidths[i] - Math.Min(shrinkable, proportionalShrink);
            }

            var finalTotalWidth = 0;
            for (int i = 0; i < finalWidths.Length; i++) {
                finalTotalWidth += finalWidths[i];
            }

            var widthDelta = availableTabsWidth - finalTotalWidth;
            for (int i = finalWidths.Length - 1; i >= 0 && widthDelta != 0; i--) {
                var growRoom = preferredWidths[i] - finalWidths[i];
                if (widthDelta > 0 && growRoom > 0) {
                    var growBy = Math.Min(growRoom, widthDelta);
                    finalWidths[i] += growBy;
                    widthDelta -= growBy;
                    continue;
                }

                if (widthDelta < 0 && finalWidths[i] > minimumWidths[i]) {
                    var shrinkBy = Math.Min(finalWidths[i] - minimumWidths[i], -widthDelta);
                    finalWidths[i] -= shrinkBy;
                    widthDelta += shrinkBy;
                }
            }
        }

        _tabButtonsLayout.SuspendLayout();
        try {
            _tabButtonsLayout.ColumnStyles.Clear();
            for (int i = 0; i < finalWidths.Length; i++) {
                _tabButtonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, finalWidths[i]));
                var isNearMinimum = finalWidths[i] <= minimumWidths[i] + 2;
                var horizontalPadding = isNearMinimum ? minHorizontalPadding : TabButtonHorizontalPadding;
                _tabButtons[i].Padding = new Padding(horizontalPadding, 0, horizontalPadding, 0);
            }
        } finally {
            _tabButtonsLayout.ResumeLayout(true);
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
            RowCount = 5,
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
            RowCount = 5,
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
            Height = 35,
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
            Height = 35,
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
            RowCount = 5,
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
            RowCount = 5,
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

        _chartRawDataPointIntervalSecondsNumeric = CreateNumeric(1, 60, 3, normalFont);
        layout.Controls.Add(CreateSettingRow(
            "Raw Data Step",
            "Expected interval between raw measurements. "
                 + "Used in raw-data point count calculations (for example in Chart Period Presets editor). "
                 + "Increase it if devices send data less frequently than once per second.",
            "sec",
            _chartRawDataPointIntervalSecondsNumeric,
            titleFont,
            descriptionFont), 0, 3);

        page.Controls.Add(layout);
        return page;
    }

    private Panel CreateConfigTab(Font titleFont, Font normalFont, Font descriptionFont) {
        var page = new Panel {
            Padding = new Padding(0, 4, 0, 0),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _materialColorSchemeComboBox = new ComboBox {
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = normalFont,
            DrawMode = DrawMode.OwnerDrawFixed
        };
        var sortedSchemes = AppColorizationService.Instance.AvailableMaterialSchemes
            .Select(schemeName => new {
                SchemeName = schemeName,
                DisplayName = GetMaterialSchemeDisplayName(schemeName)
            })
            .OrderBy(static item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var scheme in sortedSchemes) {
            _materialColorSchemeComboBox.Items.Add(new MaterialSchemeComboItem(
                scheme.SchemeName,
                scheme.DisplayName,
                AppColorizationService.Instance.GetPrimaryActionColor(scheme.SchemeName)));
        }

        _materialColorSchemeComboBox.DrawItem += MaterialColorSchemeComboBox_DrawItem;

        layout.Controls.Add(CreateSettingRow(
            "Color scheme",
            "Choose a colorization scheme that defines the main accent color of the application (header, save buttons, menu selection, etc.).",
            string.Empty,
            _materialColorSchemeComboBox,
            titleFont,
            descriptionFont,
            ConfigSettingValueColumnWidth), 0, 0);

        _configColorSectionSeparator = new Panel {
            Dock = DockStyle.Top,
            Height = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        layout.Controls.Add(_configColorSectionSeparator, 0, 1);

        _exportSettingsButton = CreateSettingsActionButton("Export...", "output_circle", normalFont);
        _exportSettingsButton.Click += ExportSettingsButton_Click;
        layout.Controls.Add(CreateSettingRow(
            "Export settings",
            "Export all application settings as a ZIP backup archive: dashboards, visual preferences, chart presets, and other configuration files. "
            + "By default, backup files are created in the settings folder under Backup and use the YYYY-MM-DD.zip naming format.",
            "",
            _exportSettingsButton,
            titleFont,
            descriptionFont,
            ConfigSettingValueColumnWidth), 0, 2);

        _importSettingsButton = CreateSettingsActionButton("Import...", "input_circle", normalFont);
        _importSettingsButton.Click += ImportSettingsButton_Click;
        layout.Controls.Add(CreateSettingRow(
            "Import settings",
            "Import a previously exported settings ZIP archive and fully replace current configuration. "
            + "After import, the application refreshes dashboards and visual state to reflect loaded settings.",
            "",
            _importSettingsButton,
            titleFont,
            descriptionFont,
            ConfigSettingValueColumnWidth), 0, 3);

        _resetSettingsButton = CreateSettingsActionButton("Reset...", "dangerous", normalFont);
        _resetSettingsButton.Click += ResetSettingsButton_Click;
        layout.Controls.Add(CreateSettingRow(
            "Reset settings",
            "Restore all settings to default values. This clears current dashboard and custom configuration data, then rebuilds default startup settings.",
            "",
            _resetSettingsButton,
            titleFont,
            descriptionFont,
            ConfigSettingValueColumnWidth), 0, 4);
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

    private static Button CreateSettingsActionButton(string text, string iconName, Font font) {
        var button = new Button {
            Text = text,
            Width = 130,
            Height = 35,
            Font = font,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 0)
        };

        button.Image = MaterialIcons.GetIcon(iconName, Color.White, 24);
        return button;
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


    private static TableLayoutPanel CreateSettingRow(string title, string description, string units, Control valueControl, Font titleFont, Font descriptionFont, int valueColumnWidth = SettingValueColumnWidth, int unitColumnWidth = SettingUnitColumnWidth) {
        var row = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 16)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, valueColumnWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, unitColumnWidth));
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

        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.FromArgb(195, 205, 210) : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.Black : Color.White;
        var background = checkBox.Checked ? activeBg : segmentBg;

        checkBox.BackColor = background;
        checkBox.ForeColor = checkBox.Checked ? activeFg : inactiveFg;
        checkBox.FlatAppearance.BorderSize = 0;
        checkBox.FlatAppearance.BorderColor = background;
        checkBox.FlatAppearance.CheckedBackColor = activeBg;
        checkBox.FlatAppearance.MouseOverBackColor = isLight ? Color.FromArgb(235, 240, 243) : Color.FromArgb(55, 70, 76);
        checkBox.FlatAppearance.MouseDownBackColor = activeBg;

        checkBox.Text = checkBox.Checked ? "ON" : "OFF";
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
        _chartRawDataPointIntervalSecondsNumeric.Value = Math.Clamp(ChartRawDataPointIntervalSeconds, 1, 60);

        if (_materialColorSchemeComboBox.Items.Count > 0) {
            var selectedItem = _materialColorSchemeComboBox
                .Items
                .OfType<MaterialSchemeComboItem>()
                .FirstOrDefault(item => string.Equals(item.SchemeName, MaterialColorScheme, StringComparison.OrdinalIgnoreCase));

            _materialColorSchemeComboBox.SelectedItem = selectedItem;
            if (_materialColorSchemeComboBox.SelectedIndex < 0) {
                _materialColorSchemeComboBox.SelectedIndex = 0;
            }
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ApplySettingSwitchTheme(isLight);
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        SettingsMaintenanceAction = SettingsMaintenanceAction.None;
        StoragePath = _storagePathTextBox.Text;
        RefreshInterval = (int)_refreshIntervalNumeric.Value;
        ShowRefreshIndicator = _showRefreshIndicatorSwitch.Checked;
        MinimizeToTray = _minimizeToTraySwitch.Checked;
        StartMinimized = _startMinimizedSwitch.Checked;
        var selectedSchemeName = (_materialColorSchemeComboBox.SelectedItem as MaterialSchemeComboItem)?.SchemeName;
        MaterialColorScheme = AppColorizationService.Instance.NormalizeMaterialSchemeName(selectedSchemeName);
        ValueTileLookbackMinutes = (int)_valueTileLookbackMinutesNumeric.Value;
        ChartStaticTimeoutSeconds = (int)_chartStaticTimeoutNumeric.Value;
        ChartStaticAggregationPresetMatchTolerancePercent = (double)_chartStaticAggregationPresetMatchToleranceNumeric.Value;
        ChartStaticAggregationTargetPointCount = (int)_chartStaticAggregationTargetPointsNumeric.Value;
        ChartRawDataPointIntervalSeconds = (int)_chartRawDataPointIntervalSecondsNumeric.Value;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void MaterialColorSchemeComboBox_DrawItem(object? sender, DrawItemEventArgs e) {
        if (sender is not ComboBox comboBox || e.Index < 0 || e.Index >= comboBox.Items.Count) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var bg = comboBox.BackColor;
        var fg = comboBox.ForeColor;

        if ((e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0) {
            bg = isLight ? SystemColors.Highlight : MaterialSkinManager.Instance.ColorScheme.PrimaryColor;
            fg = isLight ? SystemColors.HighlightText : Color.White;
        }

        using var bgBrush = new SolidBrush(bg);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        if (comboBox.Items[e.Index] is not MaterialSchemeComboItem item) {
            var fallbackText = comboBox.GetItemText(comboBox.Items[e.Index]);
            TextRenderer.DrawText(e.Graphics, fallbackText, e.Font ?? comboBox.Font, e.Bounds, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            return;
        }

        var swatchRect = new Rectangle(
            e.Bounds.Left + ColorSwatchLeftPadding,
            e.Bounds.Top + Math.Max(0, (e.Bounds.Height - ColorSwatchSize) / 2),
            ColorSwatchSize,
            ColorSwatchSize);

        using (var swatchBrush = new SolidBrush(item.PrimaryColor)) {
            e.Graphics.FillRectangle(swatchBrush, swatchRect);
        }

        using var swatchBorderPen = new Pen(Color.FromArgb(140, Color.Black));
        e.Graphics.DrawRectangle(swatchBorderPen, swatchRect);

        var textRect = new Rectangle(
            swatchRect.Right + ColorSwatchTextGap,
            e.Bounds.Top,
            Math.Max(0, e.Bounds.Width - (swatchRect.Right - e.Bounds.Left) - ColorSwatchTextGap - 4),
            e.Bounds.Height);

        TextRenderer.DrawText(e.Graphics, item.DisplayName, e.Font ?? comboBox.Font, textRect, fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void ExportSettingsButton_Click(object? sender, EventArgs e) {
        using var dialog = new SaveFileDialog {
            Filter = "ZIP archive (*.zip)|*.zip",
            Title = "Export settings",
            FileName = Path.GetFileName(_settingsMaintenanceService.GetDefaultBackupFilePath()),
            InitialDirectory = Path.GetDirectoryName(_settingsMaintenanceService.GetDefaultBackupFilePath())
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) {
            return;
        }

        try {
            _settingsMaintenanceService.ExportToArchive(dialog.FileName);
            SettingsMaintenanceAction = SettingsMaintenanceAction.Export;
            ThemedMessageBox.Show(this, "Settings exported successfully.", "Export settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (Exception ex) {
            ThemedMessageBox.Show(this, $"Failed to export settings: {ex.Message}", "Export settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportSettingsButton_Click(object? sender, EventArgs e) {
        var confirm = ThemedMessageBox.Show(this,
            "All current settings will be lost and replaced with settings from the selected archive. Continue?",
            "Import settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) {
            return;
        }

        using var dialog = new OpenFileDialog {
            Filter = "ZIP archive (*.zip)|*.zip",
            Title = "Import settings"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) {
            return;
        }

        try {
            _settingsMaintenanceService.ImportFromArchive(dialog.FileName);
            SettingsMaintenanceAction = SettingsMaintenanceAction.Import;
            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            ThemedMessageBox.Show(this, $"Failed to import settings: {ex.Message}", "Import settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetSettingsButton_Click(object? sender, EventArgs e) {
        var confirm = ThemedMessageBox.Show(this,
            "All settings will be restored to defaults. Continue?",
            "Reset settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) {
            return;
        }

        try {
            _settingsMaintenanceService.ResetToDefaults();
            SettingsMaintenanceAction = SettingsMaintenanceAction.Reset;
            DialogResult = DialogResult.OK;
            Close();
        } catch (Exception ex) {
            ThemedMessageBox.Show(this, $"Failed to reset settings: {ex.Message}", "Reset settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string GetMaterialSchemeDisplayName(string schemeName) {
        return schemeName switch {
            "BlueGray" => "Gray",
            "DeepOrange" => "Orange",
            _ => schemeName
        };
    }

    private static Color GetConnectionStatusColor(bool isSuccess) {
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        if (isSuccess) {
            return isLight ? Color.Black : Color.White;
        }

        return Color.FromArgb(220, 0, 0);
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
        private bool _isExpandable;

        public ExpandableDescriptionPanel(string text, Font font) {
            _fullText = text;
            Cursor = Cursors.Default;
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
                Cursor = Cursors.Default
            };

            _arrowPicture = new PictureBox {
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Default,
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
            if (!_isExpandable) {
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

            _isExpandable = truncated;
            var cursor = _isExpandable ? Cursors.Hand : Cursors.Default;
            Cursor = cursor;
            _descriptionLabel.Cursor = cursor;
            _arrowPicture.Cursor = cursor;

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
