namespace SafetyMonitorView.Models;

/// <summary>
/// Stores ScottPlot Y-axis rule settings for a specific metric type.
/// Rules: MaximumBoundary, MinimumBoundary, MaximumSpan, MinimumSpan.
/// </summary>
public class MetricAxisRuleSetting {

    #region Public Properties

    /// <summary>
    /// Whether this rule is enabled and should be applied to charts.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum upper boundary for the Y axis (MaximumBoundary).
    /// Null means no constraint.
    /// </summary>
    public double? MaxBoundary { get; set; }

    /// <summary>
    /// Maximum span (range) of the Y axis (MaximumSpan).
    /// Null means no constraint.
    /// </summary>
    public double? MaxSpan { get; set; }

    /// <summary>
    /// The metric type this rule applies to.
    /// </summary>
    public MetricType Metric { get; set; }

    /// <summary>
    /// Minimum lower boundary for the Y axis (MinimumBoundary).
    /// Null means no constraint.
    /// </summary>
    public double? MinBoundary { get; set; }

    /// <summary>
    /// Minimum span (range) of the Y axis (MinimumSpan).
    /// Null means no constraint.
    /// </summary>
    public double? MinSpan { get; set; }

    #endregion Public Properties
}
