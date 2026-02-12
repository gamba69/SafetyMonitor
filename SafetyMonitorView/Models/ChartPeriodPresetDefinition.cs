namespace SafetyMonitorView.Models;

public enum ChartPeriodUnit {
    Minutes,
    Hours,
    Days,
    Weeks,
    Months
}

public class ChartPeriodPresetDefinition {
    public string Uid { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public double Value { get; set; } = 1;
    public ChartPeriodUnit Unit { get; set; } = ChartPeriodUnit.Hours;

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
