using DataStorage.Models;
using SafetyMonitor.Services;
using System.Text.Json.Serialization;

namespace SafetyMonitor.Models;

/// <summary>
/// Represents dashboard and encapsulates its related behavior and state.
/// </summary>
public class Dashboard {

    #region Public Properties
    /// <summary>
    /// Gets or sets the columns for dashboard. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int Columns { get; set; } = 4;
    /// <summary>
    /// Gets or sets the created at for dashboard. Stores a timestamp used for ordering, filtering, or range calculations.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    /// <summary>
    /// Gets or sets the id for dashboard. Identifies the related entity and is used for lookups, linking, or persistence.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the is quick access for dashboard. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool IsQuickAccess { get; set; } = false;
    [JsonIgnore]
    /// <summary>
    /// Gets or sets the needs startup reset for dashboard. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool NeedsStartupReset { get; set; }
    /// <summary>
    /// Gets or sets the initial chart link mode for dashboard. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public DashboardChartLinkMode InitialChartLinkMode { get; set; } = DashboardChartLinkMode.Full;
    /// <summary>
    /// Gets or sets the used link groups for dashboard. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int UsedLinkGroups { get; set; } = ChartLinkGroupInfo.MaxUsedGroups;
    /// <summary>
    /// Gets or sets the link group period preset uids for dashboard. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public Dictionary<ChartLinkGroup, string> LinkGroupPeriodPresetUids { get; set; } = new();
    /// <summary>
    /// Gets or sets the modified at for dashboard. Stores a timestamp used for ordering, filtering, or range calculations.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    /// <summary>
    /// Gets or sets the name for dashboard. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Name { get; set; } = "New Dashboard";
    /// <summary>
    /// Gets or sets the rows for dashboard. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int Rows { get; set; } = 4;
    /// <summary>
    /// Gets or sets the sort order for dashboard. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int SortOrder { get; set; }
    /// <summary>
    /// Gets or sets the tiles for dashboard. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<TileConfig> Tiles { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

    public static Dashboard CreateDefault() => CreateDefaultSet().First();

    /// <summary>
    /// Creates the default set for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static List<Dashboard> CreateDefaultSet() {
        var dashboards = new List<Dashboard> {
            CreateOverviewDashboard(),
            CreateNowDashboard(),
            CreateNightDashboard(),
            CreateHistoryDashboard(),
            CreateObservatoryDashboard(),
            CreateMeteoDashboard(),
            CreateAstroDashboard()
        };

        for (int i = 0; i < dashboards.Count; i++) {
            dashboards[i].SortOrder = i;
        }

