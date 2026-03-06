using System.Text.Json.Serialization;
namespace SafetyMonitor.Models;

public enum ChartPeriod { Last15Minutes, LastHour, Last6Hours, Last24Hours, Last7Days, Last30Days, Custom }
public enum ValueTileDisplayMode { ValueOnly, TextOnly, TextAndValue }

public enum TileType { Value, Chart }
[JsonDerivedType(typeof(ValueTileConfig), typeDiscriminator: "value")]
[JsonDerivedType(typeof(ChartTileConfig), typeDiscriminator: "chart")]
/// <summary>
/// Represents tile config and encapsulates its related behavior and state.
/// </summary>
public abstract class TileConfig {

    #region Public Properties

    /// <summary>
    /// Gets or sets the column for tile config. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the column span for tile config. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int ColumnSpan { get; set; } = 1;
    /// <summary>
    /// Gets or sets the id for tile config. Identifies the related entity and is used for lookups, linking, or persistence.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the row for tile config. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int Row { get; set; }
    /// <summary>
    /// Gets or sets the row span for tile config. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public int RowSpan { get; set; } = 1;
    /// <summary>
    /// Gets or sets the title for tile config. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// Gets or sets the type for tile config. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public abstract TileType Type { get; }

    #endregion Public Properties
}
/// <summary>
/// Represents value tile config and encapsulates its related behavior and state.
/// </summary>
public class ValueTileConfig : TileConfig {

    #region Public Properties

    /// <summary>
    /// Gets or sets the color scheme name for value tile config. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string ColorSchemeName { get; set; } = "Temperature";
    /// <summary>
    /// Gets or sets the decimal places for value tile config. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int DecimalPlaces { get; set; } = 1;
    /// <summary>
    /// Gets or sets the icon color scheme name for value tile config. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string IconColorSchemeName { get; set; } = "";
    /// <summary>
    /// Gets or sets the metric for value tile config. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public MetricType Metric { get; set; }
    /// <summary>
    /// Gets or sets the display mode for value tile config. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public ValueTileDisplayMode DisplayMode { get; set; } = ValueTileDisplayMode.TextOnly;
    /// <summary>
    /// Gets or sets the show icon for value tile config. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowIcon { get; set; } = true;
    /// <summary>
    /// Gets or sets the show top value gradient for value tile config. Defines layout or geometry used to position and size UI elements.
    /// </summary>
    public bool ShowTopValueGradient { get; set; }
    /// <summary>
    /// Gets or sets the show unit for value tile config. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowUnit { get; set; } = true;
    /// <summary>
    /// Gets or sets the text color scheme name for value tile config. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string TextColorSchemeName { get; set; } = "";
    public override TileType Type => TileType.Value;
    /// <summary>
    /// Gets or sets the value scheme name for value tile config. Controls visual presentation used by themed rendering and UI styling.
    /// </summary>
    public string ValueSchemeName { get; set; } = "";

    #endregion Public Properties
}
/// <summary>
/// Represents chart tile config and encapsulates its related behavior and state.
/// </summary>
public class ChartTileConfig : TileConfig {

    #region Public Properties

    [JsonIgnore]
    /// <summary>
    /// Gets or sets the custom aggregation interval for chart tile config. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public TimeSpan? CustomAggregationInterval { get; set; }
    [JsonIgnore]
    /// <summary>
    /// Gets or sets the custom period duration for chart tile config. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public TimeSpan? CustomPeriodDuration { get; set; }
    [JsonIgnore]
    /// <summary>
    /// Gets or sets the custom end time for chart tile config. Stores a timestamp used for ordering, filtering, or range calculations.
    /// </summary>
    public DateTime? CustomEndTime { get; set; }
    [JsonIgnore]
    /// <summary>
    /// Gets or sets the custom start time for chart tile config. Stores a timestamp used for ordering, filtering, or range calculations.
    /// </summary>
    public DateTime? CustomStartTime { get; set; }
    /// <summary>
    /// Gets or sets the metric aggregations for chart tile config. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<MetricAggregation> MetricAggregations { get; set; } = [];
    /// <summary>
    /// Gets or sets the link group for chart tile config. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public ChartLinkGroup LinkGroup { get; set; } = ChartLinkGroup.Alpha;
    [JsonIgnore]
    /// <summary>
    /// Gets or sets the period for chart tile config. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public ChartPeriod Period { get; set; } = ChartPeriod.Last24Hours;
    /// <summary>
    /// Gets or sets the period preset uid for chart tile config. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public string PeriodPresetUid { get; set; } = "";
    /// <summary>
    /// Gets or sets the show grid for chart tile config. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowGrid { get; set; } = true;
    /// <summary>
    /// Gets or sets the show inspector for chart tile config. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowInspector { get; set; }
    /// <summary>
    /// Gets or sets the show legend for chart tile config. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool ShowLegend { get; set; } = true;
    [JsonIgnore]
    /// <summary>
    /// Gets or sets the static mode paused for chart tile config. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool StaticModePaused { get; set; }
    public override TileType Type => TileType.Chart;

    #endregion Public Properties
}
