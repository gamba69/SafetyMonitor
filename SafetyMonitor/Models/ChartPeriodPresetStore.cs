namespace SafetyMonitor.Models;

public readonly record struct ChartPeriodPreset(
    string Uid,
    string Label,
    TimeSpan Duration,
    ChartPeriod Period,
    TimeSpan AggregationInterval);

public static class ChartPeriodPresetStore {

    private const int DefaultTargetPointCount = 300;
    private const int DefaultRawDataPointIntervalSeconds = 3;
    private static readonly (string Uid, string Name, double Value, ChartPeriodUnit Unit)[] DefaultPresetDefinitions = [
        ("preset-15-minutes", "15 Minutes", 15, ChartPeriodUnit.Minutes),
        ("preset-1-hour", "1 Hour", 1, ChartPeriodUnit.Hours),
        ("preset-2-hours", "2 Hours", 2, ChartPeriodUnit.Hours),
        ("preset-3-hours", "3 Hours", 3, ChartPeriodUnit.Hours),
        ("preset-6-hours", "6 Hours", 6, ChartPeriodUnit.Hours),
        ("preset-1-day", "1 Day", 1, ChartPeriodUnit.Days),
        ("preset-2-days", "2 Days", 2, ChartPeriodUnit.Days),
        ("preset-3-days", "3 Days", 3, ChartPeriodUnit.Days),
        ("preset-1-week", "1 Week", 1, ChartPeriodUnit.Weeks),
        ("preset-2-weeks", "2 Weeks", 2, ChartPeriodUnit.Weeks),
        ("preset-1-month", "1 Month", 1, ChartPeriodUnit.Months),
        ("preset-2-months", "2 Months", 2, ChartPeriodUnit.Months),
        ("preset-3-months", "3 Months", 3, ChartPeriodUnit.Months),
        ("preset-6-months", "6 Months", 6, ChartPeriodUnit.Months),
        ("preset-1-year", "1 Year", 12, ChartPeriodUnit.Months),
        ("preset-2-years", "2 Years", 24, ChartPeriodUnit.Months),
        ("preset-3-years", "3 Years", 36, ChartPeriodUnit.Months)
    ];

    private static List<ChartPeriodPresetDefinition> _presets = CreateDefaultPresets();

    public static event Action? PresetsChanged;

    public static IReadOnlyList<ChartPeriodPresetDefinition> PresetDefinitions => _presets;

    public static List<ChartPeriodPresetDefinition> CreateDefaultPresets(
        int targetPointCount = DefaultTargetPointCount,
        int rawDataPointIntervalSeconds = DefaultRawDataPointIntervalSeconds) {
        var safeTargetPointCount = Math.Max(2, targetPointCount);
        var safeRawDataPointIntervalSeconds = Math.Max(1, rawDataPointIntervalSeconds);

        return [.. DefaultPresetDefinitions.Select(def => {
            var duration = new ChartPeriodPresetDefinition {
                Value = def.Value,
                Unit = def.Unit
            }.ToTimeSpan();

            return new ChartPeriodPresetDefinition {
                Uid = def.Uid,
                Name = def.Name,
                Value = def.Value,
                Unit = def.Unit,
                AggregationInterval = CalculateDefaultAggregationInterval(duration, safeTargetPointCount, safeRawDataPointIntervalSeconds)
            };
        })];
    }

    public static void SetPresets(IEnumerable<ChartPeriodPresetDefinition>? presets) {
        _presets = NormalizePresets(presets);
        PresetsChanged?.Invoke();
    }

    public static IReadOnlyList<ChartPeriodPreset> GetPresetItems() {
        return _presets
            .Select(def => {
                var duration = def.ToTimeSpan();
                var period = MapDurationToPeriod(duration);
                var aggregationInterval = def.AggregationInterval > TimeSpan.Zero
                    ? def.AggregationInterval
                    : def.AggregationInterval == TimeSpan.Zero
                        ? TimeSpan.Zero
                        : GetRecommendedAggregationInterval(duration);
                return new ChartPeriodPreset(def.Uid, def.Name, duration, period, aggregationInterval);
            })
            .ToList();
    }

    public static int FindMatchingPresetIndex(string? uid, IReadOnlyList<ChartPeriodPreset> presets) {
        if (string.IsNullOrWhiteSpace(uid)) {
            return -1;
        }

        for (int i = 0; i < presets.Count; i++) {
            if (string.Equals(presets[i].Uid, uid, StringComparison.Ordinal)) {
                return i;
            }
        }

        return -1;
    }

    public static ChartPeriodPreset GetFallbackPreset(IReadOnlyList<ChartPeriodPreset> presets) {
        if (presets.Count > 0) {
            return presets[0];
        }

        return new ChartPeriodPreset("preset-1-day", "1 Day", TimeSpan.FromDays(1), ChartPeriod.Last24Hours, TimeSpan.FromMinutes(15));
    }

