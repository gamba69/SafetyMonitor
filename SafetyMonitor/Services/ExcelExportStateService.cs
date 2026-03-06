namespace SafetyMonitor.Services;

/// <summary>
/// Represents excel export state service and encapsulates its related behavior and state.
/// </summary>
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

    /// <summary>
    /// Attempts to begin export for excel export state service.
    /// </summary>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
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

    /// <summary>
    /// Executes report progress as part of excel export state service processing.
    /// </summary>
    /// <param name="percent">Input value for percent.</param>
    public static void ReportProgress(int percent) {
        _progressPercent = Math.Clamp(percent, 0, 100);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Executes end export as part of excel export state service processing.
    /// </summary>
    public static void EndExport() {
        lock (_lock) {
            _isExporting = false;
            _progressPercent = 0;
        }

        StateChanged?.Invoke();
    }

    #endregion Public Methods
}
