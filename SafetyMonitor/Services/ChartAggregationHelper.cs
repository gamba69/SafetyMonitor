using SafetyMonitor.Models;

namespace SafetyMonitor.Services;

public static class ChartAggregationHelper {

    private static readonly TimeSpan[] DefaultFixedAggregationIntervals = [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4),
        TimeSpan.FromHours(12),
        TimeSpan.FromDays(1),
        TimeSpan.FromDays(3),
        TimeSpan.FromDays(7)
    ];

    private static readonly (TimeSpan Interval, string Label)[] AggregationLabels = [
        (TimeSpan.Zero, "raw"),
        (TimeSpan.FromSeconds(10), "10s"),
        (TimeSpan.FromSeconds(30), "30s"),
        (TimeSpan.FromMinutes(1), "1m"),
        (TimeSpan.FromMinutes(5), "5m"),
        (TimeSpan.FromMinutes(15), "15m"),
        (TimeSpan.FromHours(1), "1h"),
        (TimeSpan.FromHours(4), "4h"),
        (TimeSpan.FromHours(12), "12h"),
        (TimeSpan.FromDays(1), "1d"),
        (TimeSpan.FromDays(3), "3d"),
        (TimeSpan.FromDays(7), "1w")
    ];

    public static IReadOnlyList<TimeSpan> SupportedAggregationIntervals => AggregationLabels.Select(x => x.Interval).ToArray();

    public static TimeSpan CalculateAutomaticAggregationInterval(
        TimeSpan range,
        double presetMatchTolerancePercent,
        int targetPointCount,
        IEnumerable<(TimeSpan Duration, TimeSpan AggregationInterval)> presetCandidates,
        bool applyPeriodMatching = true) {


        var rangeSeconds = range.TotalSeconds;
        if (rangeSeconds <= 1) {
            return TimeSpan.Zero;
        }

        if (applyPeriodMatching) {
            var toleranceRatio = Math.Clamp(presetMatchTolerancePercent, 0, 100) / 100d;
            var matchingPreset = presetCandidates
                .Where(x => x.Duration > TimeSpan.Zero && x.AggregationInterval >= TimeSpan.Zero)
                .Select(x => new {
                    x.AggregationInterval,
                    RelativeDeviation = Math.Abs(range.TotalSeconds - x.Duration.TotalSeconds) / x.Duration.TotalSeconds
                })
                .Where(x => x.RelativeDeviation <= toleranceRatio)
                .OrderBy(x => x.RelativeDeviation)
                .FirstOrDefault();

            if (matchingPreset != null) {
                return matchingPreset.AggregationInterval;
            }
        }

        var safeTargetPointCount = Math.Max(2, targetPointCount);
        var intervalSeconds = Math.Max(1, (int)Math.Ceiling(rangeSeconds / safeTargetPointCount));
        var intervalTarget = TimeSpan.FromSeconds(intervalSeconds);

        var fixedCandidates = presetCandidates
            .Select(x => x.AggregationInterval)
            .Concat(DefaultFixedAggregationIntervals)
            .Where(x => x > TimeSpan.Zero)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        if (fixedCandidates.Count == 0) {
            return intervalTarget;
        }

        return fixedCandidates
            .OrderBy(x => Math.Abs((x - intervalTarget).TotalSeconds))
            .First();
    }

    public static string FormatAggregationLabel(TimeSpan? interval) {
        if (!interval.HasValue || interval.Value <= TimeSpan.Zero) {
            return "raw";
        }

        var normalizedInterval = NormalizeAggregationInterval(interval.Value);
        return AggregationLabels.First(x => x.Interval == normalizedInterval).Label;
    }

    public static TimeSpan NormalizeAggregationInterval(TimeSpan interval) {
        if (interval <= TimeSpan.Zero) {
            return TimeSpan.Zero;
        }

        return SupportedAggregationIntervals
            .Where(x => x > TimeSpan.Zero)
            .OrderBy(x => Math.Abs((x - interval).TotalSeconds))
            .First();
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
