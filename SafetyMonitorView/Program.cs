using SafetyMonitorView.Forms;
using SafetyMonitorView.Services;
using System.Diagnostics;
using System.Threading;

namespace SafetyMonitorView;

static class Program {
    private const string SingleInstanceMutexName = "SafetyMonitorView.SingleInstance";
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

        var appSettings = new AppSettingsService().LoadSettings();
        var context = new SplashApplicationContext(appSettings.IsDarkTheme, SplashMinimumVisibleMs);

        Application.Run(context);
    }

    #endregion Private Methods

    private sealed class SplashApplicationContext : ApplicationContext {
        private readonly int _minimumVisibleMs;
        private readonly SplashForm _splashForm;
        private readonly Stopwatch _startupStopwatch = Stopwatch.StartNew();
        private MainForm? _mainForm;
        private bool _startupReady;

        public SplashApplicationContext(bool isDarkTheme, int minimumVisibleMs) {
            _minimumVisibleMs = minimumVisibleMs;
            _splashForm = new SplashForm(isDarkTheme);
            _splashForm.FormClosed += OnSplashClosed;
            _splashForm.Shown += OnSplashShown;
            _splashForm.Show();
        }

        private void OnSplashClosed(object? sender, FormClosedEventArgs e) {
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
                StartMainForm();
            };
            delayedStartTimer.Start();
        }

        private void StartMainForm() {
            _mainForm = new MainForm();
            _mainForm.StartupReady += OnMainFormStartupReady;
            _mainForm.FormClosed += OnMainFormClosed;
            _mainForm.Show();
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
