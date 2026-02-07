using SafetyMonitorView.Forms;

namespace SafetyMonitorView;

static class Program {
    #region Private Methods

    [STAThread]
    static void Main() {
        // Explicit DPI configuration before any controls are created
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Now run the application
        Application.Run(new MainForm());
    }

    #endregion Private Methods
}
