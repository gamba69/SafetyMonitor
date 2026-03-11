using SafetyMonitor.Services;

namespace SafetyMonitor.Models;

/// <summary>
/// Performs the chart period preset operation.
/// </summary>
public readonly record struct ChartPeriodPreset(
    string Uid,
    string Label,
    string ShortLabel,
    TimeSpan Duration,
    ChartPeriod Period,
    TimeSpan AggregationInterval);

/// <summary>
/// Represents chart period preset store and encapsulates its related behavior and state.
/// </summary>
public static class ChartPeriodPresetStore {

    private const int DefaultTargetPointCount = 300;
    private const int DefaultRawDataPointIntervalSeconds = 3;
    private static readonly (string Uid, string Name, string ShortName, double Value, ChartPeriodUnit Unit)[] DefaultPresetDefinitions = [
        ("15m", "15 minutes", "15m", 15, ChartPeriodUnit.Minutes),
        ("30m", "30 minutes", "30m", 30, ChartPeriodUnit.Minutes),
        ("45m", "45 minutes", "45m", 45, ChartPeriodUnit.Minutes),
        ("60m", "60 minutes", "60m", 60, ChartPeriodUnit.Minutes),
        ("90m", "90 minutes", "90m", 90, ChartPeriodUnit.Minutes),
        ("120m", "120 minutes", "120m", 120, ChartPeriodUnit.Minutes),
        ("3h", "3 hours", "3h", 3, ChartPeriodUnit.Hours),
        ("4h", "4 hours", "4h", 4, ChartPeriodUnit.Hours),
        ("6h", "6 hours", "6h", 6, ChartPeriodUnit.Hours),
        ("12h", "12 hours", "12h", 12, ChartPeriodUnit.Hours),
        ("24h", "24 hours", "24h", 24, ChartPeriodUnit.Hours),
        ("36h", "36 hours", "36h", 36, ChartPeriodUnit.Hours),
        ("48h", "48 hours", "48h", 48, ChartPeriodUnit.Hours),
        ("3d", "3 days", "3d", 3, ChartPeriodUnit.Days),
        ("5d", "5 days", "5d", 5, ChartPeriodUnit.Days),
        ("7d", "7 days", "7d", 7, ChartPeriodUnit.Days),
        ("15d", "15 days", "15d", 15, ChartPeriodUnit.Days),
        ("30d", "30 days", "30d", 30, ChartPeriodUnit.Days),
        ("2mo", "2 months", "2mo", 2, ChartPeriodUnit.Months),
        ("3mo", "3 months", "3mo", 3, ChartPeriodUnit.Months),
        ("4mo", "4 months", "4mo", 4, ChartPeriodUnit.Months),
        ("6mo", "6 months", "6mo", 6, ChartPeriodUnit.Months),
        ("1yr", "1 year", "1yr", 12, ChartPeriodUnit.Months),
        ("2yr", "2 years", "2yr", 24, ChartPeriodUnit.Months),
        ("3yr", "3 years", "3yr", 36, ChartPeriodUnit.Months),
        ("5yr", "5 years", "5yr", 60, ChartPeriodUnit.Months)
    ];

