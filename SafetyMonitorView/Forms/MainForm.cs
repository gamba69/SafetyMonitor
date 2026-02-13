using MaterialSkin;
using MaterialSkin.Controls;
using SafetyMonitorView.Controls;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ColorScheme = MaterialSkin.ColorScheme;

namespace SafetyMonitorView.Forms;

public class MainForm : MaterialForm {

    #region Private Fields

    private readonly AppSettings _appSettings = null!;
    private readonly AppSettingsService _appSettingsService;
    private readonly DashboardService _dashboardService;
    private readonly bool _isLoading = true;
    private readonly MaterialSkinManager _skinManager;
    private Label _chartsLabel = null!;
    private Dashboard? _currentDashboard;
    private Label _dashboardLabel = null!;
    private RadioButton _darkThemeButton = null!;
    private Panel _dashboardContainer = null!;
    private DashboardPanel? _dashboardPanel;
    private List<Dashboard> _dashboards = [];
    private ToolStripStatusLabel _dataPathLabel = null!;
    private DataService _dataService;
    private RadioButton _linkedChartsButton = null!;
    private CheckBox _linkChartsCheckBox = null!;
    private RadioButton _unlinkedChartsButton = null!;
    private RadioButton _lightThemeButton = null!;
    private MenuStrip _mainMenu = null!;
    private ThemedMenuRenderer _menuRenderer = null!;
    private const int MenuIconSize = 16;
    private Panel _quickAccessPanel = null!;
    private Panel _quickDashboardsPanel = null!;
    private Panel _linkSegmentPanel = null!;
    private Label _themeLabel = null!;
    private Panel _themeSegmentPanel = null!;
    private System.Windows.Forms.Timer? _refreshTimer;
    private ToolStripStatusLabel _statusLabel = null!;
    private StatusStrip _statusStrip = null!;

    private System.Windows.Forms.Timer? _themeTimer;
    private bool _isExitConfirmed;
    private bool _restoreToMaximizedAfterMinimize;
    private bool _shouldStartMaximized;
    private FormWindowState _windowStateBeforeMinimize = FormWindowState.Normal;

    #endregion Private Fields

    #region Public Constructors

    public MainForm() {
        _skinManager = MaterialSkinManager.Instance;
        _skinManager.AddFormToManage(this);

        _dashboardService = new DashboardService();
        _appSettingsService = new AppSettingsService();

        // Load settings BEFORE initializing UI
        _appSettings = _appSettingsService.LoadSettings();
        if (_appSettings.ChartPeriodPresets == null || _appSettings.ChartPeriodPresets.Count == 0) {
            _appSettings.ChartPeriodPresets = ChartPeriodPresetStore.CreateDefaultPresets();
        }
        ChartPeriodPresetStore.SetPresets(_appSettings.ChartPeriodPresets);
        MetricAxisRuleStore.SetRules(_appSettings.MetricAxisRules);

        // Apply theme from settings
        _skinManager.Theme = _appSettings.IsDarkTheme
            ? MaterialSkinManager.Themes.DARK
            : MaterialSkinManager.Themes.LIGHT;

        _skinManager.ColorScheme = new ColorScheme(
            Primary.Teal700, Primary.Teal900, Primary.Teal500,
            Accent.Teal200, TextShade.WHITE
        );

        _dataService = new DataService(_appSettings.StoragePath);
        AttachDataServiceHandlers();

        // Set default font for the form
        Font = new Font("Roboto", 9f, FontStyle.Regular);

        InitializeComponent();
        ApplyWindowSettings();
        LoadDashboards();
        UpdateStatusBar();
        SetupRefreshTimer();

        _isLoading = false;
    }

    #endregion Public Constructors

    #region Protected Methods

