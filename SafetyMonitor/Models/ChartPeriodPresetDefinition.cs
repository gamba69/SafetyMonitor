using System.Text.Json.Serialization;
using SafetyMonitor.Services;

namespace SafetyMonitor.Models;

public enum ChartPeriodUnit {
    Minutes,
    Hours,
    Days,
    Weeks,
    Months
}

/// <summary>
/// Represents chart period preset definition and encapsulates its related behavior and state.
/// </summary>
public class ChartPeriodPresetDefinition {
    /// <summary>
    /// Gets or sets the uid for chart period preset definition. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Uid { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the name for chart period preset definition. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the short name for chart period preset definition. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string ShortName { get; set; } = "";
    /// <summary>
    /// Gets or sets the value for chart period preset definition. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public double Value { get; set; } = 1;
    /// <summary>
    /// Gets or sets the unit for chart period preset definition. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public ChartPeriodUnit Unit { get; set; } = ChartPeriodUnit.Hours;
    [JsonConverter(typeof(ChartAggregationIntervalJsonConverter))]
    /// <summary>
    /// Gets or sets the aggregation interval for chart period preset definition. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public TimeSpan AggregationInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Executes to time span as part of chart period preset definition processing.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TimeSpan ToTimeSpan() {
        return Unit switch {
            ChartPeriodUnit.Minutes => TimeSpan.FromMinutes(Value),
            ChartPeriodUnit.Hours => TimeSpan.FromHours(Value),
            ChartPeriodUnit.Days => TimeSpan.FromDays(Value),
            ChartPeriodUnit.Weeks => TimeSpan.FromDays(Value * 7),
            ChartPeriodUnit.Months => TimeSpan.FromDays(Value * 30),
            _ => TimeSpan.FromHours(Value)
        };
    }
}