    private static readonly Dictionary<string, string> LegacyPresetUidAliases = new(StringComparer.Ordinal) {
        ["preset-15-minutes"] = "15m",
        ["preset-1-hour"] = "60m",
        ["preset-2-hours"] = "120m",
        ["preset-3-hours"] = "3h",
        ["preset-6-hours"] = "6h",
        ["preset-1-day"] = "24h",
        ["preset-2-days"] = "48h",
        ["preset-3-days"] = "3d",
        ["preset-1-week"] = "7d",
        ["preset-2-weeks"] = "15d",
        ["preset-1-month"] = "30d",
        ["preset-2-months"] = "2mo",
        ["preset-3-months"] = "3mo",
        ["preset-6-months"] = "6mo",
        ["preset-1-year"] = "1yr",
        ["preset-2-years"] = "2yr",
        ["preset-3-years"] = "3yr"
    };

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
                ShortName = def.ShortName,
                Value = def.Value,
                Unit = def.Unit,
                AggregationInterval = CalculateDefaultAggregationInterval(duration, safeTargetPointCount, safeRawDataPointIntervalSeconds)
            };
        })];
    }

    /// <summary>
    /// Recalculates aggregation intervals for the provided preset definitions.
    /// </summary>
    /// <param name="presets">Collection of presets items used by the operation.</param>
    /// <param name="targetPointCount">Input value for target point count.</param>
    /// <param name="rawDataPointIntervalSeconds">Input value for raw data point interval seconds.</param>
    /// <returns>The recalculated preset list.</returns>
    public static List<ChartPeriodPresetDefinition> RecalculateAggregationIntervals(
        IEnumerable<ChartPeriodPresetDefinition>? presets,
        int targetPointCount,
        int rawDataPointIntervalSeconds) {
        var normalized = NormalizePresets(presets);
        var safeTargetPointCount = Math.Max(2, targetPointCount);
        var safeRawDataPointIntervalSeconds = Math.Max(1, rawDataPointIntervalSeconds);

        return [.. normalized.Select(preset => {
            var duration = preset.ToTimeSpan();
            return new ChartPeriodPresetDefinition {
                Uid = preset.Uid,
                Name = preset.Name,
                ShortName = preset.ShortName,
                Value = preset.Value,
                Unit = preset.Unit,
                AggregationInterval = CalculateDefaultAggregationInterval(duration, safeTargetPointCount, safeRawDataPointIntervalSeconds)
            };
        })];
    }

    /// <summary>
    /// Sets the presets for chart period preset store.
    /// </summary>
    /// <param name="presets">Collection of presets items used by the operation.</param>
    public static void SetPresets(IEnumerable<ChartPeriodPresetDefinition>? presets) {
        _presets = NormalizePresets(presets);
        PresetsChanged?.Invoke();
    }

    /// <summary>
    /// Gets the preset items for chart period preset store.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<ChartPeriodPreset> GetPresetItems() {
        return _presets
            .Select(def => {
                var duration = def.ToTimeSpan();
                var period = MapDurationToPeriod(duration);
                var aggregationInterval = def.AggregationInterval > TimeSpan.Zero
                    ? ChartAggregationHelper.NormalizeAggregationInterval(def.AggregationInterval)
                    : def.AggregationInterval == TimeSpan.Zero
                        ? TimeSpan.Zero
                        : GetRecommendedAggregationInterval(duration);
                return new ChartPeriodPreset(def.Uid, def.Name, def.ShortName, duration, period, aggregationInterval);
            })
            .ToList();
    }

    /// <summary>
    /// Finds the matching preset index for chart period preset store.
    /// </summary>
    /// <param name="uid">Identifier of uid.</param>
    /// <param name="presets">Input value for presets.</param>
    /// <returns>The result of the operation.</returns>
    public static int FindMatchingPresetIndex(string? uid, IReadOnlyList<ChartPeriodPreset> presets) {
        if (string.IsNullOrWhiteSpace(uid)) {
            return -1;
        }

        var effectiveUid = uid;
        if (LegacyPresetUidAliases.TryGetValue(uid, out var replacementUid)) {
            effectiveUid = replacementUid;
        }

        for (int i = 0; i < presets.Count; i++) {
            if (string.Equals(presets[i].Uid, effectiveUid, StringComparison.Ordinal)) {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets the fallback preset for chart period preset store.
    /// </summary>
    /// <param name="presets">Input value for presets.</param>
    /// <returns>The result of the operation.</returns>
    public static ChartPeriodPreset GetFallbackPreset(IReadOnlyList<ChartPeriodPreset> presets) {
        if (presets.Count > 0) {
            return presets[0];
        }

        return new ChartPeriodPreset("24h", "24 hours", "24h", TimeSpan.FromDays(1), ChartPeriod.Last24Hours, TimeSpan.FromMinutes(15));
    }

    /// <summary>
    /// Formats the duration for chart period preset store.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <returns>The resulting string value.</returns>
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

    /// <summary>
    /// Maps the duration to period for chart period preset store.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Determines whether are durations close for chart period preset store.
    /// </summary>
    /// <param name="a">Input value for a.</param>
    /// <param name="b">Input value for b.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool AreDurationsClose(TimeSpan a, TimeSpan b) {
        return Math.Abs((a - b).TotalSeconds) < 0.5;
    }

    /// <summary>
    /// Determines whether is whole number for chart period preset store.
    /// </summary>
    /// <param name="value">Input value for value.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool IsWholeNumber(double value) {
        return Math.Abs(value - Math.Round(value)) < 0.0001;
    }

    /// <summary>
    /// Normalizes the presets for chart period preset store.
    /// </summary>
    /// <param name="presets">Collection of presets items used by the operation.</param>
    /// <returns>The result of the operation.</returns>
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
                    ShortName = string.IsNullOrWhiteSpace(preset.ShortName) ? preset.Name.Trim() : preset.ShortName.Trim(),
                    Value = preset.Value,
                    Unit = unit,
                    AggregationInterval = preset.AggregationInterval > TimeSpan.Zero
                        ? ChartAggregationHelper.NormalizeAggregationInterval(preset.AggregationInterval)
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

    /// <summary>
    /// Gets the recommended aggregation interval for chart period preset store.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Calculates the default aggregation interval for chart period preset store.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <param name="targetPointCount">Input value for target point count.</param>
    /// <param name="rawDataPointIntervalSeconds">Input value for raw data point interval seconds.</param>
    /// <returns>The result of the operation.</returns>
    private static TimeSpan CalculateDefaultAggregationInterval(TimeSpan duration, int targetPointCount, int rawDataPointIntervalSeconds) {
        var intervalSeconds = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds / targetPointCount));
        var rawInterval = TimeSpan.FromSeconds(rawDataPointIntervalSeconds);
        var generated = TimeSpan.FromSeconds(intervalSeconds);
        if (generated <= rawInterval) {
            return TimeSpan.Zero;
        }

        return ChartAggregationHelper.NormalizeAggregationInterval(generated);
    }
}
