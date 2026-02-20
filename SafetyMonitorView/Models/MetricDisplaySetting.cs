namespace SafetyMonitorView.Models;

public class MetricDisplaySetting {

    public int Decimals { get; set; } = 2;
    public bool HideZeroes { get; set; }
    public bool InvertY { get; set; }
    public MetricType Metric { get; set; }
    public string TrayName { get; set; } = string.Empty;
}
