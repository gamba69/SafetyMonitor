namespace SafetyMonitorView.Models;

public class ValueScheme {
    #region Public Properties

    public bool Descending { get; set; }
    public string Name { get; set; } = "Default";
    public List<ValueStop> Stops { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

    public string? GetText(double value) {
        if (Stops.Count == 0) {
            return null;
        }

        if (Descending) {
            var sorted = Stops.OrderByDescending(s => s.Value).ToList();

            foreach (var stop in sorted) {
                if (value >= stop.Value) {
                    return stop.Text;
                }
            }
            return sorted[^1].Text;
        } else {
            var sorted = Stops.OrderBy(s => s.Value).ToList();

            foreach (var stop in sorted) {
                if (value <= stop.Value) {
                    return stop.Text;
                }
            }
            return sorted[^1].Text;
        }
    }

    #endregion Public Methods
}

public class ValueStop {
    #region Public Properties

    public string Description { get; set; } = "";
    public string Text { get; set; } = "";
    public double Value { get; set; }

    #endregion Public Properties
}
