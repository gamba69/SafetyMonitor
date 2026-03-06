namespace SafetyMonitor.Models;

/// <summary>
/// Represents metric axis rule setting and encapsulates its related behavior and state.
/// </summary>
public class MetricAxisRuleSetting {

    #region Public Properties

    /// <summary>
    /// Gets or sets the enabled for metric axis rule setting. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the max boundary for metric axis rule setting. Specifies sizing or boundary constraints used by runtime calculations.
    /// </summary>
    public double? MaxBoundary { get; set; }

    /// <summary>
    /// Gets or sets the max span for metric axis rule setting. Specifies sizing or boundary constraints used by runtime calculations.
    /// </summary>
    public double? MaxSpan { get; set; }

    /// <summary>
    /// Gets or sets the metric for metric axis rule setting. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public MetricType Metric { get; set; }

    /// <summary>
    /// Gets or sets the min boundary for metric axis rule setting. Specifies sizing or boundary constraints used by runtime calculations.
    /// </summary>
    public double? MinBoundary { get; set; }

    /// <summary>
    /// Gets or sets the min span for metric axis rule setting. Specifies sizing or boundary constraints used by runtime calculations.
    /// </summary>
    public double? MinSpan { get; set; }

    #endregion Public Properties
}
