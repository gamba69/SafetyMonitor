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
            CreateObservatoryDashboard()
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
            Name = "Overview",
            Rows = 4,
            Columns = 4,
            IsQuickAccess = true,
            InitialChartLinkMode = DashboardChartLinkMode.Grouped,
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
            ValueTile("Safety", MetricType.IsSafe, 0, 0, displayMode: ValueTileDisplayMode.TextOnly, showUnit: false, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety"),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover"),
            ValueTile("Rain Rate", MetricType.RainRate, 0, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Rain Rate", textColorSchemeName: "Rain Rate", valueSchemeName: "Rain Rate"),
            ValueTile("Wind Speed", MetricType.WindSpeed, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Speed", textColorSchemeName: "Wind Speed", valueSchemeName: "Wind Speed"),
            ChartTile("Temperature / Dew Point (24h)", 1, 0, 3, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: TimeSpan.FromMinutes(1),
                series: [
                    Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 112, 67), "Temperature"),
                    Aggregation(MetricType.DewPoint, AggregationFunction.Average, Color.FromArgb(66, 165, 245), "Dew Point")
                ]),
            ChartTile("Humidity / Pressure (24h)", 1, 2, 2, 2, ChartPeriod.Last24Hours,
                linkGroup: ChartLinkGroup.Alpha,
                periodPresetUid: "24h",
                customAggregationInterval: TimeSpan.FromMinutes(1),
                series: [
                    Aggregation(MetricType.Humidity, AggregationFunction.Average, Color.FromArgb(38, 166, 154), "Humidity"),
                    Aggregation(MetricType.Pressure, AggregationFunction.Average, Color.FromArgb(171, 71, 188), "Pressure")
                ]),
            ChartTile("Wind & Rain (6h)", 3, 2, 1, 2, ChartPeriod.Last6Hours,
                linkGroup: ChartLinkGroup.Bravo,
                periodPresetUid: "6h",
                customAggregationInterval: TimeSpan.FromSeconds(30),
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
            UsedLinkGroups = 6,
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
            ValueTile("Safety", MetricType.IsSafe, 0, 0, displayMode: ValueTileDisplayMode.TextOnly, showUnit: false, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Safety", textColorSchemeName: "Safety", valueSchemeName: "Safety", id: Guid.Parse("d7776357-edb5-4647-9d1c-39a2de4ea87a")),
            ValueTile("Temperature", MetricType.Temperature, 0, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Temperature", textColorSchemeName: "Temperature", valueSchemeName: "Temperature", id: Guid.Parse("581511d7-15fb-47c7-9ce0-fe1cfca9239b")),
            ValueTile("Humidity", MetricType.Humidity, 0, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Humidity", textColorSchemeName: "Humidity", valueSchemeName: "Humidity", id: Guid.Parse("29ca29b5-bfa1-4fb8-b098-58d82f18add5")),
            ValueTile("Pressure", MetricType.Pressure, 0, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Pressure", textColorSchemeName: "Pressure", valueSchemeName: "Pressure", id: Guid.Parse("82127bef-2af4-4b8e-9aeb-9047b47316ed")),
            ValueTile("Dew Point", MetricType.DewPoint, 0, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("0ff58579-bb06-4621-a664-e070e5ef9b0f")),
            ValueTile("Cloud Cover", MetricType.CloudCover, 1, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Cloud Cover", textColorSchemeName: "Cloud Cover", valueSchemeName: "Cloud Cover", id: Guid.Parse("71123269-a775-4a6d-8a7c-7c45fa095b30")),
            ValueTile("Sky Temp", MetricType.SkyTemperature, 1, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("ddb58772-5602-4a6a-9e51-eb136fdb9561")),
            ValueTile("Sky Brightness", MetricType.SkyBrightness, 1, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Sky Brightness", textColorSchemeName: "Sky Brightness", valueSchemeName: "Sky Brightness", id: Guid.Parse("0dc0e13f-9593-4520-b25a-7fbbaf5f9fb3")),
            ValueTile("Sky Quality", MetricType.SkyQuality, 1, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Sky Quality", textColorSchemeName: "Sky Quality", valueSchemeName: "Sky Quality", id: Guid.Parse("818aeb90-a085-47bb-9fb9-2b7a7cca9ac1")),
            ValueTile("Rain Rate", MetricType.RainRate, 1, 4, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Rain Rate", textColorSchemeName: "Rain Rate", valueSchemeName: "Rain Rate", id: Guid.Parse("2eee88ae-3fae-4073-a0aa-ee9ca0c6c05c")),
            ValueTile("Wind Speed", MetricType.WindSpeed, 2, 0, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Speed", textColorSchemeName: "Wind Speed", valueSchemeName: "Wind Speed", id: Guid.Parse("f150fff7-c170-4904-b635-5c45eda83440")),
            ValueTile("Wind Gust", MetricType.WindGust, 2, 1, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "Wind Gust", textColorSchemeName: "Wind Gust", valueSchemeName: "Wind Gust", id: Guid.Parse("5b0e0e19-23b3-43c5-8a38-3d443e7d75d7")),
            ValueTile("Wind Direction", MetricType.WindDirection, 2, 2, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: null, id: Guid.Parse("455f430a-bf69-43bb-ba92-3545786992c3")),
            ValueTile("Star FWHM", MetricType.StarFwhm, 2, 3, displayMode: ValueTileDisplayMode.TextAndValue, showUnit: true, showIcon: true, decimalPlaces: 1, iconColorSchemeName: "", colorSchemeName: "", textColorSchemeName: "", valueSchemeName: "", id: Guid.Parse("bca06f9c-1b15-41c7-b8a6-81e1c76c31a2"))
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the night dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateNightDashboard() {
        var dashboard = new Dashboard {
            Name = "Night Watch",
            Rows = 4,
            Columns = 4
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 1),
            ValueTile("Sky Quality", MetricType.SkyQuality, 0, 2),
            ValueTile("Sky Brightness", MetricType.SkyBrightness, 0, 3),
            ChartTile("Sky Transparency (6h)", 1, 0, 2, 2, ChartPeriod.Last6Hours,
                Aggregation(MetricType.SkyTemperature, AggregationFunction.Average, Color.FromArgb(41, 182, 246), "Sky Temp"),
                Aggregation(MetricType.CloudCover, AggregationFunction.Maximum, Color.FromArgb(120, 144, 156), "Cloud Max")),
            ChartTile("Seeing & Darkness (6h)", 1, 2, 2, 2, ChartPeriod.Last6Hours,
                Aggregation(MetricType.StarFwhm, AggregationFunction.Average, Color.FromArgb(255, 167, 38), "FWHM Avg"),
                Aggregation(MetricType.SkyQuality, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "SQM Avg")),
            ChartTile("Safety Trend (24h)", 3, 0, 1, 4, ChartPeriod.Last24Hours,
                Aggregation(MetricType.IsSafe, AggregationFunction.Average, Color.FromArgb(129, 199, 132), "Safety Avg"),
                Aggregation(MetricType.IsSafe, AggregationFunction.Minimum, Color.FromArgb(239, 83, 80), "Safety Min"))
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the history dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateHistoryDashboard() {
        var dashboard = new Dashboard {
            Name = "History",
            Rows = 5,
            Columns = 4
        };

        dashboard.Tiles.AddRange([
            ChartTile("Temperature Min / Avg / Max (30d)", 0, 0, 2, 2, ChartPeriod.Last30Days,
                Aggregation(MetricType.Temperature, AggregationFunction.Minimum, Color.FromArgb(66, 165, 245), "Temp Min"),
                Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 167, 38), "Temp Avg"),
                Aggregation(MetricType.Temperature, AggregationFunction.Maximum, Color.FromArgb(239, 83, 80), "Temp Max")),
            ChartTile("Humidity Min / Avg / Max (30d)", 0, 2, 2, 2, ChartPeriod.Last30Days,
                Aggregation(MetricType.Humidity, AggregationFunction.Minimum, Color.FromArgb(38, 198, 218), "Hum Min"),
                Aggregation(MetricType.Humidity, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "Hum Avg"),
                Aggregation(MetricType.Humidity, AggregationFunction.Maximum, Color.FromArgb(156, 204, 101), "Hum Max")),
            ChartTile("Pressure Min / Avg / Max (30d)", 2, 0, 2, 2, ChartPeriod.Last30Days,
                Aggregation(MetricType.Pressure, AggregationFunction.Minimum, Color.FromArgb(126, 87, 194), "Pressure Min"),
                Aggregation(MetricType.Pressure, AggregationFunction.Average, Color.FromArgb(171, 71, 188), "Pressure Avg"),
                Aggregation(MetricType.Pressure, AggregationFunction.Maximum, Color.FromArgb(236, 64, 122), "Pressure Max")),
            ChartTile("Wind Speed Avg / Gust Max (30d)", 2, 2, 2, 2, ChartPeriod.Last30Days,
                Aggregation(MetricType.WindSpeed, AggregationFunction.Average, Color.FromArgb(255, 202, 40), "Wind Avg"),
                Aggregation(MetricType.WindGust, AggregationFunction.Maximum, Color.FromArgb(255, 112, 67), "Gust Max")),
            ChartTile("Safety Reliability (7d)", 4, 0, 1, 4, ChartPeriod.Last7Days,
                Aggregation(MetricType.IsSafe, AggregationFunction.Average, Color.FromArgb(102, 187, 106), "Safety Avg"),
                Aggregation(MetricType.IsSafe, AggregationFunction.Minimum, Color.FromArgb(239, 83, 80), "Safety Min"))
        ]);

        return dashboard;
    }

    /// <summary>
    /// Creates the observatory dashboard for dashboard.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dashboard CreateObservatoryDashboard() {
        var dashboard = new Dashboard {
            Name = "Observatory",
            Rows = 4,
            Columns = 5
        };

        dashboard.Tiles.AddRange([
            ValueTile("Rain Rate", MetricType.RainRate, 0, 0),
            ValueTile("Wind Gust", MetricType.WindGust, 0, 1),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 2),
            ValueTile("Safety", MetricType.IsSafe, 0, 3),
            ValueTile("Sky Temp", MetricType.SkyTemperature, 0, 4),
            ChartTile("Rain & Clouds (24h)", 1, 0, 3, 2, ChartPeriod.Last24Hours,
                Aggregation(MetricType.RainRate, AggregationFunction.Maximum, Color.FromArgb(66, 165, 245), "Rain Max"),
                Aggregation(MetricType.CloudCover, AggregationFunction.Average, Color.FromArgb(120, 144, 156), "Cloud Avg")),
            ChartTile("Wind Stability (24h)", 1, 2, 3, 2, ChartPeriod.Last24Hours,
                Aggregation(MetricType.WindSpeed, AggregationFunction.Average, Color.FromArgb(255, 202, 40), "Wind Avg"),
                Aggregation(MetricType.WindGust, AggregationFunction.Maximum, Color.FromArgb(255, 112, 67), "Gust Max")),
            ChartTile("Thermal Delta: Ambient vs Sky (24h)", 1, 4, 3, 1, ChartPeriod.Last24Hours,
                Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 138, 101), "Ambient"),
                Aggregation(MetricType.SkyTemperature, AggregationFunction.Average, Color.FromArgb(41, 182, 246), "Sky"))
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
    private static ValueTileConfig ValueTile(string title, MetricType metric, int row, int column, ValueTileDisplayMode displayMode = ValueTileDisplayMode.TextAndValue, bool showUnit = true, bool showIcon = true, int decimalPlaces = 1, string? iconColorSchemeName = null, string? colorSchemeName = null, string? textColorSchemeName = null, string? valueSchemeName = null, Guid? id = null) {
        return new ValueTileConfig {
            Id = id ?? Guid.NewGuid(),
            Title = title,
            Metric = metric,
            Row = row,
            Column = column,
            DisplayMode = displayMode,
            ShowUnit = showUnit,
            ShowIcon = showIcon,
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
        return ChartTile(title, row, column, rowSpan, columnSpan, period, ChartLinkGroup.Alpha, string.Empty, null, series);
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
        MetricAggregation[] series) {
        return new ChartTileConfig {
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
            Label = label
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
