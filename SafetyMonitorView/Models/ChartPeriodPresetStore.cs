namespace SafetyMonitorView.Models;

public readonly record struct ChartPeriodPreset(string Label, TimeSpan Duration, ChartPeriod Period);

public static class ChartPeriodPresetStore {

    private static List<ChartPeriodPresetDefinition> _presets = CreateDefaultPresets();

    public static event Action? PresetsChanged;

    public static IReadOnlyList<ChartPeriodPresetDefinition> PresetDefinitions => _presets;

    public static List<ChartPeriodPresetDefinition> CreateDefaultPresets() => [
        new ChartPeriodPresetDefinition { Name = "15 Minutes", Value = 15, Unit = ChartPeriodUnit.Minutes },
        new ChartPeriodPresetDefinition { Name = "1 Hour", Value = 1, Unit = ChartPeriodUnit.Hours },
        new ChartPeriodPresetDefinition { Name = "6 Hours", Value = 6, Unit = ChartPeriodUnit.Hours },
        new ChartPeriodPresetDefinition { Name = "24 Hours", Value = 24, Unit = ChartPeriodUnit.Hours },
        new ChartPeriodPresetDefinition { Name = "7 Days", Value = 7, Unit = ChartPeriodUnit.Days },
        new ChartPeriodPresetDefinition { Name = "30 Days", Value = 30, Unit = ChartPeriodUnit.Days }
    ];

    public static void SetPresets(IEnumerable<ChartPeriodPresetDefinition>? presets) {
        _presets = NormalizePresets(presets);
        PresetsChanged?.Invoke();
    }

    public static IReadOnlyList<ChartPeriodPreset> GetPresetItems() {
        return _presets
            .Select(def => {
                var duration = def.ToTimeSpan();
                var period = MapDurationToPeriod(duration);
                return new ChartPeriodPreset(def.Name, duration, period);
            })
            .ToList();
    }

    public static int FindMatchingPresetIndex(TimeSpan? duration, ChartPeriod period, IReadOnlyList<ChartPeriodPreset> presets) {
        if (period != ChartPeriod.Custom) {
            for (int i = 0; i < presets.Count; i++) {
                if (presets[i].Period == period) {
                    return i;
                }
            }
        }

        if (duration.HasValue) {
            for (int i = 0; i < presets.Count; i++) {
                if (AreDurationsClose(presets[i].Duration, duration.Value)) {
                    return i;
                }
            }
        }

        return -1;
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
        if (presets != null) {
            foreach (var preset in presets) {
                if (preset == null || string.IsNullOrWhiteSpace(preset.Name) || preset.Value <= 0) {
                    continue;
                }
                var unit = Enum.IsDefined(typeof(ChartPeriodUnit), preset.Unit)
                    ? preset.Unit
                    : ChartPeriodUnit.Hours;
                list.Add(new ChartPeriodPresetDefinition {
                    Name = preset.Name.Trim(),
                    Value = preset.Value,
                    Unit = unit
                });
            }
        }

        if (list.Count == 0) {
            list = CreateDefaultPresets();
        }

        return list;
    }
}