    protected override void OnFormClosing(FormClosingEventArgs e) {
        if (!_isExitConfirmed && e.CloseReason == CloseReason.UserClosing) {
            var confirmExit = ThemedMessageBox.Show(
                this,
                "Exit Safety Monitor?",
                "Exit Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmExit != DialogResult.Yes) {
                e.Cancel = true;
                return;
            }

            _isExitConfirmed = true;
        }

        // Save window settings before closing
        SaveWindowSettings();

        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _themeTimer?.Stop();
        _themeTimer?.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnMove(EventArgs e) {
        base.OnMove(e);

        // ИЗМЕНЕНО: сохранять только после загрузки и в нормальном состоянии
        if (!_isLoading && WindowState == FormWindowState.Normal) {
            SaveWindowSettings();
        }
    }

    protected override void OnResize(EventArgs e) {
        base.OnResize(e);

        if (!_isLoading) {
            if (WindowState == FormWindowState.Minimized) {
                _restoreToMaximizedAfterMinimize = _windowStateBeforeMinimize == FormWindowState.Maximized;
            } else {
                if (_windowStateBeforeMinimize == FormWindowState.Minimized
                    && _restoreToMaximizedAfterMinimize
                    && WindowState == FormWindowState.Normal) {
                    BeginInvoke(() => {
                        if (WindowState == FormWindowState.Normal) {
                            WindowState = FormWindowState.Maximized;
                        }
                    });
                }

                _restoreToMaximizedAfterMinimize = false;
            }

            _windowStateBeforeMinimize = WindowState;
        }

        // ИЗМЕНЕНО: сохранять только после загрузки
        if (!_isLoading && (WindowState == FormWindowState.Maximized || WindowState == FormWindowState.Normal)) {
            SaveWindowSettings();
        }
    }

    protected override void OnShown(EventArgs e) {
        base.OnShown(e);

        if (_shouldStartMaximized && WindowState != FormWindowState.Maximized) {
            WindowState = FormWindowState.Maximized;
        }

        // Re-apply theme after MaterialSkinManager has finished its initialization
        ScheduleThemeReapply();
    }

    #endregion Protected Methods

    #region Private Methods

    private static void ApplyQuickAccessColors(Control parent, Color bg, Color fg) {
        foreach (Control control in parent.Controls) {
            if (control is RadioButton { Appearance: Appearance.Button }) {
                continue;
            }

            control.BackColor = bg;
            control.ForeColor = fg;
            if (control.HasChildren) {
                ApplyQuickAccessColors(control, bg, fg);
            }
        }
    }

    private static ToolStripMenuItem CreateMenuItem(string text, string iconName, Color iconColor, EventHandler? onClick) {
        var item = new ToolStripMenuItem(text) {
            Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize)
        };
        if (onClick != null) {
            item.Click += onClick;
        }
        return item;
    }

    private static string GetIconNameForMenuItem(string text) {
        return text switch {
            "Settings" => "settings",
            "Exit" => "exit",
            "Theme" => "theme",
            "Light" => "light",
            "Dark" => "dark",
            "About" => "about",
            "New Dashboard" => "add",
            "Edit Current" => "edit",
            "Duplicate Current" => "copy",
            "Delete Current" => "delete",
            "Axis Rules..." => "chart",
            "Chart Periods..." => "schedule",
            "Color Schemes..." => "palette",
            _ => ""
        };
    }