        return dashboards;
    }

    /// <summary>
    /// Determines whether can place tile for dashboard.
    /// </summary>
    /// <param name="tile">Input value for tile.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    public bool CanPlaceTile(TileConfig tile) {
        if (tile.Row < 0 || tile.Column < 0 || tile.Row + tile.RowSpan > Rows || tile.Column + tile.ColumnSpan > Columns) {
            return false;
        }
        foreach (var existing in Tiles) {
            if (existing.Id == tile.Id) {
                continue;
            }
            bool rowOverlap = tile.Row < existing.Row + existing.RowSpan && tile.Row + tile.RowSpan > existing.Row;
            bool colOverlap = tile.Column < existing.Column + existing.ColumnSpan && tile.Column + tile.ColumnSpan > existing.Column;
            if (rowOverlap && colOverlap) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets the link group period preset uid for dashboard.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <returns>The resulting string value.</returns>
    public string GetLinkGroupPeriodPresetUid(ChartLinkGroup group) {
        EnsureLinkGroupPeriodDefaults();
        return LinkGroupPeriodPresetUids[group];
    }

    /// <summary>
    /// Sets the link group period preset uid for dashboard.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <param name="periodPresetUid">Identifier of period preset.</param>
    public void SetLinkGroupPeriodPresetUid(ChartLinkGroup group, string periodPresetUid) {
        EnsureLinkGroupPeriodDefaults();
        LinkGroupPeriodPresetUids[group] = periodPresetUid;
    }

    /// <summary>
    /// Gets the link group period short name for dashboard.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <returns>The resulting string value.</returns>
    public string GetLinkGroupPeriodShortName(ChartLinkGroup group) {
        var presetUid = GetLinkGroupPeriodPresetUid(group);
        var preset = ChartPeriodPresetStore
            .GetPresetItems()
            .FirstOrDefault(item => string.Equals(item.Uid, presetUid, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(preset.ShortLabel)
            ? ChartPeriodPresetStore.GetFallbackPreset(ChartPeriodPresetStore.GetPresetItems()).ShortLabel
            : preset.ShortLabel;
    }

    /// <summary>
    /// Ensures the link group period defaults for dashboard.
    /// </summary>
    public void EnsureLinkGroupPeriodDefaults() {
        UsedLinkGroups = ChartLinkGroupInfo.NormalizeUsedGroups(UsedLinkGroups);
        var fallbackPresetUid = ChartPeriodPresetStore
            .GetFallbackPreset(ChartPeriodPresetStore.GetPresetItems())
            .Uid;

        foreach (var group in ChartLinkGroupInfo.All) {
            if (!LinkGroupPeriodPresetUids.ContainsKey(group) || string.IsNullOrWhiteSpace(LinkGroupPeriodPresetUids[group])) {
                LinkGroupPeriodPresetUids[group] = fallbackPresetUid;
            }
        }
    }

    public IReadOnlyList<ChartLinkGroup> GetAvailableLinkGroups() => ChartLinkGroupInfo.GetAvailable(UsedLinkGroups);

    /// <summary>
    /// Ensures the link group configuration for dashboard.
    /// </summary>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    public bool EnsureLinkGroupConfiguration() {
        var changed = false;
        var normalizedUsedGroups = ChartLinkGroupInfo.NormalizeUsedGroups(UsedLinkGroups);
        if (normalizedUsedGroups != UsedLinkGroups) {
            UsedLinkGroups = normalizedUsedGroups;
            changed = true;
        }

        foreach (var chart in Tiles.OfType<ChartTileConfig>()) {
            var normalizedGroup = ChartLinkGroupInfo.NormalizeGroup(chart.LinkGroup, UsedLinkGroups);
            if (chart.LinkGroup != normalizedGroup) {
                chart.LinkGroup = normalizedGroup;
                changed = true;
            }
        }

        if (UsedLinkGroups == 1 && InitialChartLinkMode == DashboardChartLinkMode.Grouped) {
            InitialChartLinkMode = DashboardChartLinkMode.Full;
            changed = true;
        }

        return changed;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the overview dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateOverviewDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("9ce8eb4f-08be-4cd8-a3f2-79f88e13779e"),
            Name = "Overview",
            CreatedAt = DateTimeOffset.Parse("2026-03-08T14:17:37.1499741+02:00").DateTime,
            ModifiedAt = DateTimeOffset.Parse("2026-03-08T15:24:04.6065641+02:00").DateTime,
            Rows = 4,
            Columns = 4,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Grouped,
            UsedLinkGroups = 2,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "24h",
                [ChartLinkGroup.Bravo] = "6h",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0, displayMode: ValueTileDisplayMode.TextOnly, showUnit: false, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("69f28ed6-f52f-4558-870e-559fcc4d0ba3")),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover", id: Guid.Parse("7cc485af-4c60-4d41-bce4-4795043b8b8b")),
            ValueTile("Rain Rate", MetricType.RainRate, 0, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Rain Rate", textColorSchemeName: "Rain Rate", valueSchemeName: "Rain Rate", id: Guid.Parse("4aec7f4d-f2ab-4eb6-bab6-fa078fb3009d")),
            ValueTile("Wind Speed", MetricType.WindSpeed, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Speed", textColorSchemeName: "Wind Speed", valueSchemeName: "Wind Speed", id: Guid.Parse("fe85b486-6e28-40eb-9c79-f8e33a521a06")),
            ChartTile("Temperature / Dew Point (24h)", 1, 0, 3, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: TimeSpan.FromMinutes(1),
                id: Guid.Parse("12e8273c-085c-4231-b9b1-2c5ce2a20be8"),
                series: [
                    Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 112, 67), "Temperature"),
                    Aggregation(MetricType.DewPoint, AggregationFunction.Average, Color.FromArgb(66, 165, 245), "Dew Point")
                ]),
            ChartTile("Humidity / Pressure (24h)", 1, 2, 2, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: TimeSpan.FromMinutes(1),
                id: Guid.Parse("66036e5f-ddd6-4aec-bae3-96a96be6b43e"),
                series: [
                    Aggregation(MetricType.Humidity, AggregationFunction.Average, Color.FromArgb(38, 166, 154), "Humidity"),
                    Aggregation(MetricType.Pressure, AggregationFunction.Average, Color.FromArgb(171, 71, 188), "Pressure")
                ]),
            ChartTile("Wind & Rain (6h)", 3, 2, 1, 2, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Bravo,
                periodPresetUid: "6h",
                customAggregationInterval: TimeSpan.FromSeconds(30),
                id: Guid.Parse("413cb6b5-ee15-40b7-9362-917fcf39d298"),
                series: [
                    Aggregation(MetricType.WindSpeed, AggregationFunction.Maximum, Color.FromArgb(255, 202, 40), "Wind Max"),
                    Aggregation(MetricType.RainRate, AggregationFunction.Maximum, Color.FromArgb(239, 83, 80), "Rain Max")
                ])
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the now dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateNowDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("586a05ad-74cc-4ae4-a321-55cd69b86817"),
            Name = "Now",
            Rows = 3,
            Columns = 5,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Full,
            UsedLinkGroups = 1,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "15m",
                [ChartLinkGroup.Bravo] = "15m",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0, displayMode: ValueTileDisplayMode.TextOnly, showUnit: false, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("d7776357-edb5-4647-9d1c-39a2de4ea87a")),
            ValueTile("Temperature", MetricType.Temperature, 0, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Temperature", textColorSchemeName: "Temperature", valueSchemeName: "Temperature", id: Guid.Parse("581511d7-15fb-47c7-9ce0-fe1cfca9239b")),
            ValueTile("Humidity", MetricType.Humidity, 0, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Humidity", textColorSchemeName: "Humidity", valueSchemeName: "Humidity", id: Guid.Parse("29ca29b5-bfa1-4fb8-b098-58d82f18add5")),
            ValueTile("Pressure", MetricType.Pressure, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Pressure", textColorSchemeName: "Pressure", valueSchemeName: "Pressure", id: Guid.Parse("82127bef-2af4-4b8e-9aeb-9047b47316ed")),
            ValueTile("Dew Point", MetricType.DewPoint, 0, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("0ff58579-bb06-4621-a664-e070e5ef9b0f")),
            ValueTile("Cloud Cover", MetricType.CloudCover, 1, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover", id: Guid.Parse("71123269-a775-4a6d-8a7c-7c45fa095b30")),
            ValueTile("Sky Temp", MetricType.SkyTemperature, 1, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("ddb58772-5602-4a6a-9e51-eb136fdb9561")),
            ValueTile("Sky Brightness", MetricType.SkyBrightness, 1, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Sky Brightness", textColorSchemeName: "Sky Brightness", valueSchemeName: "Sky Brightness", id: Guid.Parse("0dc0e13f-9593-4520-b25a-7fbbaf5f9fb3")),
            ValueTile("Sky Quality", MetricType.SkyQuality, 1, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Sky Quality", textColorSchemeName: "Sky Quality", valueSchemeName: "Sky Quality", id: Guid.Parse("818aeb90-a085-47bb-9fb9-2b7a7cca9ac1")),
            ValueTile("Rain Rate", MetricType.RainRate, 1, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Rain Rate", textColorSchemeName: "Rain Rate", valueSchemeName: "Rain Rate", id: Guid.Parse("2eee88ae-3fae-4073-a0aa-ee9ca0c6c05c")),
            ValueTile("Wind Speed", MetricType.WindSpeed, 2, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Speed", textColorSchemeName: "Wind Speed", valueSchemeName: "Wind Speed", id: Guid.Parse("f150fff7-c170-4904-b635-5c45eda83440")),
            ValueTile("Wind Gust", MetricType.WindGust, 2, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Gust", textColorSchemeName: "Wind Gust", valueSchemeName: "Wind Gust", id: Guid.Parse("5b0e0e19-23b3-43c5-8a38-3d443e7d75d7")),
            ValueTile("Wind Direction", MetricType.WindDirection, 2, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "Wind Direction", id: Guid.Parse("455f430a-bf69-43bb-ba92-3545786992c3")),
            ValueTile("Star FWHM", MetricType.StarFwhm, 2, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("bca06f9c-1b15-41c7-b8a6-81e1c76c31a2"))
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the night dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateNightDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("d03e4506-d5d5-4be6-98d9-2b4ce6d5f4e5"),
            Name = "Night",
            CreatedAt = DateTimeOffset.Parse("2026-03-08T15:44:47.9411842+02:00").DateTime,
            ModifiedAt = DateTimeOffset.Parse("2026-03-08T15:47:48.4608613+02:00").DateTime,
            Rows = 4,
            Columns = 4,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Grouped,
            UsedLinkGroups = 2,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "6h",
                [ChartLinkGroup.Bravo] = "24h",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0, displayMode: ValueTileDisplayMode.TextOnly, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("938476cd-c1d7-4e56-b69f-722df2129c56")),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover", id: Guid.Parse("b6248670-6247-4db1-aba6-80e9e0e23970")),
            ValueTile("Sky Quality", MetricType.SkyQuality, 0, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Sky Quality", textColorSchemeName: "Sky Quality", valueSchemeName: "Sky Quality", id: Guid.Parse("a7f7bcb2-117b-4648-86b9-17eb63a1e941")),
            ValueTile("Sky Brightness", MetricType.SkyBrightness, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Sky Brightness", textColorSchemeName: "Sky Brightness", valueSchemeName: "Sky Brightness", id: Guid.Parse("67cd85d8-5b60-4629-b73e-5e4a621d63d0")),
            ChartTile("Sky Transparency (6h)", 1, 0, 2, 2, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "6h",
                customAggregationInterval: null,
                id: Guid.Parse("4bd4f8f4-a8e5-4c57-90bb-bc858e9bb012"),
                series: [
                Aggregation(MetricType.SkyTemperature, AggregationFunction.Average, Color.FromArgb(41, 182, 246), "Sky Temp"),
                Aggregation(MetricType.CloudCover, AggregationFunction.Maximum, Color.FromArgb(120, 144, 156), "Cloud Max")]),
            ChartTile("Seeing & Darkness (6h)", 1, 2, 2, 2, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "6h",
                customAggregationInterval: null,
                id: Guid.Parse("4107c63e-0dcf-4747-9901-07b4c50a4ecb"),
                series: [
                Aggregation(MetricType.StarFwhm, AggregationFunction.Average, Color.FromArgb(255, 167, 38), "FWHM Avg"),
                Aggregation(MetricType.SkyQuality, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "SQM Avg")]),
            ChartTile("Safety Trend (24h)", 3, 0, 1, 4, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Bravo,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("fd55cf2d-a854-4842-a015-d70f1c5b54bc"),
                series: [
                Aggregation(MetricType.IsSafe, AggregationFunction.Average, Color.FromArgb(129, 199, 132), "Safety Avg"),
                Aggregation(MetricType.IsSafe, AggregationFunction.Minimum, Color.FromArgb(239, 83, 80), "Safety Min")])
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the history dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateHistoryDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("e0c3109e-3537-4372-8c78-2efc9f8504b1"),
            Name = "History",
            Rows = 5,
            Columns = 4,
            CreatedAt = DateTimeOffset.Parse("2026-03-08T16:11:39.7907822+02:00").DateTime,
            ModifiedAt = DateTimeOffset.Parse("2026-03-08T16:14:32.5188132+02:00").DateTime,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Grouped,
            UsedLinkGroups = 2,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "30d",
                [ChartLinkGroup.Bravo] = "7d",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ChartTile("Temperature Min / Avg / Max (30d)", 0, 0, 2, 2, ChartPeriod.Last30Days,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "30d",
                customAggregationInterval: null,
                id: Guid.Parse("4f9f2888-e9c3-4d22-8e57-4a8b0131d06c"),
                series: [
                Aggregation(MetricType.Temperature, AggregationFunction.Minimum, Color.FromArgb(66, 165, 245), "Temp Min"),
                Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 167, 38), "Temp Avg"),
                Aggregation(MetricType.Temperature, AggregationFunction.Maximum, Color.FromArgb(239, 83, 80), "Temp Max")]),
            ChartTile("Humidity Min / Avg / Max (30d)", 0, 2, 2, 2, ChartPeriod.Last30Days,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "30d",
                customAggregationInterval: null,
                id: Guid.Parse("3eeb6a06-d681-4fc9-ba6a-02d773168336"),
                series: [
                Aggregation(MetricType.Humidity, AggregationFunction.Minimum, Color.FromArgb(38, 198, 218), "Hum Min"),
                Aggregation(MetricType.Humidity, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "Hum Avg"),
                Aggregation(MetricType.Humidity, AggregationFunction.Maximum, Color.FromArgb(156, 204, 101), "Hum Max")]),
            ChartTile("Pressure Min / Avg / Max (30d)", 2, 0, 2, 2, ChartPeriod.Last30Days,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "30d",
                customAggregationInterval: null,
                id: Guid.Parse("76d4e3fa-77c4-4c96-9360-9f2483a27417"),
                series: [
                Aggregation(MetricType.Pressure, AggregationFunction.Minimum, Color.FromArgb(126, 87, 194), "Pressure Min"),
                Aggregation(MetricType.Pressure, AggregationFunction.Average, Color.FromArgb(171, 71, 188), "Pressure Avg"),
                Aggregation(MetricType.Pressure, AggregationFunction.Maximum, Color.FromArgb(236, 64, 122), "Pressure Max")]),
            ChartTile("Wind Speed Avg / Gust Max (30d)", 2, 2, 2, 2, ChartPeriod.Last30Days,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "30d",
                customAggregationInterval: null,
                id: Guid.Parse("d5050b2e-0be9-4326-bcc6-a63fef23d4fd"),
                series: [
                Aggregation(MetricType.WindSpeed, AggregationFunction.Average, Color.FromArgb(255, 202, 40), "Wind Avg"),
                Aggregation(MetricType.WindGust, AggregationFunction.Maximum, Color.FromArgb(255, 112, 67), "Gust Max")]),
            ChartTile("Safety Reliability (7d)", 4, 0, 1, 4, ChartPeriod.Last30Days,
                linkGroup: ChartLinkGroup.Bravo,
                periodPresetUid: "7d",
                customAggregationInterval: null,
                id: Guid.Parse("7965e641-ad08-4065-aaed-ddd6d465872c"),
                series: [
                Aggregation(MetricType.IsSafe, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "Safety Avg"),
                Aggregation(MetricType.IsSafe, AggregationFunction.Minimum, Color.FromArgb(239, 83, 80), "Safety Min")])
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the observatory dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateObservatoryDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("3958ac73-15ca-4bf3-8a6a-467fe3b3bb4f"),
            Name = "Observatory",
            CreatedAt = DateTimeOffset.Parse("2026-03-08T16:35:56.5899676+02:00").DateTime,
            ModifiedAt = DateTimeOffset.Parse("2026-03-08T16:38:06.2665768+02:00").DateTime,
            Rows = 4,
            Columns = 5,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Full,
            UsedLinkGroups = 1,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "24h",
                [ChartLinkGroup.Bravo] = "15m",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ValueTile("Rain Rate", MetricType.RainRate, 0, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Rain Rate", textColorSchemeName: "Rain Rate", valueSchemeName: "Rain Rate", id: Guid.Parse("cb448e4f-c590-417f-8986-e138c3678efe")),
            ValueTile("Wind Gust", MetricType.WindGust, 0, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Gust", textColorSchemeName: "Wind Gust", valueSchemeName: "Wind Gust", id: Guid.Parse("30edb5af-0a39-4a04-9071-b1221114d9c9")),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover", id: Guid.Parse("80f51d1a-09c9-4967-b57b-0f75c0c73981")),
            ValueTile("Safety", MetricType.IsSafe, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("c735013d-4f94-4640-8e09-85d39c1eaf00")),
            ValueTile("Sky Temp", MetricType.SkyTemperature, 0, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("2673f545-1efd-4ffb-b983-45778f8094f9")),
            ChartTile("Rain & Clouds (24h)", 1, 0, 3, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("7ce7cba7-1951-48d3-9d1b-46ac3f34aa01"),
                series: [
                    Aggregation(MetricType.RainRate, AggregationFunction.Maximum, Color.FromArgb(66, 165, 245), "Rain Max"),
                    Aggregation(MetricType.CloudCover, AggregationFunction.Average, Color.FromArgb(120, 144, 156), "Cloud Avg")
                ]),
            ChartTile("Wind Stability (24h)", 1, 2, 3, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("845f4b2f-b8cc-46c7-82c0-411ad5a66dec"),
                series: [
                    Aggregation(MetricType.WindSpeed, AggregationFunction.Average, Color.FromArgb(255, 202, 40), "Wind Avg"),
                    Aggregation(MetricType.WindGust, AggregationFunction.Maximum, Color.FromArgb(255, 112, 67), "Gust Max")
                ]),
            ChartTile("Thermal Delta: Ambient vs Sky (24h)", 1, 4, 3, 1, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("412c3e7b-8659-4c09-963d-d39263717535"),
                series: [
                    Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 138, 101), "Ambient"),
                    Aggregation(MetricType.SkyTemperature, AggregationFunction.Average, Color.FromArgb(41, 182, 246), "Sky")
                ])
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the meteo dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateMeteoDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("0d4214f8-fe27-43b8-971f-7d6adefa2a4f"),
            Name = "Meteo",
            CreatedAt = DateTimeOffset.Parse("2026-03-08T19:49:58.3926485+02:00").DateTime,
            ModifiedAt = DateTimeOffset.Parse("2026-03-08T20:28:36.991527+02:00").DateTime,
            Rows = 4,
            Columns = 5,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Grouped,
            UsedLinkGroups = 2,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "24h",
                [ChartLinkGroup.Bravo] = "6h",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0, displayMode: ValueTileDisplayMode.TextOnly, showUnit: false, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("f30fcd6d-4bd9-4695-be95-39002674a76d")),
            ValueTile("Temp", MetricType.Temperature, 1, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Temperature", textColorSchemeName: "Temperature", valueSchemeName: "Temperature", id: Guid.Parse("121b59cc-a45d-4040-8767-c7ef01f9629b")),
            ValueTile("Humidity", MetricType.Humidity, 2, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Humidity", textColorSchemeName: "Humidity", valueSchemeName: "Humidity", id: Guid.Parse("9f9b2ce9-ce57-4f4e-a7ce-94001a2f3532")),
            ValueTile("Pressure", MetricType.Pressure, 3, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Pressure", textColorSchemeName: "Pressure", valueSchemeName: "Pressure", id: Guid.Parse("cf3be9db-b7d2-4f89-be0d-fd100e98f3c9")),
            ChartTile("Thermal Profile (24h)", 0, 1, 2, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("c9f3f18c-eed2-4eb2-8eb5-8d47e27595cc"),
                series: [
                    Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 138, 101), "Temp Avg"),
                    Aggregation(MetricType.DewPoint, AggregationFunction.Average, Color.FromArgb(66, 165, 245), "Dew Avg")
                ]),
            ChartTile("Moisture & Pressure (24h)", 0, 3, 2, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("67de6ed6-f400-416f-b83f-befd547f267a"),
                series: [
                    Aggregation(MetricType.Humidity, AggregationFunction.Average, Color.FromArgb(38, 166, 154), "Humidity Avg"),
                    Aggregation(MetricType.Pressure, AggregationFunction.Average, Color.FromArgb(126, 87, 194), "Pressure Avg")
                ]),
            ChartTile("Wind & Rain (6h)", 2, 1, 2, 4, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Bravo,
                periodPresetUid: "6h",
                customAggregationInterval: null,
                id: Guid.Parse("67bba5c8-cef8-4421-af8f-b6f5cfec2e0b"),
                series: [
                    Aggregation(MetricType.WindSpeed, AggregationFunction.Maximum, Color.FromArgb(255, 202, 40), "Wind Max"),
                    Aggregation(MetricType.RainRate, AggregationFunction.Maximum, Color.FromArgb(66, 165, 245), "Rain Max")
                ])
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the astro dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateAstroDashboard() {
        var dashboard = new Dashboard {
            Id = Guid.Parse("8538274f-59db-4514-b7bf-28e0d104b84f"),
            Name = "Astro",
            Rows = 4,
            Columns = 5,
            CreatedAt = DateTimeOffset.Parse("2026-03-08T19:49:58.3936651+02:00").DateTime,
            ModifiedAt = DateTimeOffset.Parse("2026-03-08T20:32:00.5558639+02:00").DateTime,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Grouped,
            UsedLinkGroups = 2,
            LinkGroupPeriodPresetUids = new Dictionary<ChartLinkGroup, string> {
                [ChartLinkGroup.Alpha] = "6h",
                [ChartLinkGroup.Bravo] = "24h",
                [ChartLinkGroup.Charlie] = "15m",
                [ChartLinkGroup.Delta] = "15m",
                [ChartLinkGroup.Echo] = "15m",
                [ChartLinkGroup.Foxtrot] = "15m"
            }
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 2, displayMode: ValueTileDisplayMode.TextOnly, showUnit: false, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("7217ec3c-1c48-4b0a-80fc-c92f8648fab4")),
            ValueTile("SQM", MetricType.SkyQuality, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Sky Quality", textColorSchemeName: "Sky Quality", valueSchemeName: "Sky Quality", id: Guid.Parse("ec0cecb6-43fa-4f9f-a98f-a82572bcc79a")),
            ValueTile("SkyTemp", MetricType.SkyTemperature, 0, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, id: Guid.Parse("ae4787f3-df60-44bd-b15b-27e620df2e95")),
            ValueTile("Cloud", MetricType.CloudCover, 1, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover", id: Guid.Parse("31595ad8-92dd-43fe-9eca-2d2fb1082f8f")),
            ValueTile("Seeing", MetricType.StarFwhm, 2, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, id: Guid.Parse("df97adfe-97b0-44df-8ab4-f702a0f26a58")),
            ValueTile("Wind", MetricType.WindSpeed, 3, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, showTopValueGradient: false, decimalPlaces: 1, colorSchemeName: "Wind Speed", textColorSchemeName: "Wind Speed", valueSchemeName: "Wind Speed", id: Guid.Parse("6505a429-f72a-4cf8-9ce0-9780f4ca9ec3")),
            ChartTile("Transparency (6h)", 0, 0, 2, 2, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "6h",
                customAggregationInterval: null,
                id: Guid.Parse("f9a65eb0-b8b2-43f5-8667-3faebd5e58c9"),
                series: [
                    Aggregation(MetricType.SkyTemperature, AggregationFunction.Average, Color.FromArgb(41, 182, 246), "Sky Temp"),
                    Aggregation(MetricType.CloudCover, AggregationFunction.Maximum, Color.FromArgb(120, 144, 156), "Cloud Max")
                ]),
            ChartTile("Darkness & Seeing (24h)", 1, 2, 3, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Bravo,
                periodPresetUid: "24h",
                customAggregationInterval: null,
                id: Guid.Parse("7da3af37-a9eb-4108-969d-2d5bd7b2105e"),
                series: [
                    Aggregation(MetricType.SkyQuality, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "SQM Avg"),
                    Aggregation(MetricType.StarFwhm, AggregationFunction.Average, Color.FromArgb(255, 167, 38), "FWHM Avg")
                ]),
            ChartTile("SkyGlow Pulse (6h)", 2, 0, 2, 2, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "6h",
                customAggregationInterval: null,
                id: Guid.Parse("47db5e09-29af-446f-bca4-80a5f6eddb56"),
                series: [
                    Aggregation(MetricType.SkyBrightness, AggregationFunction.Maximum, Color.FromArgb(66, 165, 245), "Sky Max"),
                    Aggregation(MetricType.IsSafe, AggregationFunction.Minimum, Color.FromArgb(239, 83, 80), "Safety Min")
                ])
        ]);

        return dashboard;
    }

    /// <summary>
    /// Executes value tile as part of dashboard processing.
    /// </summary>
    /// <param name="title">Input value for title.</param>
    /// <param name="metric">Input value for metric.</param>
    /// <param name="row">Input value for row.</param>
    /// <param name="column">Input value for column.</param>
    /// <param name="displayMode">Input value for display mode.</param>
    /// <param name="showUnit">Input value for show unit.</param>
    /// <param name="showIcon">Input value for show icon.</param>
    /// <param name="decimalPlaces">Input value for decimal places.</param>
    /// <param name="iconColorSchemeName">Input value for icon color scheme name.</param>
    /// <param name="colorSchemeName">Input value for color scheme name.</param>
    /// <param name="textColorSchemeName">Input value for text color scheme name.</param>
    /// <param name="valueSchemeName">Input value for value scheme name.</param>
    /// <param name="id">Identifier of id.</param>
    /// <returns>The result of the operation.</returns>
    private static ValueTileConfig ValueTile(string title, MetricType metric, int row, int column, ValueTileDisplayMode displayMode = ValueTileDisplayMode.TextAndValue, bool showUnit = true, bool showIcon = true, bool showTopValueGradient = false, int decimalPlaces = 1, string? iconColorSchemeName = null, string? colorSchemeName = null, string? textColorSchemeName = null, string? valueSchemeName = null, Guid? id = null) {
        return new ValueTileConfig {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Metric = metric,
            Row = row,
            Column = column,
            DisplayMode = displayMode,
            ShowUnit = showUnit,
            ShowIcon = showIcon,
            ShowTopValueGradient = showTopValueGradient,
            DecimalPlaces = decimalPlaces,
            IconColorSchemeName = iconColorSchemeName ?? string.Empty,
            ColorSchemeName = colorSchemeName ?? ColorSchemeService.GetDefaultSchemeName(metric),
            TextColorSchemeName = textColorSchemeName ?? ColorSchemeService.GetDefaultSchemeName(metric),
            ValueSchemeName = valueSchemeName ?? ValueSchemeService.GetDefaultSchemeName(metric)
        };
    }

    /// <summary>
    /// Executes chart tile as part of dashboard processing.
    /// </summary>
    /// <param name="title">Input value for title.</param>
    /// <param name="row">Input value for row.</param>
    /// <param name="column">Input value for column.</param>
    /// <param name="rowSpan">Input value for row span.</param>
    /// <param name="columnSpan">Input value for column span.</param>
    /// <param name="period">Input value for period.</param>
    /// <param name="series">Input value for series.</param>
    /// <returns>The result of the operation.</returns>
    private static ChartTileConfig ChartTile(string title, int row, int column, int rowSpan, int columnSpan, ChartPeriod period, params MetricAggregation[] series) {
        return ChartTile(title, row, column, rowSpan, columnSpan, period, ChartLinkGroup.Alpha, string.Empty, null, null, series);
    }

    private static ChartTileConfig ChartTile(
        string title,
        int row,
        int column,
        int rowSpan,
        int columnSpan,
        ChartPeriod period,
        ChartLinkGroup linkGroup,
        string periodPresetUid,
        TimeSpan? customAggregationInterval,
        Guid? id,
        MetricAggregation[] series) {
        return new ChartTileConfig {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Row = row,
            Column = column,
            RowSpan = rowSpan,
            ColumnSpan = columnSpan,
            Period = period,
            LinkGroup = linkGroup,
            PeriodPresetUid = periodPresetUid,
            CustomAggregationInterval = customAggregationInterval,
            ShowInspector = true,
            MetricAggregations = [.. series]
        };
    }

    /// <summary>
    /// Executes aggregation as part of dashboard processing.
    /// </summary>
    /// <param name="metric">Input value for metric.</param>
    /// <param name="function">Input value for function.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="label">Input value for label.</param>
    /// <returns>The result of the operation.</returns>
    private static MetricAggregation Aggregation(MetricType metric, AggregationFunction function, Color color, string label) {
        return new MetricAggregation {
            Metric = metric,
            Function = function,
            Color = color,
            DarkThemeColor = CreateDarkThemeSeriesColor(color),
            Label = label,
            Smooth = metric != MetricType.IsSafe
        };
    }

    /// <summary>
    /// Creates the dark theme series color for dashboard.
    /// </summary>
    /// <param name="lightThemeColor">Input value for light theme color.</param>
    /// <returns>The result of the operation.</returns>
    private static Color CreateDarkThemeSeriesColor(Color lightThemeColor) {
        var hue = lightThemeColor.GetHue();
        var saturation = lightThemeColor.GetSaturation();
        var brightness = lightThemeColor.GetBrightness();

        const double darkThemeValue = 0.82;
        var adjustedBrightness = Math.Max(brightness, darkThemeValue);
        return ColorFromHsv(hue, saturation, adjustedBrightness);
    }

    /// <summary>
    /// Executes color from hsv as part of dashboard processing.
    /// </summary>
    /// <param name="hue">Input value for hue.</param>
    /// <param name="saturation">Input value for saturation.</param>
    /// <param name="value">Input value for value.</param>
    /// <returns>The result of the operation.</returns>
    private static Color ColorFromHsv(double hue, double saturation, double value) {
        var h = (hue % 360 + 360) % 360;
        var c = value * saturation;
        var x = c * (1 - Math.Abs((h / 60.0 % 2) - 1));
        var m = value - c;

        (double r, double g, double b) = h switch {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x)
        };

        return Color.FromArgb(
            255,
            (int)Math.Round((r + m) * 255),
            (int)Math.Round((g + m) * 255),
            (int)Math.Round((b + m) * 255));
    }

    #endregion Private Methods
}
