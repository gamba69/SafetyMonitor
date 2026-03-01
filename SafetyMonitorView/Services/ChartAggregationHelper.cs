using SafetyMonitorView.Models;

namespace SafetyMonitorView.Services;

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

        var value = interval.Value;
        if (value.TotalSeconds < 60 && Math.Abs(value.TotalSeconds - Math.Round(value.TotalSeconds)) < 0.001) {
            return $"{Math.Round(value.TotalSeconds):0}s";
        }

        if (value.TotalMinutes < 60 && Math.Abs(value.TotalMinutes - Math.Round(value.TotalMinutes)) < 0.001) {
            return $"{Math.Round(value.TotalMinutes):0}m";
        }

        if (value.TotalHours < 24 && Math.Abs(value.TotalHours - Math.Round(value.TotalHours)) < 0.001) {
            return $"{Math.Round(value.TotalHours):0}h";
        }

        if (Math.Abs(value.TotalDays - Math.Round(value.TotalDays)) < 0.001) {
            return $"{Math.Round(value.TotalDays):0}d";
        }

        return value.ToString(@"hh\:mm\:ss");
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
