using MaterialSkin;
using MaterialSkin.Controls;
using SafetyMonitorView.Controls;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ColorScheme = MaterialSkin.ColorScheme;

namespace SafetyMonitorView.Forms;

public class MainForm : MaterialForm {

    #region Public Events

    public event EventHandler? StartupReady;

    #endregion Public Events

    #region Private Fields

    private readonly AppSettings _appSettings = null!;
    private readonly AppSettingsService _appSettingsService;
    private readonly AppSettingsMaintenanceService _appSettingsMaintenanceService;
    private readonly DashboardService _dashboardService;
    private readonly ValueSchemeService _valueSchemeService = new();
    private readonly bool _isLoading = true;
    private readonly MaterialSkinManager _skinManager;
    private Label _chartsLabel = null!;
    private Dashboard? _currentDashboard;
    private Label _dashboardLabel = null!;
    private RadioButton _darkThemeButton = null!;
    private Panel _dashboardContainer = null!;
    private DashboardPanel? _dashboardPanel;
    private readonly Dictionary<Guid, DashboardPanel> _dashboardPanelCache = [];
    private List<Dashboard> _dashboards = [];
    private ToolStripStatusLabel _dataPathLabel = null!;
    private DataService _dataService;
    private RadioButton _linkedChartsButton = null!;
    private CheckBox _linkChartsCheckBox = null!;
    private RadioButton _unlinkedChartsButton = null!;
    private RadioButton _lightThemeButton = null!;
    private MenuStrip _mainMenu = null!;
    private ThemedMenuRenderer _menuRenderer = null!;
    private const int MenuIconSize = 22;
    private const int MaxQuickAccessDashboards = 7;
    private Panel _quickAccessPanel = null!;
    private Panel _quickDashboardsPanel = null!;
    private Panel _linkSegmentPanel = null!;
    private PictureBox _exportProgressIcon = null!;
    private Label _exportProgressLabel = null!;
    private Label _themeLabel = null!;
    private Panel _themeSegmentPanel = null!;
    private System.Windows.Forms.Timer? _refreshTimer;
    private CancellationTokenSource? _refreshCts;
    private bool _isRefreshing;
    private ToolStripStatusLabel _statusLabel = null!;
    private StatusStrip _statusStrip = null!;

    // ── Refresh indicator fields ──
    private PictureBox _refreshIndicatorIcon = null!;
    private Label _refreshIndicatorTimeLabel = null!;
    private System.Windows.Forms.Timer? _refreshCountdownTimer;
    private DateTime _lastRefreshTime = DateTime.Now;
    private DateTime _refreshHourglassVisibleUntil = DateTime.MinValue;
    private const int RefreshHourglassHoldMs = 300;
    private static readonly string[] RefreshLoaderIconSequence = [
        MaterialIcons.RefreshCircle, MaterialIcons.RefreshLoader10, MaterialIcons.RefreshLoader20,
        MaterialIcons.RefreshLoader40, MaterialIcons.RefreshLoader60, MaterialIcons.RefreshLoader80,
        MaterialIcons.RefreshLoader90];

    private System.Windows.Forms.Timer? _themeTimer;
    private Icon? _themeApplicationIcon;
    private bool _isExitConfirmed;
    private bool _restoreToMaximizedAfterMinimize;
    private bool _shouldStartMaximized;
    private bool _startupReadyRaised;
    private FormWindowState _windowStateBeforeMinimize = FormWindowState.Normal;

    // ── Tray icon fields ──
    private NotifyIcon? _trayIcon;
    private System.Windows.Forms.Timer? _trayRefreshTimer;
    private bool _isMinimizedToTray;
    private bool _isTrayRefreshing;

    // ── Visor (забрало) fields ──
    // The visor is a borderless overlay Form with real Opacity support.
    // It covers the dashboard area to hide layout/scaling artifacts during
    // initial startup, dashboard switching and theme changes.
    private Form? _visorForm;
    private System.Windows.Forms.Timer? _visorFadeTimer;

    // True once the initial dashboard reveal has completed.
    // While false, LoadDashboard does NOT call QueueDashboardInitialRender —
    // the dashboard panel is created invisible and revealed only by
    // BeginInitialDashboardReveal called from OnShown.
    private bool _initialRevealCompleted;

    // ── Visor timing constants (configurable) ──
    // Delay in OnShown before visor appears and dashboard starts building.
    // Gives the window time to fully stabilize (maximize, DPI, theme).
    private const int VisorStartupStabilizeMs = 50;
    // Delay after dashboard is fully built before starting the fade-out.
    private const int VisorPreRevealDelayMs = 50;
    // Total duration of the visor fade-out animation.
    private const int VisorFadeDurationMs = 250;
    // Timer interval between individual fade-out opacity steps.
    private const int VisorFadeStepMs = 10;

    #endregion Private Fields

    #region Public Constructors

