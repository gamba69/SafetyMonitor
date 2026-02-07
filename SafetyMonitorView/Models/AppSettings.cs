namespace SafetyMonitorView.Models;

public class AppSettings {
    #region Public Properties

    // Theme settings
    public bool IsDarkTheme { get; set; } = false;
    public bool IsMaximized { get; set; } = false;

    // Dashboard settings
    public Guid? LastDashboardId { get; set; }
    public int RefreshInterval { get; set; } = 5;

    // Data settings
    public string StoragePath { get; set; } = "";
    public int WindowHeight { get; set; } = 900;

    // Window settings
    public int WindowWidth { get; set; } = 1400;
    public int WindowX { get; set; } = -1;  // -1 = center
    public int WindowY { get; set; } = -1;

    #endregion Public Properties

    // -1 = center
}
