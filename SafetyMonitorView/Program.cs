using SafetyMonitorView.Forms;
using System.Threading;

namespace SafetyMonitorView;

static class Program {
    private const string SingleInstanceMutexName = "SafetyMonitorView.SingleInstance";

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

        // Now run the application
        Application.Run(new MainForm());
    }

    #endregion Private Methods
}
