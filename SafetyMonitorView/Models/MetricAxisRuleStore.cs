namespace SafetyMonitorView.Models;

/// <summary>
/// Global in-memory store for metric axis rule settings.
/// Notifies subscribers when rules change so charts can re-apply them.
/// </summary>
public static class MetricAxisRuleStore {

    #region Private Fields

    private static List<MetricAxisRuleSetting> _rules = [];

    #endregion Private Fields

    #region Public Events

    public static event Action? RulesChanged;

    #endregion Public Events

    #region Public Properties

    public static IReadOnlyList<MetricAxisRuleSetting> Rules => _rules;

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Returns the enabled rule for a given metric, or null if none exists.
    /// </summary>
    public static MetricAxisRuleSetting? GetRule(MetricType metric) {
        return _rules.FirstOrDefault(r => r.Enabled && r.Metric == metric);
    }

    /// <summary>
    /// Replaces the entire rule set and fires the change event.
    /// </summary>
    public static void SetRules(IEnumerable<MetricAxisRuleSetting>? rules) {
        _rules = rules != null
            ? [.. rules.Select(r => new MetricAxisRuleSetting {
                Metric = r.Metric,
                Enabled = r.Enabled,
                MinBoundary = r.MinBoundary,
                MaxBoundary = r.MaxBoundary,
                MinSpan = r.MinSpan,
                MaxSpan = r.MaxSpan
            })]
            : [];
        RulesChanged?.Invoke();
    }

    #endregion Public Methods
}
