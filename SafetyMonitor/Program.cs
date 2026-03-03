using DataStorage;
using SafetyMonitor.Forms;
using SafetyMonitor.Models;
using SafetyMonitor.Services;
using System.Diagnostics;
using System.Threading;

namespace SafetyMonitor;

static class Program {
    internal static class StartupLaunchOptions {
        public static bool IgnoreStartMinimized { get; set; }
    }

    private const string SingleInstanceMutexName = "SafetyMonitor.SingleInstance";
    private const int SplashMinimumVisibleMs = 1500;

    #region Private Methods

    [STAThread]
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

        private void OnSplashClosed(object? sender, FormClosedEventArgs e) {
            if (_splashHiddenForStartupDialogs) {
                return;
            }

            if (_mainForm == null || _mainForm.IsDisposed) {
                ExitThread();
            }
        }

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


        private void ApplyStartupTheme() {
            var skinManager = MaterialSkin.MaterialSkinManager.Instance;
            skinManager.Theme = _appSettings.IsDarkTheme
                ? MaterialSkin.MaterialSkinManager.Themes.DARK
                : MaterialSkin.MaterialSkinManager.Themes.LIGHT;
            skinManager.ColorScheme = AppColorizationService.Instance.GetMaterialColorScheme(_appSettings.MaterialColorScheme);
        }

        private void HideSplashForStartupDialogs() {
            if (_splashHiddenForStartupDialogs || _splashForm.IsDisposed) {
                return;
            }

            _splashHiddenForStartupDialogs = true;
            _splashForm.Hide();
        }

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

        private bool EnsureStorageConfigurationAtStartup() {
            var validation = DataStorage.DataStorage.ValidateStorageStructure(_appSettings.StoragePath, _appSettings.ValidateDatabaseStructureOnStartup);

            if (validation.HasErrors) {
                HideSplashForStartupDialogs();
                var details = string.Join(Environment.NewLine, validation.Issues.Where(x => x.Severity == DataStorage.DataStorage.StorageValidationIssueSeverity.Error).Select(x => $"• {x.Message}"));
                ThemedMessageBox.Show($"Storage data structure does not match expected format. The application will be closed.\n\n{details}", "Storage structure error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _startupAborted = true;
                ExitThread();
                return false;
            }

            if (validation.HasWarnings) {
                StartupLaunchOptions.IgnoreStartMinimized = true;
                HideSplashForStartupDialogs();
                var details = string.Join(Environment.NewLine, validation.Issues.Where(x => x.Severity == DataStorage.DataStorage.StorageValidationIssueSeverity.Warning).Select(x => $"• {x.Message}"));
                var answer = ThemedMessageBox.Show($"Data storage is not configured.\n\n{details}\n\nOpen Database settings now?", "Storage not configured", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                return answer == DialogResult.Yes;
            }

            return false;
        }

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
