namespace SafetyMonitor.Models;

/// <summary>
/// Represents metric axis rule store and encapsulates its related behavior and state.
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
    /// Gets the rule for metric axis rule store.
    /// </summary>
    /// <param name="metric">Input value for metric.</param>
    /// <returns>The result of the operation.</returns>
    public static MetricAxisRuleSetting? GetRule(MetricType metric) {
        return _rules.FirstOrDefault(r => r.Enabled && r.Metric == metric);
    }

    /// <summary>
    /// Sets the rules for metric axis rule store.
    /// </summary>
    /// <param name="rules">Collection of rules items used by the operation.</param>
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