    public static string FormatDuration(TimeSpan duration) {
        if (duration.TotalDays >= 1 && IsWholeNumber(duration.TotalDays)) {
            var days = (int)Math.Round(duration.TotalDays);
            return days == 1 ? "1 Day" : $"{days} Days";
        }
        if (duration.TotalHours >= 1 && IsWholeNumber(duration.TotalHours)) {
            var hours = (int)Math.Round(duration.TotalHours);
            return hours == 1 ? "1 Hour" : $"{hours} Hours";
        }
        if (duration.TotalMinutes >= 1 && IsWholeNumber(duration.TotalMinutes)) {
            var minutes = (int)Math.Round(duration.TotalMinutes);
            return minutes == 1 ? "1 Minute" : $"{minutes} Minutes";
        }

        var seconds = (int)Math.Round(duration.TotalSeconds);
        return seconds == 1 ? "1 Second" : $"{seconds} Seconds";
    }

    private static ChartPeriod MapDurationToPeriod(TimeSpan duration) {
        if (AreDurationsClose(duration, TimeSpan.FromMinutes(15))) {
            return ChartPeriod.Last15Minutes;
        }
        if (AreDurationsClose(duration, TimeSpan.FromHours(1))) {
            return ChartPeriod.LastHour;
        }
        if (AreDurationsClose(duration, TimeSpan.FromHours(6))) {
            return ChartPeriod.Last6Hours;
        }
        if (AreDurationsClose(duration, TimeSpan.FromHours(24))) {
            return ChartPeriod.Last24Hours;
        }
        if (AreDurationsClose(duration, TimeSpan.FromDays(7))) {
            return ChartPeriod.Last7Days;
        }
        if (AreDurationsClose(duration, TimeSpan.FromDays(30))) {
            return ChartPeriod.Last30Days;
        }

        return ChartPeriod.Custom;
    }

    private static bool AreDurationsClose(TimeSpan a, TimeSpan b) {
        return Math.Abs((a - b).TotalSeconds) < 0.5;
    }

    private static bool IsWholeNumber(double value) {
        return Math.Abs(value - Math.Round(value)) < 0.0001;
    }

    private static List<ChartPeriodPresetDefinition> NormalizePresets(IEnumerable<ChartPeriodPresetDefinition>? presets) {
        var list = new List<ChartPeriodPresetDefinition>();
        var usedUids = new HashSet<string>(StringComparer.Ordinal);

        if (presets != null) {
            foreach (var preset in presets) {
                if (preset == null || string.IsNullOrWhiteSpace(preset.Name) || preset.Value <= 0) {
                    continue;
                }

                var unit = Enum.IsDefined(typeof(ChartPeriodUnit), preset.Unit)
                    ? preset.Unit
                    : ChartPeriodUnit.Hours;

                var uid = string.IsNullOrWhiteSpace(preset.Uid)
                    ? Guid.NewGuid().ToString("N")
                    : preset.Uid.Trim();
                while (!usedUids.Add(uid)) {
                    uid = Guid.NewGuid().ToString("N");
                }

                list.Add(new ChartPeriodPresetDefinition {
                    Uid = uid,
                    Name = preset.Name.Trim(),
                    Value = preset.Value,
                    Unit = unit,
                    AggregationInterval = preset.AggregationInterval > TimeSpan.Zero
                        ? preset.AggregationInterval
                        : preset.AggregationInterval == TimeSpan.Zero
                            ? TimeSpan.Zero
                            : GetRecommendedAggregationInterval(preset.ToTimeSpan())
                });
            }
        }

        if (list.Count == 0) {
            list = CreateDefaultPresets();
        }

        return list;
    }

    private static TimeSpan GetRecommendedAggregationInterval(TimeSpan duration) {
        if (duration <= TimeSpan.FromMinutes(15)) {
            return TimeSpan.FromSeconds(30);
        }
        if (duration <= TimeSpan.FromHours(1)) {
            return TimeSpan.FromMinutes(1);
        }
        if (duration <= TimeSpan.FromHours(6)) {
            return TimeSpan.FromMinutes(5);
        }
        if (duration <= TimeSpan.FromHours(24)) {
            return TimeSpan.FromMinutes(15);
        }
        if (duration <= TimeSpan.FromDays(7)) {
            return TimeSpan.FromHours(1);
        }

        return TimeSpan.FromHours(6);
    }

    private static TimeSpan CalculateDefaultAggregationInterval(TimeSpan duration, int targetPointCount, int rawDataPointIntervalSeconds) {
        var intervalSeconds = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds / targetPointCount));
        var rawInterval = TimeSpan.FromSeconds(rawDataPointIntervalSeconds);
        var generated = TimeSpan.FromSeconds(intervalSeconds);
        return generated <= rawInterval ? TimeSpan.Zero : generated;
    }
}
