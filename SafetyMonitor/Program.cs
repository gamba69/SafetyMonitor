using DataStorage;
using SafetyMonitor.Forms;
using SafetyMonitor.Models;
using SafetyMonitor.Services;
using System.Diagnostics;
using System.Threading;

namespace SafetyMonitor;

/// <summary>
/// Represents program and encapsulates its related behavior and state.
/// </summary>
static class Program {
    /// <summary>
    /// Represents startup launch options and encapsulates its related behavior and state.
    /// </summary>
    internal static class StartupLaunchOptions {
        /// <summary>
        /// Gets or sets the ignore start minimized for startup launch options. Represents a state flag that enables or disables related behavior.
        /// </summary>
        public static bool IgnoreStartMinimized { get; set; }
    }

    private const string SingleInstanceMutexName = "SafetyMonitor.SingleInstance";
    private const int SplashMinimumVisibleMs = 1500;

    #region Private Methods

    [STAThread]
    /// <summary>
    /// Defines the application entry point and startup workflow.
    /// </summary>
    static void Main() {
        using Mutex singleInstanceMutex = new(false, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance) {
            MessageBox.Show(
                "The application is already running. Launching a second instance is not allowed.",
                "Safety Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return;
        }

        // Explicit DPI configuration before any controls are created
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var appSettingsService = new AppSettingsService();
        var settingsMaintenanceService = new AppSettingsMaintenanceService(appSettingsService, new DashboardService());
        settingsMaintenanceService.EnsureSettingsExists();
        var appSettings = appSettingsService.LoadSettings();
        var context = new SplashApplicationContext(appSettings.IsDarkTheme, SplashMinimumVisibleMs, appSettingsService, settingsMaintenanceService, appSettings);

        Application.Run(context);
    }

    #endregion Private Methods

    /// <summary>
    /// Represents splash application context and encapsulates its related behavior and state.
    /// </summary>
    private sealed class SplashApplicationContext : ApplicationContext {
        private readonly int _minimumVisibleMs;
        private readonly SplashForm _splashForm;
        private readonly Stopwatch _startupStopwatch = Stopwatch.StartNew();
        private readonly AppSettingsService _appSettingsService;
        private readonly AppSettingsMaintenanceService _settingsMaintenanceService;
        private AppSettings _appSettings;
        private MainForm? _mainForm;
        private bool _startupReady;
        private bool _startupAborted;
        private bool _splashHiddenForStartupDialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplashApplicationContext"/> class.
        /// </summary>
        /// <param name="isDarkTheme">Input value for is dark theme.</param>
        /// <param name="minimumVisibleMs">Input value for minimum visible ms.</param>
        /// <param name="appSettingsService">Input value for app settings service.</param>
        /// <param name="settingsMaintenanceService">Input value for settings maintenance service.</param>
        /// <param name="appSettings">Input value for app settings.</param>
        /// <remarks>
        /// The constructor wires required dependencies and initial state.
        /// </remarks>
        public SplashApplicationContext(bool isDarkTheme, int minimumVisibleMs, AppSettingsService appSettingsService, AppSettingsMaintenanceService settingsMaintenanceService, AppSettings appSettings) {
            _minimumVisibleMs = minimumVisibleMs;
            _appSettingsService = appSettingsService;
            _settingsMaintenanceService = settingsMaintenanceService;
            _appSettings = appSettings;
            _splashForm = new SplashForm(isDarkTheme);
            _splashForm.FormClosed += OnSplashClosed;
            _splashForm.Shown += OnSplashShown;
            _splashForm.Show();
        }

        /// <summary>
        /// Executes on splash closed as part of splash application context processing.
        /// </summary>
        /// <param name="sender">Input value for sender.</param>
        /// <param name="e">Input value for e.</param>
        private void OnSplashClosed(object? sender, FormClosedEventArgs e) {
            if (_splashHiddenForStartupDialogs) {
                return;
            }

            if (_mainForm == null || _mainForm.IsDisposed) {
                ExitThread();
            }
        }

        /// <summary>
        /// Executes on splash shown as part of splash application context processing.
        /// </summary>
        /// <param name="sender">Input value for sender.</param>
        /// <param name="e">Input value for e.</param>
        private void OnSplashShown(object? sender, EventArgs e) {
            _splashForm.Shown -= OnSplashShown;

            var delayedStartTimer = new System.Windows.Forms.Timer { Interval = 1 };
            delayedStartTimer.Tick += (_, _) => {
                delayedStartTimer.Stop();
                delayedStartTimer.Dispose();
                HandleStartupValidationAndStart();
            };
            delayedStartTimer.Start();
        }


        /// <summary>
        /// Applies the startup theme for splash application context.
        /// </summary>
        private void ApplyStartupTheme() {
            var skinManager = MaterialSkin.MaterialSkinManager.Instance;
            skinManager.Theme = _appSettings.IsDarkTheme
                ? MaterialSkin.MaterialSkinManager.Themes.DARK
                : MaterialSkin.MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = AppColorizationService.Instance.GetMaterialColorScheme(_appSettings.MaterialColorScheme);
        }

        /// <summary>
        /// Hides the splash for startup dialogs for splash application context.
        /// </summary>
        private void HideSplashForStartupDialogs() {
            if (_splashHiddenForStartupDialogs || _splashForm.IsDisposed) {
                return;
            }

            _splashHiddenForStartupDialogs = true;
            _splashForm.Hide();
        }

        /// <summary>
        /// Handles the startup validation and start for splash application context.
        /// </summary>
        private void HandleStartupValidationAndStart() {
            ApplyStartupTheme();

            while (true) {
                var openDatabaseSettings = EnsureStorageConfigurationAtStartup();
                if (_startupAborted) {
                    return;
                }

                if (!openDatabaseSettings) {
                    break;
                }

                ShowStartupSettingsDialog();
            }

            StartMainForm();
        }

        /// <summary>
        /// Ensures the storage configuration at startup for splash application context.
        /// </summary>
        /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// Use the boolean result to branch success and fallback logic.
        /// </remarks>
        private bool EnsureStorageConfigurationAtStartup() {
            var validation = DataStorage.DataStorage.ValidateStorageStructure(_appSettings.StoragePath, _appSettings.ValidateDatabaseStructureOnStartup);

            if (validation.HasErrors) {
                HideSplashForStartupDialogs();
                var details = StorageValidationMessageFormatter.BuildMessage(
                    validation.Issues.Where(x => x.Severity == DataStorage.DataStorage.StorageValidationIssueSeverity.Error),
                    "Unable to open data storage: structure validation failed.",
                    "Check the database path and restore storage structure, then restart the application.");
                ThemedMessageBox.Show(
                    details,
                    "Storage structure error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _startupAborted = true;
                ExitThread();
                return false;
            }

            if (validation.HasWarnings) {
                StartupLaunchOptions.IgnoreStartMinimized = true;
                HideSplashForStartupDialogs();
                var details = StorageValidationMessageFormatter.BuildMessage(
                    validation.Issues.Where(x => x.Severity == DataStorage.DataStorage.StorageValidationIssueSeverity.Warning),
                    "Data storage is not configured or contains unexpected files.",
                    "To continue, open Settings and provide valid database parameters.\nOpen Settings now?");
                var answer = ThemedMessageBox.Show(
                    details,
                    "Storage not configured",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                return answer == DialogResult.Yes;
            }

            return false;
        }

        /// <summary>
        /// Shows the startup settings dialog for splash application context.
        /// </summary>
        private void ShowStartupSettingsDialog() {
            ApplyStartupTheme();

            using var settingsForm = new SettingsForm(
                _settingsMaintenanceService,
                _appSettings.StoragePath,
                _appSettings.RefreshInterval,
                _appSettings.ValueTileLookbackMinutes,
                _appSettings.ChartStaticModeTimeoutSeconds,
                _appSettings.ChartStaticAggregationPresetMatchTolerancePercent,
                _appSettings.ChartStaticAggregationTargetPointCount,
                _appSettings.ChartRawDataPointIntervalSeconds,
                _appSettings.ShowRefreshIndicator,
                _appSettings.MinimizeToTray,
                _appSettings.StartMinimized,
                _appSettings.ValidateDatabaseStructureOnStartup,
                _appSettings.MaterialColorScheme,
                initialTabIndex: 2);

            var result = settingsForm.ShowDialog();
            if (result != DialogResult.OK) {
                return;
            }

            if (settingsForm.SettingsMaintenanceAction == SettingsMaintenanceAction.Import || settingsForm.SettingsMaintenanceAction == SettingsMaintenanceAction.Reset) {
                _appSettings = _appSettingsService.LoadSettings();
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
            _appSettings.ChartRawDataPointIntervalSeconds = settingsForm.ChartRawDataPointIntervalSeconds;
            _appSettings.ShowRefreshIndicator = settingsForm.ShowRefreshIndicator;
            _appSettings.MinimizeToTray = settingsForm.MinimizeToTray;
            _appSettings.StartMinimized = settingsForm.StartMinimized;
            _appSettings.MaterialColorScheme = settingsForm.MaterialColorScheme;
            _appSettings.ValidateDatabaseStructureOnStartup = settingsForm.ValidateDatabaseStructureOnStartup;
            _appSettingsService.SaveSettings(_appSettings);
        }

        /// <summary>
        /// Starts the main form for splash application context.
        /// </summary>
        private void StartMainForm() {
            try {
                _mainForm = new MainForm();
                StartupLaunchOptions.IgnoreStartMinimized = false;
                _mainForm.StartupReady += OnMainFormStartupReady;
                _mainForm.FormClosed += OnMainFormClosed;
                _mainForm.Show();
            } catch (OperationCanceledException) {
                StartupLaunchOptions.IgnoreStartMinimized = false;
                if (!_splashForm.IsDisposed) {
                    _splashForm.Close();
                }

                ExitThread();
            }
        }

        /// <summary>
        /// Executes on main form startup ready as part of splash application context processing.
        /// </summary>
        /// <param name="sender">Input value for sender.</param>
        /// <param name="e">Input value for e.</param>
        private async void OnMainFormStartupReady(object? sender, EventArgs e) {
            if (_startupReady) {
                return;
            }

            _startupReady = true;
            var remainingMs = _minimumVisibleMs - (int)_startupStopwatch.ElapsedMilliseconds;
            if (remainingMs > 0) {
                await Task.Delay(remainingMs);
            }

            if (!_splashForm.IsDisposed) {
                _splashForm.Close();
            }

            if (_mainForm != null && !_mainForm.IsDisposed && _mainForm.Visible) {
                _mainForm.Activate();
            }
        }

        /// <summary>
        /// Executes on main form closed as part of splash application context processing.
        /// </summary>
        /// <param name="sender">Input value for sender.</param>
        /// <param name="e">Input value for e.</param>
        private void OnMainFormClosed(object? sender, FormClosedEventArgs e) {
            if (_mainForm != null) {
                _mainForm.StartupReady -= OnMainFormStartupReady;
                _mainForm.FormClosed -= OnMainFormClosed;
            }

            if (!_splashForm.IsDisposed) {
                _splashForm.Close();
                return;
            }

            ExitThread();
        }
    }
}