    private static void UpdateMenuItemsIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is ToolStripMenuItem menuItem) {
                // Update icon color based on item text
                var iconName = GetIconNameForMenuItem(menuItem.Text!);
                if (!string.IsNullOrEmpty(iconName)) {
                    menuItem.Image?.Dispose();
                    menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize);
                }

                // Recursively update submenu items
                if (menuItem.HasDropDownItems) {
                    UpdateMenuItemsIcons(menuItem.DropDownItems, iconColor);
                }
            }
        }
    }

    private void ApplyWindowSettings() {
        // Apply window size
        if (_appSettings.WindowWidth > 0 && _appSettings.WindowHeight > 0) {
            Size = new Size(_appSettings.WindowWidth, _appSettings.WindowHeight);
        } else {
            Size = new Size(1400, 900);  // Default size
        }

        // Apply window position
        if (_appSettings.WindowX >= 0 && _appSettings.WindowY >= 0) {
            StartPosition = FormStartPosition.Manual;
            Location = new Point(_appSettings.WindowX, _appSettings.WindowY);

            // Ensure window is visible on at least one screen
            var bounds = new Rectangle(Location, Size);
            var isVisible = false;
            foreach (var screen in Screen.AllScreens) {
                if (screen.WorkingArea.IntersectsWith(bounds)) {
                    isVisible = true;
                    break;
                }
            }

            if (!isVisible) {
                StartPosition = FormStartPosition.CenterScreen;
            }
        } else {
            StartPosition = FormStartPosition.CenterScreen;
        }

        // Apply maximized state after the form is shown.
        // Restoring maximized state before the first show can produce incorrect work area bounds.
        _shouldStartMaximized = _appSettings.IsMaximized;
    }

    private void CreateMenuItems() {
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(CreateMenuItem("Settings", "settings", iconColor, (s, e) => ShowSettings()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(CreateMenuItem("Exit", "exit", iconColor, (s, e) => Close()));

        var dashboardMenu = new ToolStripMenuItem("Dashboards");
        UpdateDashboardMenu(dashboardMenu);

        var viewMenu = new ToolStripMenuItem("View");
        var themeMenu = CreateMenuItem("Theme", "theme", iconColor, null);
        themeMenu.DropDownItems.Add(CreateMenuItem("Light", "light", iconColor, (s, e) => { _lightThemeButton.Checked = true; }));
        themeMenu.DropDownItems.Add(CreateMenuItem("Dark", "dark", iconColor, (s, e) => { _darkThemeButton.Checked = true; }));
        viewMenu.DropDownItems.Add(themeMenu);
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add(CreateMenuItem("Axis Rules...", "chart", iconColor, (s, e) => ShowAxisRulesEditor()));
        viewMenu.DropDownItems.Add(CreateMenuItem("Chart Periods...", "schedule", iconColor, (s, e) => ShowChartPeriodPresetEditor()));
        viewMenu.DropDownItems.Add(CreateMenuItem("Color Schemes...", "palette", iconColor, (s, e) => ShowColorSchemeEditor()));

        var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add(CreateMenuItem("About", "about", iconColor, (s, e) => ShowAbout()));

        _mainMenu.Items.AddRange([fileMenu, dashboardMenu, viewMenu, helpMenu]);
    }

    private void CreateNewDashboard() {
        var dashboard = new Dashboard {
            Name = $"New Dashboard {_dashboards.Count + 1}",
            Rows = 4,
            Columns = 4
        };
        _dashboardService.SaveDashboard(dashboard);
        _dashboards.Add(dashboard);
        LoadDashboard(dashboard);
    }

    private void CreateQuickAccessControls() {
        _dashboardLabel = new Label {
            Text = "Dashboard",
            AutoSize = true,
            Font = new Font("Roboto", 9f, FontStyle.Bold)
        };

        _chartsLabel = new Label {
            Text = "Charts",
            AutoSize = true,
            Font = new Font("Roboto", 9f, FontStyle.Bold)
        };

        _themeLabel = new Label {
            Text = "Theme",
            AutoSize = true,
            Font = new Font("Roboto", 9f, FontStyle.Bold)
        };

        _lightThemeButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 0,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            Font = new Font("Roboto", 9f, FontStyle.Bold),
            Padding = Padding.Empty,
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = !_appSettings.IsDarkTheme
        };
        _lightThemeButton.CheckedChanged += (s, e) => {
            if (_lightThemeButton.Checked) {
                SetTheme(MaterialSkinManager.Themes.LIGHT);
                _appSettings.IsDarkTheme = false;
                _appSettingsService.SaveSettings(_appSettings);
                UpdateThemeSwitchAppearance();
            }
        };

        _darkThemeButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 0,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            Font = new Font("Roboto", 9f, FontStyle.Bold),
            Padding = Padding.Empty,
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = _appSettings.IsDarkTheme
        };
        _darkThemeButton.CheckedChanged += (s, e) => {
            if (_darkThemeButton.Checked) {
                SetTheme(MaterialSkinManager.Themes.DARK);
                _appSettings.IsDarkTheme = true;
                _appSettingsService.SaveSettings(_appSettings);
                UpdateThemeSwitchAppearance();
            }
        };

        _themeSegmentPanel = new Panel {
            Location = new Point(10, 10),
            Size = new Size(74, 32),
            Padding = new Padding(1)
        };
        _themeSegmentPanel.Controls.Add(_lightThemeButton);
        _themeSegmentPanel.Controls.Add(_darkThemeButton);

        _linkedChartsButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 0,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = _appSettings.LinkChartPeriods
        };
        _linkedChartsButton.CheckedChanged += (s, e) => {
            if (_linkedChartsButton.Checked) {
                _linkChartsCheckBox.Checked = true;
            }
        };

        _unlinkedChartsButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 0,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = !_appSettings.LinkChartPeriods
        };
        _unlinkedChartsButton.CheckedChanged += (s, e) => {
            if (_unlinkedChartsButton.Checked) {
                _linkChartsCheckBox.Checked = false;
            }
        };

        _linkSegmentPanel = new Panel {
            Location = new Point(10, 10),
            Size = new Size(74, 32),
            Padding = new Padding(1)
        };
        _linkSegmentPanel.Controls.Add(_linkedChartsButton);
        _linkSegmentPanel.Controls.Add(_unlinkedChartsButton);

        _quickDashboardsPanel = new Panel {
            Location = new Point(240, 10),
            Size = new Size(0, 32),
            Padding = new Padding(1),
            Font = new Font("Roboto", 9f, FontStyle.Bold)
        };

        _linkChartsCheckBox = new CheckBox {
            Visible = false,
            AutoSize = true,
            Checked = _appSettings.LinkChartPeriods
        };
        _linkChartsCheckBox.CheckedChanged += (s, e) => {
            _appSettings.LinkChartPeriods = _linkChartsCheckBox.Checked;
            _appSettingsService.SaveSettings(_appSettings);
            _dashboardPanel?.SetLinkChartPeriods(_linkChartsCheckBox.Checked);
            UpdateLinkSwitchAppearance();
        };

        _quickAccessPanel.Controls.AddRange([
            _dashboardLabel,
            _quickDashboardsPanel,
            _chartsLabel,
            _linkSegmentPanel,
            _themeLabel,
            _themeSegmentPanel,
            _linkChartsCheckBox
        ]);

        RefreshQuickAccessLayout();
        _quickAccessPanel.SizeChanged += (s, e) => RefreshQuickAccessLayout();
    }

    private void DeleteCurrentDashboard() {
        if (_currentDashboard == null || _dashboards.Count <= 1) {
            ThemedMessageBox.Show(this, "Cannot delete the last dashboard", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = ThemedMessageBox.Show(this, $"Delete dashboard '{_currentDashboard.Name}'?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes) {
            _dashboardService.DeleteDashboard(_currentDashboard);
            _dashboards.Remove(_currentDashboard);
            LoadDashboard(_dashboards[0]);
        }
    }

    private void DuplicateCurrentDashboard() {
        if (_currentDashboard == null) {
            return;
        }

        var copy = _dashboardService.DuplicateDashboard(_currentDashboard);
        _dashboards.Add(copy);
        LoadDashboard(copy);
    }

    private void EditCurrentDashboard() {
        if (_currentDashboard == null) {
            return;
        }

        using var editor = new DashboardEditorForm(_currentDashboard);
        if (editor.ShowDialog() == DialogResult.OK && editor.Modified) {
            _dashboardService.SaveDashboard(_currentDashboard);
            _dashboards = _dashboardService.LoadDashboards();

            var updatedDashboard = _dashboards.FirstOrDefault(d => d.Id == _currentDashboard.Id);
            if (updatedDashboard != null) {
                LoadDashboard(updatedDashboard);
            }

            if (_mainMenu.Items.Count > 1) {
                var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1];
                UpdateDashboardMenu(dashboardMenu);
            }

            UpdateQuickDashboards();
        }
    }

    private void InitializeComponent() {
        SuspendLayout();

        Text = "SafetyMonitorView - Safety Monitor Dashboard";
        // УБРАНО: Size и StartPosition будут установлены в ApplyWindowSettings

        _mainMenu = new MenuStrip {
            Dock = DockStyle.Top
        };
        _menuRenderer = new ThemedMenuRenderer();
        _menuRenderer.UpdateTheme();
        _mainMenu.Renderer = _menuRenderer;
        ToolStripManager.Renderer = _menuRenderer;
        ToolStripManager.VisualStylesEnabled = false;
        _mainMenu.BackColor = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(250, 250, 250)
            : Color.FromArgb(35, 47, 52);
        CreateMenuItems();

        _quickAccessPanel = new Panel {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 5, 10, 5)
        };
        CreateQuickAccessControls();
        UpdateQuickAccessPanelTheme();

        _dashboardContainer = new Panel {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        UpdateDashboardContainerTheme();

        _statusStrip = new StatusStrip {
            Dock = DockStyle.Bottom
        };
        _statusLabel = new ToolStripStatusLabel("Ready") {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _dataPathLabel = new ToolStripStatusLabel("Storage: not configured") {
            BorderSides = ToolStripStatusLabelBorderSides.Left
        };
        _statusStrip.Items.AddRange([_statusLabel, _dataPathLabel]);

        Controls.Add(_dashboardContainer);
        Controls.Add(_quickAccessPanel);
        Controls.Add(_statusStrip);
        Controls.Add(_mainMenu);

        MainMenuStrip = _mainMenu;
        MinimumSize = new Size(800, 600);

        ResumeLayout(false);
        PerformLayout();
    }
    private void LoadDashboard(Dashboard dashboard) {
        _currentDashboard = dashboard;

        // Save last dashboard ID
        _appSettings.LastDashboardId = dashboard.Id;
        _appSettingsService.SaveSettings(_appSettings);

        if (_dashboardPanel != null) {
            _dashboardContainer.Controls.Remove(_dashboardPanel);
            _dashboardPanel.Dispose();
        }

        _dashboardPanel = new DashboardPanel(dashboard, _dataService, _appSettings.ChartStaticModeTimeoutSeconds) { Dock = DockStyle.Fill };
        _dashboardPanel.DashboardChanged += OnDashboardChanged;
        _dashboardPanel.SetLinkChartPeriods(_appSettings.LinkChartPeriods);
        _dashboardContainer.Controls.Add(_dashboardPanel);
        _statusLabel.Text = $"Dashboard: {dashboard.Name}";

        if (_mainMenu.Items.Count > 1) {
            var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1];
            UpdateDashboardMenu(dashboardMenu);
        }

        UpdateQuickDashboardSelection();

        // Refresh data immediately instead of waiting for timer tick
        if (IsHandleCreated) {
            BeginInvoke(() => _dashboardPanel?.RefreshData());
        } else {
            HandleCreated += OnFirstHandleCreated;
        }

        // Reset timer so next tick is a full interval from now
        if (_refreshTimer != null) {
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }
    }

    private void LoadDashboards() {
        _dashboards = _dashboardService.LoadDashboards();

        if (_dashboards.Count > 0) {
            // Try to load last dashboard from settings
            Dashboard? dashboardToLoad = null;

            if (_appSettings.LastDashboardId.HasValue) {
                dashboardToLoad = _dashboards.FirstOrDefault(d => d.Id == _appSettings.LastDashboardId.Value);
            }

            // Fall back to first dashboard if last one not found
            dashboardToLoad ??= _dashboards[0];

            LoadDashboard(dashboardToLoad);
        }

        if (_mainMenu.Items.Count > 1) {
            var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1];
            UpdateDashboardMenu(dashboardMenu);
        }

        UpdateQuickDashboards();
    }

    private void OnFirstHandleCreated(object? sender, EventArgs e) {
        HandleCreated -= OnFirstHandleCreated;
        BeginInvoke(() => _dashboardPanel?.RefreshData());
    }

    private void OnDashboardChanged() {
        if (_currentDashboard == null) {
            return;
        }

        _dashboardService.SaveDashboard(_currentDashboard);
    }

    private void SaveWindowSettings() {
        _appSettings.IsMaximized = WindowState == FormWindowState.Maximized;

        // Persist normal bounds even when the window is maximized.
        // This avoids stale geometry that can cause incorrect maximize behavior on next startup.
        var normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        if (normalBounds.Width > 0 && normalBounds.Height > 0) {
            _appSettings.WindowWidth = normalBounds.Width;
            _appSettings.WindowHeight = normalBounds.Height;
            _appSettings.WindowX = normalBounds.Left;
            _appSettings.WindowY = normalBounds.Top;
        }

        _appSettingsService.SaveSettings(_appSettings);
    }
    private void ScheduleThemeReapply() {
        if (_themeTimer != null) {
            _themeTimer.Stop();
            _themeTimer.Dispose();
        }
        _themeTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _themeTimer.Tick += (s, e) => {
            _themeTimer!.Stop();
            _themeTimer.Dispose();
            _themeTimer = null;
            UpdateDashboardContainerTheme();
            UpdateQuickAccessPanelTheme();
            UpdateMenuTheme();
            _dashboardPanel?.UpdateTheme();
        };
        _themeTimer.Start();
    }

    private void SetTheme(MaterialSkinManager.Themes theme) {
        _skinManager.Theme = theme;
        // MaterialSkinManager queues multiple deferred color updates.
        // A timer guarantees we reapply our colors AFTER MSM is fully done.
        ScheduleThemeReapply();
    }

    private void SetupRefreshTimer() {
        _refreshTimer = new System.Windows.Forms.Timer { Interval = _appSettings.RefreshInterval * 1000 };
        _refreshTimer.Tick += (s, e) => _dashboardPanel?.RefreshData();
        _refreshTimer.Start();
    }

    private void AttachDataServiceHandlers() {
        _dataService.ConnectionFailed += OnDataServiceConnectionFailed;
    }

    private void OnDataServiceConnectionFailed(string details) {
        if (InvokeRequired) {
            BeginInvoke(() => OnDataServiceConnectionFailed(details));
            return;
        }

        _refreshTimer?.Stop();
        _statusLabel.Text = "SQL connection failed";

        var message = "Unable to connect to SQL Server. Application terminated."
            + Environment.NewLine
            + details;

        ThemedMessageBox.Show(
            this,
            message,
            "Connection error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        _isExitConfirmed = true;
        Close();
    }

    private void ShowAbout() {
        var message = "SafetyMonitorView v1.0"
            + Environment.NewLine
            + "ASCOM Alpaca"
            + Environment.NewLine
            + "Safety Monitor Dashboard"
            + Environment.NewLine
            + "©2026 DreamSky Observatory";
        ThemedMessageBox.Show(this, message,
            "About SafetyMonitorView", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowAxisRulesEditor() {
        using var editor = new AxisRulesEditorForm(_appSettings.MetricAxisRules);
        if (editor.ShowDialog(this) == DialogResult.OK) {
            _appSettings.MetricAxisRules = editor.Rules;
            _appSettingsService.SaveSettings(_appSettings);
            MetricAxisRuleStore.SetRules(_appSettings.MetricAxisRules);
        }
    }

    private void ShowColorSchemeEditor() {
        using var editor = new ColorSchemeEditorForm();
        editor.ShowDialog(this);
    }

    private void ShowChartPeriodPresetEditor() {
        using var editor = new ChartPeriodPresetEditorForm(_appSettings.ChartPeriodPresets);
        if (editor.ShowDialog(this) == DialogResult.OK) {
            _appSettings.ChartPeriodPresets = editor.Presets;
            _appSettingsService.SaveSettings(_appSettings);
            ChartPeriodPresetStore.SetPresets(_appSettings.ChartPeriodPresets);

            if (_currentDashboard != null) {
                LoadDashboard(_currentDashboard);
            }
        }
    }

    private void ShowSettings() {
        using var settingsForm = new SettingsForm(_appSettings.StoragePath, _appSettings.RefreshInterval, _appSettings.ChartStaticModeTimeoutSeconds);
        if (settingsForm.ShowDialog() == DialogResult.OK) {
            _appSettings.StoragePath = settingsForm.StoragePath;
            _appSettings.RefreshInterval = settingsForm.RefreshInterval;
            _appSettings.ChartStaticModeTimeoutSeconds = settingsForm.ChartStaticTimeoutSeconds;
            _appSettingsService.SaveSettings(_appSettings);

            _dataService = new DataService(_appSettings.StoragePath);
            AttachDataServiceHandlers();
            UpdateStatusBar();

            _refreshTimer?.Interval = _appSettings.RefreshInterval * 1000;
            _dashboardPanel?.SetChartStaticModeTimeoutSeconds(_appSettings.ChartStaticModeTimeoutSeconds);
            RefreshQuickAccessLayout();

            if (_currentDashboard != null) {
                LoadDashboard(_currentDashboard);
            }
        }
    }

    private void UpdateDashboardContainerTheme() {
        _dashboardContainer.BackColor = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(250, 250, 250)
            : Color.FromArgb(25, 36, 40);
    }

    private void UpdateDashboardMenu(ToolStripMenuItem dashboardMenu) {
        dashboardMenu.DropDownItems.Clear();

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;

        // Dashboard list items - without icons
        foreach (var dashboard in _dashboards) {
            var item = new ToolStripMenuItem(dashboard.Name) {
                Checked = dashboard.Id == _currentDashboard?.Id
            };
            item.Click += (s, e) => LoadDashboard(dashboard);
            dashboardMenu.DropDownItems.Add(item);
        }

        dashboardMenu.DropDownItems.Add(new ToolStripSeparator());

        // Management items - with icons
        dashboardMenu.DropDownItems.Add(CreateMenuItem("New Dashboard", "add", iconColor, (s, e) => CreateNewDashboard()));
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Edit Current", "edit", iconColor, (s, e) => EditCurrentDashboard()));
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Duplicate Current", "copy", iconColor, (s, e) => DuplicateCurrentDashboard()));
        dashboardMenu.DropDownItems.Add(new ToolStripSeparator());
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Delete Current", "delete", iconColor, (s, e) => DeleteCurrentDashboard()));
    }

    private void UpdateMenuTheme() {
        _menuRenderer.UpdateTheme();
        ToolStripManager.Renderer = _menuRenderer;

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;

        // Update menu background
        _mainMenu.BackColor = isLight
            ? Color.FromArgb(250, 250, 250)
            : Color.FromArgb(35, 47, 52);

        // Update all menu items icons
        UpdateMenuItemsIcons(_mainMenu.Items, iconColor);

        // Refresh menu
        _mainMenu.Refresh();
    }

    private void UpdateQuickAccessPanelTheme() {
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var panelBg = isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(25, 36, 40);
        var fg = isLight ? Color.Black : Color.White;

        _quickAccessPanel.BackColor = panelBg;
        ApplyQuickAccessColors(_quickAccessPanel, panelBg, fg);
        UpdateThemeSwitchAppearance();
        UpdateLinkSwitchAppearance();
        UpdateDashboardSwitchAppearance();
    }

    private void UpdateThemeSwitchAppearance() {
        if (_themeSegmentPanel == null || _lightThemeButton == null || _darkThemeButton == null) {
            return;
        }

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.FromArgb(21, 28, 31) : Color.White;
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        _themeSegmentPanel.BackColor = borderColor;
        _lightThemeButton.BackColor = _lightThemeButton.Checked ? activeBg : segmentBg;
        _darkThemeButton.BackColor = _darkThemeButton.Checked ? activeBg : segmentBg;
        _lightThemeButton.ForeColor = _lightThemeButton.Checked ? activeFg : inactiveFg;
        _darkThemeButton.ForeColor = _darkThemeButton.Checked ? activeFg : inactiveFg;

        var iconColor = isLight ? Color.FromArgb(35, 47, 52) : Color.FromArgb(223, 234, 239);
        _lightThemeButton.Image = MaterialIcons.GetIcon("light", iconColor, 16);
        _darkThemeButton.Image = MaterialIcons.GetIcon("dark", iconColor, 16);

        _lightThemeButton.ImageAlign = ContentAlignment.MiddleCenter;
        _darkThemeButton.ImageAlign = ContentAlignment.MiddleCenter;
    }

    private void UpdateLinkSwitchAppearance() {
        if (_linkSegmentPanel == null || _linkedChartsButton == null || _unlinkedChartsButton == null) {
            return;
        }

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.FromArgb(21, 28, 31) : Color.White;
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        _linkSegmentPanel.BackColor = borderColor;
        _linkedChartsButton.BackColor = _linkedChartsButton.Checked ? activeBg : segmentBg;
        _unlinkedChartsButton.BackColor = _unlinkedChartsButton.Checked ? activeBg : segmentBg;
        _linkedChartsButton.ForeColor = _linkedChartsButton.Checked ? activeFg : inactiveFg;
        _unlinkedChartsButton.ForeColor = _unlinkedChartsButton.Checked ? activeFg : inactiveFg;

        var iconColor = isLight ? Color.FromArgb(35, 47, 52) : Color.FromArgb(223, 234, 239);
        _linkedChartsButton.Image = MaterialIcons.GetIcon("link", iconColor, 16);
        _unlinkedChartsButton.Image = MaterialIcons.GetIcon("link_off", iconColor, 16);
        _linkedChartsButton.ImageAlign = ContentAlignment.MiddleCenter;
        _unlinkedChartsButton.ImageAlign = ContentAlignment.MiddleCenter;
    }

    private void UpdateDashboardSwitchAppearance() {
        if (_quickDashboardsPanel == null) {
            return;
        }

        var buttons = _quickDashboardsPanel.Controls.OfType<RadioButton>().ToList();
        if (buttons.Count == 0) {
            return;
        }

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.FromArgb(21, 28, 31) : Color.White;
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        _quickDashboardsPanel.BackColor = borderColor;
        foreach (var button in buttons) {
            button.BackColor = button.Checked ? activeBg : segmentBg;
            button.ForeColor = button.Checked ? activeFg : inactiveFg;
        }
    }


    private void RefreshQuickAccessLayout() {
        PositionLinkControls();
        UpdateThemeSwitchLayout();
        UpdateQuickDashboards();
    }

    private int GetQuickDashboardsLeft() {
        return _dashboardLabel.Right + 8;
    }

    private int GetQuickDashboardPanelMaxWidth() {
        var left = GetQuickDashboardsLeft();
        const int spacingToRightGroup = 16;
        return Math.Max(_chartsLabel.Left - spacingToRightGroup - left, 0);
    }

    private void UpdateThemeSwitchLayout() {
        if (_themeSegmentPanel == null || _lightThemeButton == null || _darkThemeButton == null) {
            return;
        }

        const int leftMargin = 10;
        const int rightMargin = 10;
        const int top = 10;
        const int textGap = 8;
        const int sectionGap = 16;
        const int segmentGap = 0;
        const int segmentHeight = 30;
        const int segmentWidth = 36;

        _dashboardLabel.Location = new Point(leftMargin, 17);

        var panelWidth = segmentWidth * 2 + 2 + segmentGap;

        _themeSegmentPanel.Location = new Point(_quickAccessPanel.Width - rightMargin - panelWidth, top);
        _themeSegmentPanel.Size = new Size(panelWidth, 32);

        _themeLabel.Location = new Point(_themeSegmentPanel.Left - textGap - _themeLabel.PreferredWidth, 17);

        _linkSegmentPanel.Location = new Point(_themeLabel.Left - sectionGap - panelWidth, top);
        _linkSegmentPanel.Size = new Size(panelWidth, 32);
        _chartsLabel.Location = new Point(_linkSegmentPanel.Left - textGap - _chartsLabel.PreferredWidth, 17);

        _lightThemeButton.Text = string.Empty;
        _darkThemeButton.Text = string.Empty;
        _lightThemeButton.Padding = Padding.Empty;
        _darkThemeButton.Padding = Padding.Empty;

        _lightThemeButton.Size = new Size(segmentWidth, segmentHeight);
        _lightThemeButton.Location = new Point(1, 1);
        _darkThemeButton.Size = new Size(segmentWidth, segmentHeight);
        _darkThemeButton.Location = new Point(1 + segmentWidth + segmentGap, 1);

        _linkedChartsButton.Size = new Size(segmentWidth, segmentHeight);
        _linkedChartsButton.Location = new Point(1, 1);
        _unlinkedChartsButton.Size = new Size(segmentWidth, segmentHeight);
        _unlinkedChartsButton.Location = new Point(1 + segmentWidth + segmentGap, 1);

        UpdateThemeSwitchAppearance();
        UpdateLinkSwitchAppearance();
    }

    private static int MeasureDashboardSegmentPreferredWidth(string text, Font font) {
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        var textWidth = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
        return Math.Max(80, textWidth + 24);
    }

    private static List<int> ScaleSegmentWidths(IReadOnlyList<int> preferredWidths, int targetWidth) {
        if (preferredWidths.Count == 0 || targetWidth <= 0) {
            return [];
        }

        var sumPreferred = preferredWidths.Sum();
        if (sumPreferred <= targetWidth) {
            return preferredWidths.ToList();
        }

        var scaled = preferredWidths
            .Select(w => Math.Max(1, (int)Math.Floor(w * (double)targetWidth / sumPreferred)))
            .ToList();

        var remainder = targetWidth - scaled.Sum();
        for (int i = 0; i < scaled.Count && remainder > 0; i++) {
            scaled[i]++;
            remainder--;
        }

        return scaled;
    }

    private static string TruncateWithEllipsis(string text, Font font, int maxWidth) {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) {
            return string.Empty;
        }

        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        if (TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags).Width <= maxWidth) {
            return text;
        }

        const string ellipsis = "...";
        var ellipsisWidth = TextRenderer.MeasureText(ellipsis, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
        if (ellipsisWidth >= maxWidth) {
            return ellipsis;
        }

        int low = 0;
        int high = text.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            var candidate = text[..mid] + ellipsis;
            var candidateWidth = TextRenderer.MeasureText(candidate, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
            if (candidateWidth <= maxWidth) {
                low = mid;
            } else {
                high = mid - 1;
            }
        }

        return text[..low] + ellipsis;
    }

    private void PositionLinkControls() {
        if (_linkChartsCheckBox == null) {
            return;
        }

        _linkChartsCheckBox.Location = new Point(Math.Max(_quickAccessPanel.Width - _linkChartsCheckBox.Width - 15, 10), 13);
    }

    private bool UpdateQuickDashboards() {
        _quickDashboardsPanel.Controls.Clear();

        var quickDashboards = _dashboards.Where(d => d.IsQuickAccess).Take(5).ToList();
        if (quickDashboards.Count == 0) {
            _quickDashboardsPanel.Size = new Size(0, 32);
            UpdateQuickAccessPanelTheme();
            return false;
        }

        var segmentPanelLeft = GetQuickDashboardsLeft();
        var preferredWidths = quickDashboards
            .Select(d => MeasureDashboardSegmentPreferredWidth(d.Name, _quickDashboardsPanel.Font))
            .ToList();

        var maxPanelWidth = GetQuickDashboardPanelMaxWidth();
        var desiredPanelInnerWidth = preferredWidths.Sum();
        var desiredPanelWidth = desiredPanelInnerWidth + 2;
        var panelWidth = Math.Min(maxPanelWidth, desiredPanelWidth);

        _quickDashboardsPanel.Location = new Point(segmentPanelLeft, 10);
        _quickDashboardsPanel.Size = new Size(Math.Max(panelWidth, 0), 32);

        var innerWidth = Math.Max(_quickDashboardsPanel.Width - 2, 0);
        if (innerWidth == 0) {
            UpdateQuickAccessPanelTheme();
            return quickDashboards.Count > 0;
        }

        var segmentWidths = ScaleSegmentWidths(preferredWidths, innerWidth);
        var x = 1;
        var anyTextTruncated = false;

        for (int i = 0; i < quickDashboards.Count; i++) {
            var dashboard = quickDashboards[i];
            var segmentWidth = segmentWidths[i];
            var renderedText = TruncateWithEllipsis(dashboard.Name, _quickDashboardsPanel.Font, Math.Max(segmentWidth - 12, 1));
            if (!string.Equals(renderedText, dashboard.Name, StringComparison.Ordinal)) {
                anyTextTruncated = true;
            }

            var btn = new RadioButton {
                Text = renderedText,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = {
                    BorderSize = 0,
                    CheckedBackColor = Color.Transparent,
                    MouseDownBackColor = Color.Transparent,
                    MouseOverBackColor = Color.Transparent
                },
                Font = _quickDashboardsPanel.Font,
                AutoSize = false,
                Size = new Size(segmentWidth, 30),
                Location = new Point(x, 1),
                TextAlign = ContentAlignment.MiddleCenter,
                Checked = dashboard.Id == _currentDashboard?.Id,
                Tag = dashboard.Id,
                Margin = Padding.Empty
            };
            btn.CheckedChanged += (s, e) => {
                if (btn.Checked && _currentDashboard?.Id != dashboard.Id) {
                    LoadDashboard(dashboard);
                }
            };

            _quickDashboardsPanel.Controls.Add(btn);
            x += segmentWidth;
        }

        UpdateQuickAccessPanelTheme();
        return anyTextTruncated;
    }

    private void UpdateQuickDashboardSelection() {
        if (_quickDashboardsPanel == null) {
            return;
        }

        var currentDashboardId = _currentDashboard?.Id;
        foreach (var button in _quickDashboardsPanel.Controls.OfType<RadioButton>()) {
            if (button.Tag is Guid dashboardId) {
                button.Checked = dashboardId == currentDashboardId;
            }
        }

        UpdateDashboardSwitchAppearance();
    }

    private void UpdateStatusBar() {
        if (_dataService.IsConnected) {
            _dataPathLabel.Text = $"Storage: {_appSettings.StoragePath}";
            _statusLabel.Text = "Connected to storage";
        } else {
            _dataPathLabel.Text = "Storage: not configured";
            _statusLabel.Text = "Storage not configured (File → Settings)";
        }
    }

    #endregion Private Methods
}