    public MainForm() {
        _skinManager = MaterialSkinManager.Instance;
        _skinManager.AddFormToManage(this);

        _dashboardService = new DashboardService();
        _appSettingsService = new AppSettingsService();
        _appSettingsMaintenanceService = new AppSettingsMaintenanceService(_appSettingsService, _dashboardService);
        _appSettingsMaintenanceService.EnsureSettingsExists();

        // Load settings BEFORE initializing UI
        _appSettings = _appSettingsService.LoadSettings();
        if (_appSettings.ChartPeriodPresets == null || _appSettings.ChartPeriodPresets.Count == 0) {
            _appSettings.ChartPeriodPresets = ChartPeriodPresetStore.CreateDefaultPresets();
        }
        ChartPeriodPresetStore.SetPresets(_appSettings.ChartPeriodPresets);
        MetricAxisRuleStore.SetRules(_appSettings.MetricAxisRules);
        MetricDisplaySettingsStore.SetSettings(_appSettings.MetricDisplaySettings);

        // Apply theme from settings
        _skinManager.Theme = _appSettings.IsDarkTheme
            ? MaterialSkinManager.Themes.DARK
            : MaterialSkinManager.Themes.LIGHT;

        ApplyMaterialColorScheme(_skinManager.Theme);

        _dataService = new DataService(_appSettings.StoragePath, _appSettings.ValueTileLookbackMinutes);
        AttachDataServiceHandlers();

        // Set default font for the form
        Font = new Font("Segoe UI", 9f, FontStyle.Regular);

        InitializeComponent();
        ApplyApplicationIcon();
        ApplyWindowSettings();
        // LoadDashboards creates the panel invisible; it will NOT be shown
        // until BeginInitialDashboardReveal runs (triggered from OnShown).
        LoadDashboards();
        UpdateStatusBar();
        SetupRefreshTimer();
        SetupTrayIcon();

        // Anti flicker hack
        this.Opacity = 0.999;
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

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

        // Clean up visor
        HideVisorImmediate();

        // Cancel any in-flight async refresh
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;

        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _refreshCountdownTimer?.Stop();
        _refreshCountdownTimer?.Dispose();
        _themeTimer?.Stop();
        _themeTimer?.Dispose();

        _trayRefreshTimer?.Stop();
        _trayRefreshTimer?.Dispose();
        if (_trayIcon != null) {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        ClearDashboardPanelCache();

        _themeApplicationIcon?.Dispose();
        _themeApplicationIcon = null;

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

                // Pause dashboard refresh when minimized
                PauseDashboardRefresh();

                // Minimize to tray if enabled
                if (_appSettings.MinimizeToTray) {
                    MinimizeToTray();
                    _windowStateBeforeMinimize = WindowState;
                    return;
                }
            } else {
                // Restore from taskbar minimize (NOT from tray — tray restore
                // is handled entirely by RestoreFromTray which pre-sets
                // _windowStateBeforeMinimize so this branch is skipped).
                if (_windowStateBeforeMinimize == FormWindowState.Minimized) {
                    ResumeDashboardRefresh();

                    if (_restoreToMaximizedAfterMinimize
                        && WindowState == FormWindowState.Normal) {
                        BeginInvoke(() => {
                            if (WindowState == FormWindowState.Normal) {
                                WindowState = FormWindowState.Maximized;
                            }
                        });
                    }
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

        if (_appSettings.StartMinimized) {
            // Pretend the window was in its intended state so that OnResize
            // correctly computes _restoreToMaximizedAfterMinimize when the
            // Minimized transition fires.
            _windowStateBeforeMinimize = _shouldStartMaximized
                ? FormWindowState.Maximized
                : FormWindowState.Normal;

            BeginInvoke(() => {
                WindowState = FormWindowState.Minimized;
            });
            return;
        }

        if (_shouldStartMaximized && WindowState != FormWindowState.Maximized) {
            WindowState = FormWindowState.Maximized;
        }

        // DO NOT call ScheduleThemeReapply() or EnsureInitialDashboardVisible here.
        // Everything is deferred to BeginInitialDashboardReveal which first raises
        // the visor and only then makes the dashboard visible behind it.

        // A short delay lets the window fully stabilize (maximize, DPI, theme init).
        // During this time the dashboard container is empty — the user sees a clean window.
        var stabilizeTimer = new System.Windows.Forms.Timer { Interval = VisorStartupStabilizeMs };
        stabilizeTimer.Tick += (s, e2) => {
            stabilizeTimer.Stop();
            stabilizeTimer.Dispose();
            BeginInitialDashboardReveal();
        };
        stabilizeTimer.Start();
    }

    #endregion Protected Methods

    #region Private Methods

    // ════════════════════════════════════════════════════════════════
    //  Visor (забрало) — hides layout artifacts during transitions
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the visor background color matching the current theme.
    /// </summary>
    private Color GetVisorColor() {
        return _skinManager.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(250, 250, 250)
            : Color.FromArgb(25, 36, 40);
    }

    /// <summary>
    /// Shows an opaque visor overlay covering the dashboard area.
    /// The visor is a borderless Form with real Opacity support, positioned
    /// exactly over _dashboardContainer in screen coordinates.
    /// </summary>
    private void ShowVisor() {
        HideVisorImmediate();

        if (!IsHandleCreated || IsDisposed) {
            return;
        }

        var visorColor = GetVisorColor();

        // Calculate screen-space bounds of the dashboard container
        Rectangle screenRect;
        try {
            // ИЗМЕНЕНО: Так как _dashboardContainer изначально Visible = false, 
            // его собственный RectangleToScreen может вернуть кривые координаты.
            // Конвертируем его Bounds (которые уже рассчитаны Dock=Fill) 
            // в экранные координаты через родительскую форму (this).
            screenRect = this.RectangleToScreen(_dashboardContainer.Bounds);
        } catch {
            return;
        }

        _visorForm = new Form {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            ControlBox = false,
            BackColor = visorColor,
            Opacity = 1.0,
            StartPosition = FormStartPosition.Manual,
            Bounds = screenRect,
            TopMost = false
        };

        _visorForm.Show(this);
    }

    /// <summary>
    /// Starts a fade-out animation on the visor, reducing its Opacity
    /// from 1.0 to 0.0 over VisorFadeDurationMs, then disposes it.
    /// </summary>
    private void FadeOutVisor(Action? onComplete = null) {
        if (_visorForm == null || _visorForm.IsDisposed) {
            onComplete?.Invoke();
            return;
        }

        var totalSteps = Math.Max(1, VisorFadeDurationMs / VisorFadeStepMs);
        var currentStep = 0;
        var opacityDecrement = 1.0 / totalSteps;

        _visorFadeTimer?.Stop();
        _visorFadeTimer?.Dispose();
        _visorFadeTimer = new System.Windows.Forms.Timer { Interval = VisorFadeStepMs };
        _visorFadeTimer.Tick += (s, e) => {
            currentStep++;
            if (_visorForm == null || _visorForm.IsDisposed || currentStep >= totalSteps) {
                _visorFadeTimer?.Stop();
                _visorFadeTimer?.Dispose();
                _visorFadeTimer = null;
                HideVisorImmediate();
                onComplete?.Invoke();
                return;
            }

            try {
                var t = (double)currentStep / totalSteps;   // 0 → 1
                _visorForm.Opacity = Math.Max(0, (1.0 - t) * (1.0 - t) * (1.0 - t));
            } catch {
                _visorFadeTimer?.Stop();
                _visorFadeTimer?.Dispose();
                _visorFadeTimer = null;
                HideVisorImmediate();
                onComplete?.Invoke();
            }
        };
        _visorFadeTimer.Start();
    }

    /// <summary>
    /// Immediately hides and disposes the visor without animation.
    /// Safe to call even when no visor is active.
    /// </summary>
    private void HideVisorImmediate() {
        _visorFadeTimer?.Stop();
        _visorFadeTimer?.Dispose();
        _visorFadeTimer = null;

        if (_visorForm != null && !_visorForm.IsDisposed) {
            _visorForm.Close();
            _visorForm.Dispose();
        }
        _visorForm = null;
    }

    /// <summary>
    /// Schedules the visor fade-out after a configurable delay.
    /// Called when the dashboard is fully built and ready.
    /// If no visor is currently displayed, does nothing.
    /// </summary>
    private void ScheduleVisorReveal() {
        if (_visorForm == null || _visorForm.IsDisposed) {
            return;
        }

        var delay = new System.Windows.Forms.Timer { Interval = VisorPreRevealDelayMs };
        delay.Tick += (s, e) => {
            delay.Stop();
            delay.Dispose();
            FadeOutVisor();
        };
        delay.Start();
    }

    /// <summary>
    /// Initial dashboard reveal sequence on first launch.
    /// 1. Show visor (opaque, theme-colored) — BEFORE anything becomes visible
    /// 2. Apply theme fixup, make dashboard visible, refresh data — all behind visor
    /// 3. Schedule visor fade-out
    /// </summary>
    private async void BeginInitialDashboardReveal() {
        // 1. Show visor FIRST — before any dashboard UI becomes visible
        ShowVisor();

        // 2. Yield to the message loop so the visor gets a WM_PAINT cycle.
        await Task.Yield();

        // Re-apply theme behind visor (MaterialSkinManager post-init fixup)
        UpdateDashboardContainerTheme();
        UpdateQuickAccessPanelTheme();
        UpdateMenuTheme();

        // Make dashboard visible and refresh behind visor
        if (_dashboardPanel != null) {
            _dashboardContainer.Visible = true;
            _dashboardPanel.Visible = true;
            _dashboardPanel.BringToFront();
            _dashboardPanel.UpdateTheme();
            await RefreshDashboardDataAsync(_dashboardPanel);
        } else {
            _dashboardContainer.Visible = true;
        }

        // Mark initial reveal as completed — subsequent LoadDashboard calls
        // will use the normal visor-based switching path.
        _initialRevealCompleted = true;
        SignalStartupReady();

        // Schedule visor fade-out
        ScheduleVisorReveal();
    }

    // ════════════════════════════════════════════════════════════════
    //  Quick access helpers
    // ════════════════════════════════════════════════════════════════

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
            Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize),
            ImageScaling = ToolStripItemImageScaling.None
        };
        if (onClick != null) {
            item.Click += onClick;
        }
        return item;
    }

    private static string GetIconNameForMenuItem(string text) {
        return text switch {
            "Settings" => MaterialIcons.MenuFileSettings,
            "Exit" => MaterialIcons.MenuFileExitApp,
            "Theme" => MaterialIcons.MenuViewTheme,
            "Light" => MaterialIcons.ThemeLightMode,
            "Dark" => MaterialIcons.ThemeDarkMode,
            "About" => MaterialIcons.MenuHelpAbout,
            "New Dashboard" => MaterialIcons.DashboardCreateNew,
            "Edit Current..." => MaterialIcons.DashboardEditCurrent,
            "Duplicate Current" => MaterialIcons.DashboardDuplicateCurrent,
            "Delete Current" => MaterialIcons.DashboardDeleteCurrent,
            "Manage Dashboards..." => MaterialIcons.DashboardManage,
            "Axis Rules..." => MaterialIcons.MenuViewAxisRules,
            "Metric Settings..." => MaterialIcons.MenuViewMetricSettings,
            "Chart Periods..." => MaterialIcons.MenuViewChartPeriods,
            "Color Schemes..." => MaterialIcons.MenuViewColorSchemes,
            "Value Schemes..." => MaterialIcons.MenuViewValueSchemes,
            _ => ""
        };
    }

    private static void UpdateMenuItemsIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is ToolStripMenuItem menuItem) {
                var iconName = GetIconNameForMenuItem(menuItem.Text!);
                if (!string.IsNullOrEmpty(iconName)) {
                    menuItem.Image?.Dispose();
                    menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize);
                }

                if (menuItem.HasDropDownItems) {
                    InteractiveCursorStyler.Apply(menuItem.DropDownItems);
                    UpdateMenuItemsIcons(menuItem.DropDownItems, iconColor);
                }
            }
        }
    }

    private void ApplyApplicationIcon() {
        var newIcon = ApplicationIconService.GetThemeIcon(_skinManager.Theme);
        var previousOwnedIcon = _themeApplicationIcon;

        Icon = newIcon;
        _themeApplicationIcon = newIcon;

        previousOwnedIcon?.Dispose();
    }

    private void ApplyWindowSettings() {
        if (_appSettings.WindowWidth > 0 && _appSettings.WindowHeight > 0) {
            Size = new Size(_appSettings.WindowWidth, _appSettings.WindowHeight);
        } else {
            Size = new Size(1400, 900);
        }

        if (_appSettings.WindowX >= 0 && _appSettings.WindowY >= 0) {
            StartPosition = FormStartPosition.Manual;
            Location = new Point(_appSettings.WindowX, _appSettings.WindowY);

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

        _shouldStartMaximized = _appSettings.IsMaximized;
    }

    private void CreateMenuItems() {
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;

        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add(CreateMenuItem("Settings", MaterialIcons.MenuFileSettings, iconColor, (s, e) => ShowSettings()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(CreateMenuItem("Exit", MaterialIcons.MenuFileExitApp, iconColor, (s, e) => Close()));

        var dashboardMenu = new ToolStripMenuItem("Dashboards");
        UpdateDashboardMenu(dashboardMenu);

        var viewMenu = new ToolStripMenuItem("View");
        var themeMenu = CreateMenuItem("Theme", MaterialIcons.MenuViewTheme, iconColor, null);
        themeMenu.DropDownItems.Add(CreateMenuItem("Light", MaterialIcons.ThemeLightMode, iconColor, (s, e) => { _lightThemeButton.Checked = true; }));
        themeMenu.DropDownItems.Add(CreateMenuItem("Dark", MaterialIcons.ThemeDarkMode, iconColor, (s, e) => { _darkThemeButton.Checked = true; }));
        viewMenu.DropDownItems.Add(themeMenu);
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add(CreateMenuItem("Metric Settings...", MaterialIcons.MenuViewMetricSettings, iconColor, (s, e) => ShowMetricSettingsEditor()));
        viewMenu.DropDownItems.Add(CreateMenuItem("Axis Rules...", MaterialIcons.MenuViewAxisRules, iconColor, (s, e) => ShowAxisRulesEditor()));
        viewMenu.DropDownItems.Add(CreateMenuItem("Chart Periods...", MaterialIcons.MenuViewChartPeriods, iconColor, (s, e) => ShowChartPeriodPresetEditor()));
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add(CreateMenuItem("Color Schemes...", MaterialIcons.MenuViewColorSchemes, iconColor, (s, e) => ShowColorSchemeEditor()));
        viewMenu.DropDownItems.Add(CreateMenuItem("Value Schemes...", MaterialIcons.MenuViewValueSchemes, iconColor, (s, e) => ShowValueSchemeEditor()));

        var helpMenu = new ToolStripMenuItem("Help");
        helpMenu.DropDownItems.Add(CreateMenuItem("About", MaterialIcons.MenuHelpAbout, iconColor, (s, e) => ShowAbout()));

        _mainMenu.Items.AddRange([fileMenu, dashboardMenu, viewMenu, helpMenu]);
        InteractiveCursorStyler.Apply(_mainMenu.Items);
    }

    private void CreateNewDashboard() {
        var dashboard = new Dashboard {
            Name = $"New Dashboard {_dashboards.Count + 1}",
            Rows = 4,
            Columns = 4,
            SortOrder = _dashboards.Where(d => !d.IsQuickAccess).Select(d => d.SortOrder).DefaultIfEmpty(-1).Max() + 1
        };
        _dashboardService.SaveDashboard(dashboard);
        _dashboards.Add(dashboard);
        SortDashboardsForDisplay();
        UpdateQuickDashboards();
        LoadDashboard(dashboard);
    }

    private void CreateQuickAccessControls() {
        _dashboardLabel = new Label {
            Text = "Dashboard",
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _chartsLabel = new Label {
            Text = "Charts",
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _themeLabel = new Label {
            Text = "Theme",
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };

        _lightThemeButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = Padding.Empty,
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = !_appSettings.IsDarkTheme,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
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
            FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = Padding.Empty,
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = _appSettings.IsDarkTheme,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _darkThemeButton.CheckedChanged += (s, e) => {
            if (_darkThemeButton.Checked) {
                SetTheme(MaterialSkinManager.Themes.DARK);
                _appSettings.IsDarkTheme = true;
                _appSettingsService.SaveSettings(_appSettings);
                UpdateThemeSwitchAppearance();
            }
        };

        _themeSegmentPanel = new Panel { Location = new Point(10, 10), Size = new Size(74, 32), Padding = new Padding(1) };
        _themeSegmentPanel.Controls.Add(_lightThemeButton);
        _themeSegmentPanel.Controls.Add(_darkThemeButton);

        _linkedChartsButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = _appSettings.LinkChartPeriods,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _linkedChartsButton.CheckedChanged += (s, e) => { if (_linkedChartsButton.Checked) { _linkChartsCheckBox.Checked = true; } };

        _unlinkedChartsButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
            AutoSize = false,
            Size = new Size(36, 30),
            Checked = !_appSettings.LinkChartPeriods,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _unlinkedChartsButton.CheckedChanged += (s, e) => { if (_unlinkedChartsButton.Checked) { _linkChartsCheckBox.Checked = false; } };

        _linkSegmentPanel = new Panel { Location = new Point(10, 10), Size = new Size(74, 32), Padding = new Padding(1) };
        _linkSegmentPanel.Controls.Add(_linkedChartsButton);
        _linkSegmentPanel.Controls.Add(_unlinkedChartsButton);

        _quickDashboardsPanel = new Panel { Location = new Point(240, 10), Size = new Size(0, 32), Padding = new Padding(1), Font = new Font("Segoe UI", 9f, FontStyle.Regular) };

        _linkChartsCheckBox = new CheckBox { Visible = false, AutoSize = true, Checked = _appSettings.LinkChartPeriods, Cursor = Cursors.Hand };
        _linkChartsCheckBox.CheckedChanged += (s, e) => {
            _appSettings.LinkChartPeriods = _linkChartsCheckBox.Checked;
            _appSettingsService.SaveSettings(_appSettings);
            _dashboardPanel?.SetLinkChartPeriods(_linkChartsCheckBox.Checked);
            UpdateLinkSwitchAppearance();
        };

        _exportProgressIcon = new PictureBox {
            Size = new Size(22, 22),
            SizeMode = PictureBoxSizeMode.Zoom,
            Visible = false
        };

        _exportProgressLabel = new Label {
            Text = "0%",
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Visible = false
        };

        ExcelExportStateService.StateChanged += () => {
            if (InvokeRequired) {
                BeginInvoke(OnExportStateChanged);
            } else {
                OnExportStateChanged();
            }
        };

        _refreshIndicatorIcon = new PictureBox {
            Size = new Size(22, 22),
            SizeMode = PictureBoxSizeMode.Zoom,
            Visible = _appSettings.ShowRefreshIndicator
        };

        _refreshIndicatorTimeLabel = new Label {
            Text = DateTime.Now.ToString("HH:mm:ss"),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Visible = _appSettings.ShowRefreshIndicator
        };

        _quickAccessPanel.Controls.AddRange([_dashboardLabel, _quickDashboardsPanel, _refreshIndicatorIcon, _refreshIndicatorTimeLabel, _exportProgressIcon, _exportProgressLabel, _chartsLabel, _linkSegmentPanel, _themeLabel, _themeSegmentPanel, _linkChartsCheckBox]);
        RefreshQuickAccessLayout();
        _quickAccessPanel.SizeChanged += (s, e) => RefreshQuickAccessLayout();
    }

    private void DeleteCurrentDashboard() {
        if (_currentDashboard == null || _dashboards.Count <= 1) { ThemedMessageBox.Show(this, "Cannot delete the last dashboard", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var result = ThemedMessageBox.Show(this, $"Delete dashboard '{_currentDashboard.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes) { InvalidateDashboardPanelCache(_currentDashboard.Id); _dashboardService.DeleteDashboard(_currentDashboard); _dashboards.Remove(_currentDashboard); LoadDashboard(_dashboards[0]); UpdateQuickDashboards(); }
    }

    private void DuplicateCurrentDashboard() {
        if (_currentDashboard == null) { return; }
        var copy = _dashboardService.DuplicateDashboard(_currentDashboard);
        if (copy.IsQuickAccess && _dashboards.Count(d => d.IsQuickAccess) >= MaxQuickAccessDashboards) { copy.IsQuickAccess = false; }
        copy.SortOrder = _dashboards.Where(d => d.IsQuickAccess == copy.IsQuickAccess).Select(d => d.SortOrder).DefaultIfEmpty(-1).Max() + 1;
        _dashboardService.SaveDashboard(copy);
        _dashboards.Add(copy);
        SortDashboardsForDisplay();
        UpdateQuickDashboards();
        LoadDashboard(copy);
    }

    private void EditCurrentDashboard() {
        if (_currentDashboard == null) { return; }
        using var editor = new DashboardEditorForm(_currentDashboard);
        if (editor.ShowDialog() == DialogResult.OK && editor.Modified) {
            if (_currentDashboard.IsQuickAccess && _dashboards.Count(d => d.IsQuickAccess && d.Id != _currentDashboard.Id) >= MaxQuickAccessDashboards) {
                _currentDashboard.IsQuickAccess = false;
                ThemedMessageBox.Show(this, $"Only {MaxQuickAccessDashboards} dashboards can be marked as favorite.", "Favorite dashboards", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            _dashboardService.SaveDashboard(_currentDashboard);
            _dashboards = _dashboardService.LoadDashboards();
            SortDashboardsForDisplay();
            PersistDashboardOrdering();
            var updatedDashboard = _dashboards.FirstOrDefault(d => d.Id == _currentDashboard.Id);
            if (updatedDashboard != null) { InvalidateDashboardPanelCache(updatedDashboard.Id); LoadDashboard(updatedDashboard); }
            if (_mainMenu.Items.Count > 1) { var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1]; UpdateDashboardMenu(dashboardMenu); }
            UpdateQuickDashboards();
        }
    }

    private void InitializeComponent() {
        SuspendLayout();
        Text = "⛨  DreamSky Observatory | Safety Monitor";

        _mainMenu = new MenuStrip { Dock = DockStyle.Top, Cursor = Cursors.Hand };
        _menuRenderer = new ThemedMenuRenderer();
        _menuRenderer.UpdateTheme();
        _mainMenu.Renderer = _menuRenderer;
        ToolStripManager.Renderer = _menuRenderer;
        ToolStripManager.VisualStylesEnabled = false;
        _mainMenu.BackColor = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT ? Color.FromArgb(250, 250, 250) : Color.FromArgb(35, 47, 52);
        CreateMenuItems();

        _quickAccessPanel = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10, 5, 10, 5) };
        CreateQuickAccessControls();
        UpdateQuickAccessPanelTheme();

        _dashboardContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        UpdateDashboardContainerTheme();

        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _dataPathLabel = new ToolStripStatusLabel("Storage: not configured") { BorderSides = ToolStripStatusLabelBorderSides.Left };
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

    /// <summary>
    /// Loads a dashboard. During initial load (_initialRevealCompleted == false),
    /// the panel is created invisible and NOT shown — BeginInitialDashboardReveal
    /// handles the first reveal behind a visor.
    /// After the initial reveal, every LoadDashboard call immediately shows a visor,
    /// builds the new dashboard behind it, then fades the visor out.
    /// </summary>
    private void LoadDashboard(Dashboard dashboard) {
        // Show visor BEFORE building the new panel — but only when the initial
        // reveal is already completed (not during the very first load from constructor).
        var isDashboardSwitch = _initialRevealCompleted;
        if (isDashboardSwitch) {
            ShowVisor();
        }

        var previousDashboard = _currentDashboard;
        _currentDashboard = dashboard;

        _appSettings.LastDashboardId = dashboard.Id;
        _appSettingsService.SaveSettings(_appSettings);

        var previousPanel = _dashboardPanel;

        // Try to reuse a previously created panel from cache
        if (_dashboardPanelCache.TryGetValue(dashboard.Id, out var cachedPanel) && !cachedPanel.IsDisposed) {
            _dashboardPanel = cachedPanel;
            _dashboardPanel.SetLinkChartPeriods(_appSettings.LinkChartPeriods);
            _dashboardContainer.SuspendLayout();
            if (!_dashboardContainer.Controls.Contains(_dashboardPanel)) {
                _dashboardPanel.Visible = false;
                _dashboardContainer.Controls.Add(_dashboardPanel);
            }
            _dashboardContainer.ResumeLayout(true);
        } else {
            _dashboardContainer.SuspendLayout();
            _dashboardPanel = new DashboardPanel(
                dashboard, _dataService,
                _appSettings.ChartStaticModeTimeoutSeconds,
                _appSettings.ChartStaticAggregationPresetMatchTolerancePercent,
                _appSettings.ChartStaticAggregationTargetPointCount,
                _appSettings.ChartAggregationRoundingSeconds) {
                Dock = DockStyle.Fill,
                Visible = false
            };
            _dashboardPanel.DashboardChanged += OnDashboardChanged;
            _dashboardPanel.TileEditRequested += OnTileEditRequested;
            _dashboardPanel.SetLinkChartPeriods(_appSettings.LinkChartPeriods);
            _dashboardContainer.Controls.Add(_dashboardPanel);
            _dashboardPanelCache[dashboard.Id] = _dashboardPanel;
            _dashboardContainer.ResumeLayout(true);
        }
        _statusLabel.Text = $"Dashboard: {dashboard.Name}";

        if (_mainMenu.Items.Count > 1) { var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1]; UpdateDashboardMenu(dashboardMenu); }

        var shouldRebuildQuickDashboards = ShouldRebuildQuickDashboards(previousDashboard, dashboard);
        if (shouldRebuildQuickDashboards) { UpdateQuickDashboards(); } else { UpdateQuickDashboardSelection(); }

        if (isDashboardSwitch) {
            // Normal path: visor is up, build and show dashboard behind it
            QueueDashboardInitialRender(_dashboardPanel, previousPanel);
        } else {
            // Initial load path: just remove old panel, keep new one invisible.
            // BeginInitialDashboardReveal will make it visible behind the visor.
            RemoveOldDashboardPanel(previousPanel, _dashboardPanel);
        }

        RestartRefreshTimerInterval();
    }

    private static bool ShouldRebuildQuickDashboards(Dashboard? previousDashboard, Dashboard currentDashboard) {
        var previousWasQuickAccess = previousDashboard != null && previousDashboard.IsQuickAccess;
        var currentIsQuickAccess = currentDashboard.IsQuickAccess;
        return !(previousWasQuickAccess && currentIsQuickAccess);
    }

    private void LoadDashboards() {
        _dashboards = _dashboardService.LoadDashboards();
        SortDashboardsForDisplay();
        PersistDashboardOrdering();

        if (_dashboards.Count > 0) {
            Dashboard? dashboardToLoad = null;
            if (_appSettings.LastDashboardId.HasValue) { dashboardToLoad = _dashboards.FirstOrDefault(d => d.Id == _appSettings.LastDashboardId.Value); }
            dashboardToLoad ??= _dashboards[0];
            LoadDashboard(dashboardToLoad);
        }

        if (_mainMenu.Items.Count > 1) { var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1]; UpdateDashboardMenu(dashboardMenu); }
        UpdateQuickDashboards();
    }

    private void OnFirstHandleCreated(object? sender, EventArgs e) {
        HandleCreated -= OnFirstHandleCreated;
        if (_dashboardPanel != null) {
            QueueDashboardInitialRender(_dashboardPanel, null);
        }
    }

    private void QueueDashboardInitialRender(DashboardPanel panel, DashboardPanel? previousPanel) {
        if (!IsHandleCreated) {
            HandleCreated -= OnFirstHandleCreated;
            HandleCreated += OnFirstHandleCreated;
            return;
        }

        if (!panel.IsHandleCreated) {
            panel.CreateControl();
            if (!panel.IsHandleCreated) {
                panel.Visible = true;
                BeginInvoke(async () => {
                    if (!ReferenceEquals(_dashboardPanel, panel) || panel.IsDisposed) { HideVisorImmediate(); return; }
                    await RefreshDashboardDataAsync(panel);
                    if (!ReferenceEquals(_dashboardPanel, panel) || panel.IsDisposed) { HideVisorImmediate(); return; }
                    _dashboardContainer.Visible = true;
                    RemoveOldDashboardPanel(previousPanel, panel);
                    ScheduleVisorReveal();
                });
                return;
            }
        }

        BeginInvoke(async () => await RefreshAndShowDashboardAsync(panel, previousPanel));
    }

    private async Task RefreshAndShowDashboardAsync(DashboardPanel panel, DashboardPanel? previousPanel) {
        if (!ReferenceEquals(_dashboardPanel, panel) || panel.IsDisposed) { HideVisorImmediate(); return; }

        await RefreshDashboardDataAsync(panel);
        if (!ReferenceEquals(_dashboardPanel, panel) || panel.IsDisposed) { HideVisorImmediate(); return; }
        panel.Visible = true;
        panel.BringToFront();
        _dashboardContainer.Visible = true;
        RemoveOldDashboardPanel(previousPanel, panel);

        // Dashboard is fully built. Schedule visor fade-out.
        ScheduleVisorReveal();
    }

    private void RemoveOldDashboardPanel(DashboardPanel? previousPanel, DashboardPanel newPanel) {
        if (previousPanel == null || ReferenceEquals(previousPanel, newPanel) || previousPanel.IsDisposed) { return; }
        previousPanel.Visible = false;
        if (_dashboardContainer.Controls.Contains(previousPanel)) { _dashboardContainer.Controls.Remove(previousPanel); }
    }

    private void InvalidateDashboardPanelCache(Guid dashboardId) {
        if (_dashboardPanelCache.Remove(dashboardId, out var panel) && !panel.IsDisposed) {
            if (_dashboardContainer.Controls.Contains(panel)) { _dashboardContainer.Controls.Remove(panel); }
            panel.Dispose();
        }
    }

    private void ClearDashboardPanelCache() {
        foreach (var (_, panel) in _dashboardPanelCache) {
            if (panel.IsDisposed) { continue; }
            if (ReferenceEquals(panel, _dashboardPanel)) { continue; }
            if (_dashboardContainer.Controls.Contains(panel)) { _dashboardContainer.Controls.Remove(panel); }
            panel.Dispose();
        }
        _dashboardPanelCache.Clear();
    }

    private void OnDashboardChanged() {
        if (_currentDashboard == null) { return; }
        _dashboardService.SaveDashboard(_currentDashboard);
    }

    private void OnTileEditRequested(TileConfig tileConfig) {
        if (_currentDashboard == null) { return; }

        DialogResult result;
        if (tileConfig is ValueTileConfig vtc) {
            using var editor = new ValueTileEditorForm(vtc);
            result = editor.ShowDialog();
        } else if (tileConfig is ChartTileConfig ctc) {
            using var editor = new ChartTileEditorForm(ctc, _currentDashboard);
            result = editor.ShowDialog();
        } else {
            return;
        }

        if (result == DialogResult.OK) {
            _dashboardService.SaveDashboard(_currentDashboard);
            _dashboards = _dashboardService.LoadDashboards();
            var updatedDashboard = _dashboards.FirstOrDefault(d => d.Id == _currentDashboard.Id);
            if (updatedDashboard != null) { InvalidateDashboardPanelCache(updatedDashboard.Id); LoadDashboard(updatedDashboard); }
        }
    }

    private void SaveWindowSettings() {
        _appSettings.IsMaximized = WindowState == FormWindowState.Maximized;
        var normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        if (normalBounds.Width > 0 && normalBounds.Height > 0) {
            _appSettings.WindowWidth = normalBounds.Width;
            _appSettings.WindowHeight = normalBounds.Height;
            _appSettings.WindowX = normalBounds.Left;
            _appSettings.WindowY = normalBounds.Top;
        }
        _appSettingsService.SaveSettings(_appSettings);
    }

    /// <summary>
    /// Schedules a deferred theme reapply. When a visor is active (theme switch),
    /// the callback calls ScheduleVisorReveal after all colors are set.
    /// </summary>
    private void ScheduleThemeReapply() {
        if (_themeTimer != null) { _themeTimer.Stop(); _themeTimer.Dispose(); }
        _themeTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _themeTimer.Tick += async (s, e) => {
            _themeTimer!.Stop();
            _themeTimer.Dispose();
            _themeTimer = null;
            UpdateDashboardContainerTheme();
            UpdateQuickAccessPanelTheme();
            UpdateMenuTheme();
            _dashboardPanel?.UpdateTheme();
            await RefreshDashboardDataAsync();

            // If a visor is active (theme switch), reveal it now that colors are applied.
            ScheduleVisorReveal();
        };
        _themeTimer.Start();
    }

    private async Task RefreshDashboardDataAsync(DashboardPanel? targetPanel = null) {
        var panel = targetPanel ?? _dashboardPanel;
        if (panel == null) {
            return;
        }

        // Cancel any previous in-flight refresh.
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = new CancellationTokenSource();
        var ct = _refreshCts.Token;

        _isRefreshing = true;
        _refreshHourglassVisibleUntil = DateTime.Now.AddMilliseconds(RefreshHourglassHoldMs);
        UpdateRefreshCountdownIcon();

        try {
            await panel.RefreshDataAsync(ct);
        } catch (OperationCanceledException) {
            return;
        } finally {
            _isRefreshing = false;
            UpdateRefreshCountdownIcon();
        }

        UpdateRefreshIndicatorTimestamp();
        RestartRefreshTimerInterval();
    }

    private void RestartRefreshTimerInterval() {
        if (_refreshTimer == null) { return; }
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private void ApplyMaterialColorScheme(MaterialSkinManager.Themes theme) {
        _skinManager.ColorScheme = theme == MaterialSkinManager.Themes.LIGHT
            //? new ColorScheme(
            //    Primary.BlueGrey700,
            //    Primary.BlueGrey900,
            //    Primary.BlueGrey500,
            //    Accent.LightBlue200,
            //    TextShade.WHITE)
            //: new ColorScheme(
            //    Primary.BlueGrey800,
            //    Primary.BlueGrey900,
            //    Primary.BlueGrey500,
            //    Accent.LightBlue200,
            //    TextShade.WHITE);
            ? new ColorScheme(
                Primary.Teal700,
                Primary.Teal900,
                Primary.Teal500,
                Accent.Teal200,
                TextShade.WHITE)
            : new ColorScheme(
                Primary.Teal700,
                Primary.Teal900,
                Primary.Teal500,
                Accent.Teal200,
                TextShade.WHITE);
    }

    /// <summary>
    /// Switches theme with visor protection: visor appears immediately before
    /// any theme colors start changing, then fades out after reapply completes.
    /// </summary>
    private void SetTheme(MaterialSkinManager.Themes theme) {
        // Show visor BEFORE changing the theme to hide all repaint artifacts.
        // На этом этапе забрало поднимется в старом цвете.
        if (_initialRevealCompleted) {
            ShowVisor();
        }

        _skinManager.Theme = theme;
        ApplyMaterialColorScheme(theme);
        ApplyApplicationIcon();

        // ИЗМЕНЕНО: Сразу после переключения темы обновляем цвет забрала на новый
        // и форсируем отрисовку (Refresh), чтобы перекрытие перекрасилось ДО того, 
        // как контролы снизу начнут тяжелый рендеринг.
        if (_visorForm != null && !_visorForm.IsDisposed) {
            _visorForm.BackColor = GetVisorColor();
            _visorForm.Refresh();
        }

        // MaterialSkinManager queues multiple deferred color updates.
        // ScheduleThemeReapply reapplies our colors AFTER MSM is done,
        // then calls ScheduleVisorReveal to fade out the visor.
        ScheduleThemeReapply();
    }

    private void SetupRefreshTimer() {
        _refreshTimer = new System.Windows.Forms.Timer { Interval = _appSettings.RefreshInterval * 1000 };
        _refreshTimer.Tick += async (s, e) => {
            if (_isRefreshing) { return; }
            if (_dashboardPanel != null) {
                await RefreshDashboardDataAsync(_dashboardPanel);
            }
        };
        _refreshTimer.Start();
        _lastRefreshTime = DateTime.Now;

        _refreshCountdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _refreshCountdownTimer.Tick += (s, e) => UpdateRefreshCountdownIcon();
        _refreshCountdownTimer.Start();
    }

    private void UpdateRefreshIndicatorTimestamp() {
        _lastRefreshTime = DateTime.Now;
        if (_refreshIndicatorTimeLabel != null) {
            _refreshIndicatorTimeLabel.Text = _lastRefreshTime.ToString("HH:mm:ss");
        }
    }

    private void UpdateRefreshCountdownIcon() {
        if (_refreshIndicatorIcon == null || !_refreshIndicatorIcon.Visible) { return; }

        var showHourglass = _isRefreshing || DateTime.Now < _refreshHourglassVisibleUntil;
        var iconName = MaterialIcons.RefreshHourglass;
        if (!showHourglass) {
            var elapsed = (DateTime.Now - _lastRefreshTime).TotalSeconds;
            var interval = _appSettings.RefreshInterval;
            var progress = interval > 0 ? Math.Clamp(elapsed / interval, 0.0, 1.0) : 0.0;
            var index = (int)(progress * (RefreshLoaderIconSequence.Length - 1));
            iconName = RefreshLoaderIconSequence[Math.Clamp(index, 0, RefreshLoaderIconSequence.Length - 1)];
        }

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        _refreshIndicatorIcon.Image?.Dispose();
        _refreshIndicatorIcon.Image = MaterialIcons.GetIcon(iconName, iconColor, 22);
    }

    private void AttachDataServiceHandlers() {
        _dataService.ConnectionFailed += OnDataServiceConnectionFailed;
    }

    private void OnDataServiceConnectionFailed(string details) {
        if (InvokeRequired) { BeginInvoke(() => OnDataServiceConnectionFailed(details)); return; }
        _refreshTimer?.Stop();
        _statusLabel.Text = "SQL connection failed";
        var message = "Unable to connect to SQL Server. Application terminated." + Environment.NewLine + details;
        ThemedMessageBox.Show(this, message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        _isExitConfirmed = true;
        Close();
    }

    private void ShowAbout() {
        var message = "SafetyMonitorView v1.0" + Environment.NewLine + "ASCOM Alpaca" + Environment.NewLine + "Safety Monitor Dashboard" + Environment.NewLine + "©2026 DreamSky Observatory";
        ThemedMessageBox.Show(this, message, "About SafetyMonitorView", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowAxisRulesEditor() {
        using var editor = new AxisRulesEditorForm(_appSettings.MetricAxisRules);
        if (editor.ShowDialog(this) == DialogResult.OK) { _appSettings.MetricAxisRules = editor.Rules; _appSettingsService.SaveSettings(_appSettings); MetricAxisRuleStore.SetRules(_appSettings.MetricAxisRules); }
    }

    private void ShowMetricSettingsEditor() {
        using var editor = new MetricSettingsEditorForm(_appSettings.MetricDisplaySettings);
        if (editor.ShowDialog(this) == DialogResult.OK) { _appSettings.MetricDisplaySettings = editor.Settings; _appSettingsService.SaveSettings(_appSettings); MetricDisplaySettingsStore.SetSettings(_appSettings.MetricDisplaySettings); }
    }

    private void ShowColorSchemeEditor() { using var editor = new ColorSchemeEditorForm(); editor.ShowDialog(this); }

    private void ShowValueSchemeEditor() { using var editor = new ValueSchemeEditorForm(); editor.ShowDialog(this); }

    private void ShowChartPeriodPresetEditor() {
        using var editor = new ChartPeriodsEditorForm(_appSettings.ChartPeriodPresets, _appSettings.ChartStaticAggregationTargetPointCount, _appSettings.ChartAggregationRoundingSeconds);
        if (editor.ShowDialog(this) == DialogResult.OK) {
            _appSettings.ChartPeriodPresets = editor.Presets;
            _appSettingsService.SaveSettings(_appSettings);
            ChartPeriodPresetStore.SetPresets(_appSettings.ChartPeriodPresets);
            ClearDashboardPanelCache();
            if (_currentDashboard != null) { LoadDashboard(_currentDashboard); } else { _ = RefreshDashboardDataAsync(); }
        }
    }

    private void ShowSettings() {
        using var settingsForm = new SettingsForm(_appSettingsMaintenanceService, _appSettings.StoragePath, _appSettings.RefreshInterval, _appSettings.ValueTileLookbackMinutes, _appSettings.ChartStaticModeTimeoutSeconds, _appSettings.ChartStaticAggregationPresetMatchTolerancePercent, _appSettings.ChartStaticAggregationTargetPointCount, _appSettings.ChartAggregationRoundingSeconds, _appSettings.ShowRefreshIndicator, _appSettings.MinimizeToTray, _appSettings.StartMinimized);
        if (settingsForm.ShowDialog() != DialogResult.OK) {
            return;
        }

        if (settingsForm.SettingsMaintenanceAction == SettingsMaintenanceAction.Import || settingsForm.SettingsMaintenanceAction == SettingsMaintenanceAction.Reset) {
            ReloadSettingsFromStorage();
            return;
        }

        if (settingsForm.SettingsMaintenanceAction == SettingsMaintenanceAction.Export) {
            return;
        }

        _appSettings.StoragePath = settingsForm.StoragePath;
        _appSettings.RefreshInterval = settingsForm.RefreshInterval;
        _appSettings.ValueTileLookbackMinutes = settingsForm.ValueTileLookbackMinutes;
        _appSettings.ChartStaticModeTimeoutSeconds = settingsForm.ChartStaticTimeoutSeconds;
        _appSettings.ChartStaticAggregationPresetMatchTolerancePercent = settingsForm.ChartStaticAggregationPresetMatchTolerancePercent;
        _appSettings.ChartStaticAggregationTargetPointCount = settingsForm.ChartStaticAggregationTargetPointCount;
        _appSettings.ChartAggregationRoundingSeconds = settingsForm.ChartAggregationRoundingSeconds;
        _appSettings.ShowRefreshIndicator = settingsForm.ShowRefreshIndicator;
        _appSettings.MinimizeToTray = settingsForm.MinimizeToTray;
        _appSettings.StartMinimized = settingsForm.StartMinimized;
        _appSettingsService.SaveSettings(_appSettings);

        ApplySettingsToRuntime();
    }

    private void ReloadSettingsFromStorage() {
        var loaded = _appSettingsService.LoadSettings();
        CopySettings(loaded, _appSettings);
        ApplySettingsToRuntime();
    }

    private static void CopySettings(AppSettings source, AppSettings target) {
        target.IsDarkTheme = source.IsDarkTheme;
        target.IsMaximized = source.IsMaximized;
        target.LinkChartPeriods = source.LinkChartPeriods;
        target.MinimizeToTray = source.MinimizeToTray;
        target.ShowRefreshIndicator = source.ShowRefreshIndicator;
        target.StartMinimized = source.StartMinimized;
        target.ChartStaticModeTimeoutSeconds = source.ChartStaticModeTimeoutSeconds;
        target.ChartStaticAggregationPresetMatchTolerancePercent = source.ChartStaticAggregationPresetMatchTolerancePercent;
        target.ChartStaticAggregationTargetPointCount = source.ChartStaticAggregationTargetPointCount;
        target.ChartAggregationRoundingSeconds = source.ChartAggregationRoundingSeconds;
        target.LastDashboardId = source.LastDashboardId;
        target.RefreshInterval = source.RefreshInterval;
        target.ValueTileLookbackMinutes = source.ValueTileLookbackMinutes;
        target.ChartPeriodPresets = source.ChartPeriodPresets;
        target.MetricAxisRules = source.MetricAxisRules;
        target.MetricDisplaySettings = source.MetricDisplaySettings;
        target.StoragePath = source.StoragePath;
        target.WindowHeight = source.WindowHeight;
        target.WindowWidth = source.WindowWidth;
        target.WindowX = source.WindowX;
        target.WindowY = source.WindowY;
    }

    private void ApplySettingsToRuntime() {
        ChartPeriodPresetStore.SetPresets(_appSettings.ChartPeriodPresets);
        MetricAxisRuleStore.SetRules(_appSettings.MetricAxisRules);
        MetricDisplaySettingsStore.SetSettings(_appSettings.MetricDisplaySettings);

        _dataService = new DataService(_appSettings.StoragePath, _appSettings.ValueTileLookbackMinutes);
        AttachDataServiceHandlers();
        UpdateStatusBar();

        _refreshTimer?.Interval = _appSettings.RefreshInterval * 1000;
        if (_trayRefreshTimer != null) { _trayRefreshTimer.Interval = _appSettings.RefreshInterval * 1000; }
        _dashboardPanel?.SetChartStaticModeTimeoutSeconds(_appSettings.ChartStaticModeTimeoutSeconds);
        _dashboardPanel?.SetChartStaticAggregationOptions(_appSettings.ChartStaticAggregationPresetMatchTolerancePercent, _appSettings.ChartStaticAggregationTargetPointCount, _appSettings.ChartAggregationRoundingSeconds);
        _refreshIndicatorIcon.Visible = _appSettings.ShowRefreshIndicator;
        _refreshIndicatorTimeLabel.Visible = _appSettings.ShowRefreshIndicator;
        RefreshQuickAccessLayout();

        _dashboards = _dashboardService.LoadDashboards();
        SortDashboardsForDisplay();

        ClearDashboardPanelCache();
        var nextDashboard = _appSettings.LastDashboardId.HasValue
            ? _dashboards.FirstOrDefault(d => d.Id == _appSettings.LastDashboardId.Value)
            : _dashboards.FirstOrDefault();
        nextDashboard ??= _dashboards.FirstOrDefault();

        if (nextDashboard != null) {
            LoadDashboard(nextDashboard);
        } else {
            _ = RefreshDashboardDataAsync();
        }
    }

    private void PersistDashboardOrdering() {
        var changed = false;
        int quickOrder = 0, regularOrder = 0;
        foreach (var dashboard in _dashboards) {
            int targetOrder = dashboard.IsQuickAccess ? quickOrder++ : regularOrder++;
            if (dashboard.SortOrder != targetOrder) { dashboard.SortOrder = targetOrder; _dashboardService.SaveDashboard(dashboard); changed = true; }
        }
        if (changed) { SortDashboardsForDisplay(); }
    }

    private void ShowDashboardManager() {
        using var manager = new DashboardManagementForm(_dashboards, _currentDashboard?.Id);
        if (manager.ShowDialog(this) != DialogResult.OK) { return; }

        foreach (var deletedId in manager.DeletedDashboardIds) {
            var dashboard = _dashboards.FirstOrDefault(d => d.Id == deletedId);
            if (dashboard != null) { InvalidateDashboardPanelCache(deletedId); _dashboardService.DeleteDashboard(dashboard); _dashboards.Remove(dashboard); }
        }

        var updatesById = manager.Updates.ToDictionary(u => u.DashboardId);
        foreach (var dashboard in _dashboards) {
            if (!updatesById.TryGetValue(dashboard.Id, out var update)) { continue; }
            dashboard.IsQuickAccess = update.IsQuickAccess; dashboard.SortOrder = update.SortOrder; dashboard.Name = update.Name;
            _dashboardService.SaveDashboard(dashboard);
        }

        _dashboards = _dashboardService.LoadDashboards();
        SortDashboardsForDisplay();
        PersistDashboardOrdering();

        if (_dashboards.Count == 0) { var fallback = Dashboard.CreateDefault(); fallback.SortOrder = 0; _dashboardService.SaveDashboard(fallback); _dashboards = [fallback]; }
        var nextDashboard = _currentDashboard != null ? _dashboards.FirstOrDefault(d => d.Id == _currentDashboard.Id) : null;
        nextDashboard ??= _dashboards.First();
        LoadDashboard(nextDashboard);

        if (_mainMenu.Items.Count > 1) { var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1]; UpdateDashboardMenu(dashboardMenu); }
        UpdateQuickDashboards();
    }

    private void SortDashboardsForDisplay() {
        _dashboards = [.. _dashboards.OrderByDescending(d => d.IsQuickAccess).ThenBy(d => d.SortOrder).ThenBy(d => d.Name)];
    }

    private void UpdateDashboardContainerTheme() {
        _dashboardContainer.BackColor = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT ? Color.FromArgb(250, 250, 250) : Color.FromArgb(25, 36, 40);
    }

    private void UpdateDashboardMenu(ToolStripMenuItem dashboardMenu) {
        dashboardMenu.DropDownItems.Clear();
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;

        foreach (var dashboard in _dashboards) {
            var isSelected = dashboard.Id == _currentDashboard?.Id;
            var item = new ToolStripMenuItem(dashboard.Name) { Checked = false, Image = isSelected ? MaterialIcons.GetIcon(MaterialIcons.CommonCheck, iconColor, MenuIconSize) : null, ImageScaling = ToolStripItemImageScaling.None };
            item.Click += (s, e) => LoadDashboard(dashboard);
            dashboardMenu.DropDownItems.Add(item);
        }

        dashboardMenu.DropDownItems.Add(new ToolStripSeparator());
        dashboardMenu.DropDownItems.Add(CreateMenuItem("New Dashboard", MaterialIcons.DashboardCreateNew, iconColor, (s, e) => CreateNewDashboard()));
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Edit current...", MaterialIcons.DashboardEditCurrent, iconColor, (s, e) => EditCurrentDashboard()));
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Duplicate Current", MaterialIcons.DashboardDuplicateCurrent, iconColor, (s, e) => DuplicateCurrentDashboard()));
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Manage Dashboards...", MaterialIcons.DashboardManage, iconColor, (s, e) => ShowDashboardManager()));
        dashboardMenu.DropDownItems.Add(new ToolStripSeparator());
        dashboardMenu.DropDownItems.Add(CreateMenuItem("Delete Current", MaterialIcons.DashboardDeleteCurrent, iconColor, (s, e) => DeleteCurrentDashboard()));
        InteractiveCursorStyler.Apply(dashboardMenu.DropDownItems);
    }

    private void UpdateMenuTheme() {
        _menuRenderer.UpdateTheme();
        ToolStripManager.Renderer = _menuRenderer;
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;
        _mainMenu.BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(35, 47, 52);
        UpdateMenuItemsIcons(_mainMenu.Items, iconColor);
        if (_mainMenu.Items.Count > 1) { var dashboardMenu = (ToolStripMenuItem)_mainMenu.Items[1]; UpdateDashboardMenu(dashboardMenu); }
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
        UpdateRefreshIndicatorAppearance();
        UpdateExportProgressAppearance();
        UpdateDashboardSwitchAppearance();
    }

    private void UpdateThemeSwitchAppearance() {
        if (_themeSegmentPanel == null || _lightThemeButton == null || _darkThemeButton == null) { return; }
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
        _lightThemeButton.Image = MaterialIcons.GetIcon(MaterialIcons.ThemeLightMode, iconColor, 22);
        _darkThemeButton.Image = MaterialIcons.GetIcon(MaterialIcons.ThemeDarkMode, iconColor, 22);
        _lightThemeButton.ImageAlign = ContentAlignment.MiddleCenter;
        _darkThemeButton.ImageAlign = ContentAlignment.MiddleCenter;
    }

    private void UpdateLinkSwitchAppearance() {
        if (_linkSegmentPanel == null || _linkedChartsButton == null || _unlinkedChartsButton == null) { return; }
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
        _linkedChartsButton.Image = MaterialIcons.GetIcon(MaterialIcons.ToolbarChartsLink, iconColor, 22);
        _unlinkedChartsButton.Image = MaterialIcons.GetIcon(MaterialIcons.ToolbarChartsUnlink, iconColor, 22);
        _linkedChartsButton.ImageAlign = ContentAlignment.MiddleCenter;
        _unlinkedChartsButton.ImageAlign = ContentAlignment.MiddleCenter;
    }

    private void UpdateRefreshIndicatorAppearance() {
        if (_refreshIndicatorIcon == null || _refreshIndicatorTimeLabel == null) { return; }
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var fg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        _refreshIndicatorTimeLabel.ForeColor = fg;
        UpdateRefreshCountdownIcon();
    }

    private void OnExportStateChanged() {
        var isExporting = ExcelExportStateService.IsExporting;
        _exportProgressIcon.Visible = isExporting;
        _exportProgressLabel.Visible = isExporting;

        if (isExporting) {
            _exportProgressLabel.Text = $"{ExcelExportStateService.ProgressPercent}%";
        }

        UpdateExportProgressAppearance();
        RefreshQuickAccessLayout();
    }

    private void UpdateExportProgressAppearance() {
        if (_exportProgressIcon == null || _exportProgressLabel == null) { return; }
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var fg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var iconColor = isLight ? Color.FromArgb(35, 47, 52) : Color.FromArgb(223, 234, 239);
        _exportProgressLabel.ForeColor = fg;
        _exportProgressIcon.Image?.Dispose();
        _exportProgressIcon.Image = MaterialIcons.GetIcon(MaterialIcons.ToolbarExportProgress, iconColor, 22);
    }

    private void UpdateDashboardSwitchAppearance() {
        if (_quickDashboardsPanel == null) { return; }
        var buttons = _quickDashboardsPanel.Controls.OfType<RadioButton>().ToList();
        if (buttons.Count == 0) { return; }

        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.FromArgb(21, 28, 31) : Color.White;
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        _quickDashboardsPanel.BackColor = borderColor;
        foreach (var button in buttons) {
            var isMenuSelectedBadge = button.Tag is string tag && tag == "menu-selected-badge";
            if (isMenuSelectedBadge) {
                var badgeBg = isLight ? Color.FromArgb(222, 222, 222) : Color.FromArgb(124, 132, 140);
                button.BackColor = badgeBg; button.FlatAppearance.CheckedBackColor = badgeBg; button.FlatAppearance.MouseDownBackColor = badgeBg; button.FlatAppearance.MouseOverBackColor = badgeBg;
                button.ForeColor = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(206, 215, 220);
                continue;
            }
            button.BackColor = button.Checked ? activeBg : segmentBg;
            button.ForeColor = button.Checked ? activeFg : inactiveFg;
            button.Font = new Font(_quickDashboardsPanel.Font, button.Checked ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    private void RefreshQuickAccessLayout() { PositionLinkControls(); UpdateThemeSwitchLayout(); UpdateQuickDashboards(); }

    private int GetQuickDashboardsLeft() { return _dashboardLabel.Right + 8; }

    private int GetQuickDashboardPanelMaxWidth() {
        var left = GetQuickDashboardsLeft();
        const int spacingToRightGroup = 16;
        int rightBound;
        if (_refreshIndicatorIcon.Visible) {
            rightBound = _refreshIndicatorIcon.Left;
        } else if (_exportProgressIcon.Visible) {
            rightBound = _exportProgressIcon.Left;
        } else {
            rightBound = _chartsLabel.Left;
        }
        return Math.Max(rightBound - spacingToRightGroup - left, 0);
    }

    private void UpdateThemeSwitchLayout() {
        if (_themeSegmentPanel == null || _lightThemeButton == null || _darkThemeButton == null) { return; }
        const int leftMargin = 10, rightMargin = 10, top = 10, textGap = 8, sectionGap = 16, segmentGap = 0, segmentHeight = 30, segmentWidth = 36;

        _dashboardLabel.Location = new Point(leftMargin, 17);
        var panelWidth = segmentWidth * 2 + 2 + segmentGap;
        _themeSegmentPanel.Location = new Point(_quickAccessPanel.Width - rightMargin - panelWidth, top);
        _themeSegmentPanel.Size = new Size(panelWidth, 32);
        _themeLabel.Location = new Point(_themeSegmentPanel.Left - textGap - _themeLabel.PreferredWidth, 17);
        _linkSegmentPanel.Location = new Point(_themeLabel.Left - sectionGap - panelWidth, top);
        _linkSegmentPanel.Size = new Size(panelWidth, 32);
        _chartsLabel.Location = new Point(_linkSegmentPanel.Left - textGap - _chartsLabel.PreferredWidth, 17);

        if (_exportProgressLabel.Visible) {
            const int exportIconGap = 4;
            _exportProgressLabel.Location = new Point(_chartsLabel.Left - sectionGap - _exportProgressLabel.PreferredWidth, 17);
            _exportProgressIcon.Location = new Point(_exportProgressLabel.Left - exportIconGap - _exportProgressIcon.Width, 14);
        }

        if (_refreshIndicatorTimeLabel.Visible) {
            var indicatorRightBound = _exportProgressIcon.Visible ? _exportProgressIcon.Left : _chartsLabel.Left;
            const int indicatorSectionGap = 16;
            const int indicatorIconGap = 4;
            _refreshIndicatorTimeLabel.Location = new Point(indicatorRightBound - indicatorSectionGap - _refreshIndicatorTimeLabel.PreferredWidth, 17);
            _refreshIndicatorIcon.Location = new Point(_refreshIndicatorTimeLabel.Left - indicatorIconGap - _refreshIndicatorIcon.Width, 14);
        }

        _lightThemeButton.Text = string.Empty; _darkThemeButton.Text = string.Empty;
        _lightThemeButton.Padding = Padding.Empty; _darkThemeButton.Padding = Padding.Empty;
        _lightThemeButton.Size = new Size(segmentWidth, segmentHeight); _lightThemeButton.Location = new Point(1, 1);
        _darkThemeButton.Size = new Size(segmentWidth, segmentHeight); _darkThemeButton.Location = new Point(1 + segmentWidth + segmentGap, 1);
        _linkedChartsButton.Size = new Size(segmentWidth, segmentHeight); _linkedChartsButton.Location = new Point(1, 1);
        _unlinkedChartsButton.Size = new Size(segmentWidth, segmentHeight); _unlinkedChartsButton.Location = new Point(1 + segmentWidth + segmentGap, 1);
        UpdateThemeSwitchAppearance(); UpdateLinkSwitchAppearance();
    }

    private static int MeasureDashboardSegmentPreferredWidth(string text, Font font) {
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        var textWidth = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
        return Math.Max(80, textWidth + 24);
    }

    private static List<int> ScaleSegmentWidths(List<int> preferredWidths, int targetWidth) {
        if (preferredWidths.Count == 0 || targetWidth <= 0) { return []; }
        var sumPreferred = preferredWidths.Sum();
        if (sumPreferred <= targetWidth) { return [.. preferredWidths]; }
        var scaled = preferredWidths.Select(w => Math.Max(1, (int)Math.Floor(w * (double)targetWidth / sumPreferred))).ToList();
        var remainder = targetWidth - scaled.Sum();
        for (int i = 0; i < scaled.Count && remainder > 0; i++) { scaled[i]++; remainder--; }
        return scaled;
    }

    private static string TruncateWithEllipsis(string text, Font font, int maxWidth) {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) { return string.Empty; }
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        if (TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), flags).Width <= maxWidth) { return text; }
        const string ellipsis = "...";
        var ellipsisWidth = TextRenderer.MeasureText(ellipsis, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
        if (ellipsisWidth >= maxWidth) { return ellipsis; }
        int low = 0, high = text.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            var candidate = text[..mid] + ellipsis;
            var candidateWidth = TextRenderer.MeasureText(candidate, font, new Size(int.MaxValue, int.MaxValue), flags).Width;
            if (candidateWidth <= maxWidth) { low = mid; } else { high = mid - 1; }
        }
        return text[..low] + ellipsis;
    }

    private void PositionLinkControls() {
        if (_linkChartsCheckBox == null) { return; }
        _linkChartsCheckBox.Location = new Point(Math.Max(_quickAccessPanel.Width - _linkChartsCheckBox.Width - 15, 10), 13);
    }

    private bool UpdateQuickDashboards() {
        _quickDashboardsPanel.Controls.Clear();
        var quickDashboards = _dashboards.Where(d => d.IsQuickAccess).Take(MaxQuickAccessDashboards).ToList();
        var showSelectedFromMenuBadge = _currentDashboard != null && quickDashboards.All(d => d.Id != _currentDashboard.Id);
        if (showSelectedFromMenuBadge && _currentDashboard != null) { quickDashboards.Add(_currentDashboard); }
        if (quickDashboards.Count == 0) { _quickDashboardsPanel.Size = new Size(0, 32); UpdateQuickAccessPanelTheme(); return false; }

        var segmentPanelLeft = GetQuickDashboardsLeft();
        var preferredWidths = quickDashboards.Select(d => MeasureDashboardSegmentPreferredWidth(d.Name, _quickDashboardsPanel.Font)).ToList();
        var maxPanelWidth = GetQuickDashboardPanelMaxWidth();
        var desiredPanelWidth = preferredWidths.Sum() + 2;
        var panelWidth = Math.Min(maxPanelWidth, desiredPanelWidth);

        _quickDashboardsPanel.Location = new Point(segmentPanelLeft, 10);
        _quickDashboardsPanel.Size = new Size(Math.Max(panelWidth, 0), 32);

        var innerWidth = Math.Max(_quickDashboardsPanel.Width - 2, 0);
        if (innerWidth == 0) { UpdateQuickAccessPanelTheme(); return quickDashboards.Count > 0; }

        var segmentWidths = ScaleSegmentWidths(preferredWidths, innerWidth);
        var x = 1;
        var anyTextTruncated = false;

        for (int i = 0; i < quickDashboards.Count; i++) {
            var dashboard = quickDashboards[i];
            var segmentWidth = segmentWidths[i];
            var renderedText = TruncateWithEllipsis(dashboard.Name, _quickDashboardsPanel.Font, Math.Max(segmentWidth - 12, 1));
            if (!string.Equals(renderedText, dashboard.Name, StringComparison.Ordinal)) { anyTextTruncated = true; }

            var btn = new RadioButton {
                Text = renderedText,
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
                Font = _quickDashboardsPanel.Font,
                AutoSize = false,
                Size = new Size(segmentWidth, 30),
                Location = new Point(x, 1),
                TextAlign = ContentAlignment.MiddleCenter,
                Checked = dashboard.Id == _currentDashboard?.Id,
                Tag = dashboard.Id,
                Margin = Padding.Empty,
                UseVisualStyleBackColor = false,
                Cursor = Cursors.Hand
            };

            if (showSelectedFromMenuBadge && dashboard.Id == _currentDashboard?.Id) {
                btn.Tag = "menu-selected-badge"; btn.AutoCheck = false; btn.TabStop = false;
            } else {
                btn.CheckedChanged += (s, e) => { if (btn.Checked && _currentDashboard?.Id != dashboard.Id) { LoadDashboard(dashboard); } };
            }

            _quickDashboardsPanel.Controls.Add(btn);
            x += segmentWidth;
        }

        UpdateQuickAccessPanelTheme();
        return anyTextTruncated;
    }

    private void UpdateQuickDashboardSelection() {
        if (_quickDashboardsPanel == null) { return; }
        var currentDashboardId = _currentDashboard?.Id;
        foreach (var button in _quickDashboardsPanel.Controls.OfType<RadioButton>()) {
            if (button.Tag is Guid dashboardId) { button.Checked = dashboardId == currentDashboardId; }
        }
        UpdateDashboardSwitchAppearance();
    }

    // ════════════════════════════════════════════════════════════════
    //  Tray icon
    // ════════════════════════════════════════════════════════════════

    private void SetupTrayIcon() {
        _trayIcon = new NotifyIcon {
            Visible = false,
            Icon = (Icon)Properties.Resources.TrayIconBrown.Clone(),
            Text = "Safety Monitor"
        };

        var trayMenu = new ContextMenuStrip();
        var isLight = _skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.Black : Color.White;

        trayMenu.Items.Add(new ToolStripMenuItem("Restore") {
            Image = MaterialIcons.GetIcon("open_in_full", iconColor, MenuIconSize),
            ImageScaling = ToolStripItemImageScaling.None
        });
        ((ToolStripMenuItem)trayMenu.Items[^1]).Click += (s, e) => RestoreFromTray();

        trayMenu.Items.Add(new ToolStripSeparator());

        trayMenu.Items.Add(new ToolStripMenuItem("Exit") {
            Image = MaterialIcons.GetIcon(MaterialIcons.MenuFileExitApp, iconColor, MenuIconSize),
            ImageScaling = ToolStripItemImageScaling.None
        });
        ((ToolStripMenuItem)trayMenu.Items[^1]).Click += (s, e) => {
            _isExitConfirmed = true;
            RestoreFromTray();
            Close();
        };
        _trayIcon.ContextMenuStrip = trayMenu;
        _trayIcon.DoubleClick += (s, e) => RestoreFromTray();

        _trayRefreshTimer = new System.Windows.Forms.Timer { Interval = _appSettings.RefreshInterval * 1000 };
        _trayRefreshTimer.Tick += async (s, e) => {
            if (_isTrayRefreshing) { return; }
            await RefreshTrayDataAsync();
        };
    }

    private void MinimizeToTray() {
        if (_trayIcon == null) { return; }
        _isMinimizedToTray = true;
        Hide();
        _trayIcon.Visible = true;
        StartTrayRefresh();

        // Startup path: app can begin directly in tray (StartMinimized + MinimizeToTray).
        // In this case, splash screen should close once app is fully in tray.
        SignalStartupReady();
    }

    private void RestoreFromTray() {
        if (_trayIcon == null) { return; }
        StopTrayRefresh();
        _trayIcon.Visible = false;
        _isMinimizedToTray = false;

        // Determine target window state from the state saved before minimize.
        var targetState = _restoreToMaximizedAfterMinimize
            ? FormWindowState.Maximized
            : FormWindowState.Normal;

        // Pre-set _windowStateBeforeMinimize so that OnResize (triggered by
        // WindowState assignment below) does NOT see a Minimized→Normal
        // transition and does not double-call ResumeDashboardRefresh.
        _windowStateBeforeMinimize = targetState;

        Show();
        WindowState = targetState;
        Activate();

        if (!_initialRevealCompleted) {
            // First-ever restore (app was started minimized to tray) —
            // run the full initial dashboard reveal which creates the visor,
            // makes the dashboard visible, refreshes data and sets
            // _initialRevealCompleted = true.
            BeginInitialDashboardReveal();
            _refreshTimer?.Start();
            _refreshCountdownTimer?.Start();
        } else {
            // Normal restore — force full theme and layout repaint because
            // MaterialSkin does not automatically repaint after Hide()/Show().
            UpdateDashboardContainerTheme();
            UpdateQuickAccessPanelTheme();
            UpdateMenuTheme();

            if (_dashboardPanel != null) {
                _dashboardPanel.Visible = true;
                _dashboardPanel.BringToFront();
                _dashboardPanel.UpdateTheme();
            }

            ResumeDashboardRefresh();
        }
    }

    private void StartTrayRefresh() {
        if (_trayRefreshTimer == null) { return; }
        _trayRefreshTimer.Interval = _appSettings.RefreshInterval * 1000;
        _trayRefreshTimer.Start();
        _ = RefreshTrayDataAsync();
    }

    private void StopTrayRefresh() {
        _trayRefreshTimer?.Stop();
    }


    private void SignalStartupReady() {
        if (_startupReadyRaised) {
            return;
        }

        _startupReadyRaised = true;
        StartupReady?.Invoke(this, EventArgs.Empty);
    }

    private async Task RefreshTrayDataAsync() {
        if (_trayIcon == null || !_isMinimizedToTray) { return; }
        _isTrayRefreshing = true;
        try {
            var data = await Task.Run(() => _dataService.GetLatestData());
            if (IsDisposed || !_isMinimizedToTray) { return; }
            UpdateTrayIcon(data);
            UpdateTrayTooltip(data);
        } catch {
            if (!IsDisposed && _isMinimizedToTray) {
                UpdateTrayIcon(null);
                UpdateTrayTooltip(null);
            }
        } finally {
            _isTrayRefreshing = false;
        }
    }

    private void UpdateTrayIcon(DataStorage.Models.ObservingData? data) {
        if (_trayIcon == null) { return; }

        Icon newIcon;
        if (data == null) {
            newIcon = (Icon)Properties.Resources.TrayIconBrown.Clone();
        } else if (data.IsSafe == true) {
            newIcon = (Icon)Properties.Resources.TrayIconGreen.Clone();
        } else if (data.IsSafe == false) {
            newIcon = (Icon)Properties.Resources.TrayIconRed.Clone();
        } else {
            newIcon = (Icon)Properties.Resources.TrayIconBrown.Clone();
        }

        var oldIcon = _trayIcon.Icon;
        _trayIcon.Icon = newIcon;
        oldIcon?.Dispose();
    }

    private void UpdateTrayTooltip(DataStorage.Models.ObservingData? data) {
        if (_trayIcon == null) { return; }

        if (data == null) {
            _trayIcon.Text = "Safety Monitor — no data";
            return;
        }

        var valueSchemes = _valueSchemeService.LoadSchemes();
        var lines = new List<string>();
        foreach (var setting in MetricDisplaySettingsStore.Settings) {
            if (string.IsNullOrWhiteSpace(setting.TrayName)) { continue; }
            var value = setting.Metric.GetValue(data);
            if (value.HasValue) {
                var formatted = MetricDisplaySettingsStore.FormatMetricValue(setting.Metric, value.Value);
                var transformedText = string.IsNullOrWhiteSpace(setting.TrayValueSchemeName)
                    ? null
                    : valueSchemes.FirstOrDefault(s => s.Name == setting.TrayValueSchemeName)?.GetText(value.Value);
                if (string.IsNullOrWhiteSpace(transformedText)) {
                    lines.Add($"{setting.TrayName}: {formatted} {setting.Metric.GetUnit()}");
                } else {
                    lines.Add($"{setting.TrayName}: {transformedText}");
                }
            } else {
                lines.Add($"{setting.TrayName}: —");
            }
        }

        var text = lines.Count > 0
            ? string.Join("\n", lines)
            : "Safety Monitor";

        if (text.Length > 127) { text = text[..124] + "..."; }
        _trayIcon.Text = text;
    }

    // ════════════════════════════════════════════════════════════════
    //  Dashboard refresh pause / resume
    // ════════════════════════════════════════════════════════════════

    private void PauseDashboardRefresh() {
        _refreshTimer?.Stop();
        _refreshCountdownTimer?.Stop();
    }

    private void ResumeDashboardRefresh() {
        _refreshTimer?.Start();
        _refreshCountdownTimer?.Start();
        _lastRefreshTime = DateTime.Now;
        _ = RefreshDashboardDataAsync();
    }

    private void UpdateStatusBar() {
        if (_dataService.IsConnected) { _dataPathLabel.Text = $"Storage: {_appSettings.StoragePath}"; _statusLabel.Text = "Connected to storage"; } else { _dataPathLabel.Text = "Storage: not configured"; _statusLabel.Text = "Storage not configured (File → Settings)"; }
    }

    #endregion Private Methods
}
