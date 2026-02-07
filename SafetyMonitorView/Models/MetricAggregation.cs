namespace SafetyMonitorView.Models;

public class MetricAggregation {

    #region Public Properties

    public Color Color { get; set; } = Color.Blue;
    public DataStorage.Models.AggregationFunction Function { get; set; }
    public string Label { get; set; } = "";
    public float LineWidth { get; set; } = 2f;
    public MetricType Metric { get; set; }
    public bool ShowMarkers { get; set; } = false;

    #endregion Public Properties
}
