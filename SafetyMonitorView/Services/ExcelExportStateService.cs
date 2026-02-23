namespace SafetyMonitorView.Services;

public static class ExcelExportStateService {

    #region Private Fields

    private static readonly Lock _lock = new();
    private static volatile bool _isExporting;
    private static volatile int _progressPercent;

    #endregion Private Fields

    #region Public Properties

    public static bool IsExporting => _isExporting;
    public static int ProgressPercent => _progressPercent;

    #endregion Public Properties

    #region Public Events

    public static event Action? StateChanged;

    #endregion Public Events

    #region Public Methods

    public static bool TryBeginExport() {
        lock (_lock) {
            if (_isExporting) {
                return false;
            }

            _isExporting = true;
            _progressPercent = 0;
        }

        StateChanged?.Invoke();
        return true;
    }

    public static void ReportProgress(int percent) {
        _progressPercent = Math.Clamp(percent, 0, 100);
        StateChanged?.Invoke();
    }

    public static void EndExport() {
        lock (_lock) {
            _isExporting = false;
            _progressPercent = 0;
        }

        StateChanged?.Invoke();
    }

    #endregion Public Methods
}
