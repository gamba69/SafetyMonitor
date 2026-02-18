using SafetyMonitorView.Models;

namespace SafetyMonitorView.Services;

public static class ChartAggregationHelper {

    public static TimeSpan CalculateAutomaticAggregationInterval(
        TimeSpan range,
        double presetMatchTolerancePercent,
        int targetPointCount,
        IEnumerable<(TimeSpan Duration, TimeSpan AggregationInterval)> presetCandidates,
        bool applyPeriodMatching = true) {

        var rangeSeconds = range.TotalSeconds;
        if (rangeSeconds <= 1) {
            return TimeSpan.FromSeconds(1);
        }

        if (applyPeriodMatching) {
            var toleranceRatio = Math.Clamp(presetMatchTolerancePercent, 0, 100) / 100d;
            var matchingPreset = presetCandidates
                .Where(x => x.Duration > TimeSpan.Zero && x.AggregationInterval > TimeSpan.Zero)
                .Select(x => new {
                    x.AggregationInterval,
                    RelativeDeviation = Math.Abs(range.TotalSeconds - x.Duration.TotalSeconds) / x.Duration.TotalSeconds
                })
                .Where(x => x.RelativeDeviation <= toleranceRatio)
                .OrderBy(x => x.RelativeDeviation)
                .FirstOrDefault();

            if (matchingPreset != null && matchingPreset.AggregationInterval > TimeSpan.Zero) {
                return matchingPreset.AggregationInterval;
            }
        }

        var safeTargetPointCount = Math.Max(2, targetPointCount);
        var intervalSeconds = Math.Max(1, (int)Math.Ceiling(rangeSeconds / safeTargetPointCount));
        return TimeSpan.FromSeconds(intervalSeconds);
    }

    public static TimeSpan BuildPeriodDuration(double value, ChartPeriodUnit unit) {
        if (value <= 0) {
            return TimeSpan.Zero;
        }

        return unit switch {
            ChartPeriodUnit.Minutes => TimeSpan.FromMinutes(value),
            ChartPeriodUnit.Hours => TimeSpan.FromHours(value),
            ChartPeriodUnit.Days => TimeSpan.FromDays(value),
            ChartPeriodUnit.Weeks => TimeSpan.FromDays(value * 7),
            ChartPeriodUnit.Months => TimeSpan.FromDays(value * 30),
            _ => TimeSpan.FromHours(value)
        };
    }
}
