using System.Text.Json.Serialization;
namespace SafetyMonitor.Models;

public enum ChartPeriod { Last15Minutes, LastHour, Last6Hours, Last24Hours, Last7Days, Last30Days, Custom }
public enum ValueTileDisplayMode { ValueOnly, TextOnly, TextAndValue }

public enum TileType { Value, Chart }
[JsonDerivedType(typeof(ValueTileConfig), typeDiscriminator: "value")]
[JsonDerivedType(typeof(ChartTileConfig), typeDiscriminator: "chart")]
public abstract class TileConfig {

    #region Public Properties

    public int Column { get; set; }
    public int ColumnSpan { get; set; } = 1;
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Row { get; set; }
    public int RowSpan { get; set; } = 1;
    public string Title { get; set; } = "";
    public abstract TileType Type { get; }

    #endregion Public Properties
}
public class ValueTileConfig : TileConfig {

    #region Public Properties

    public string ColorSchemeName { get; set; } = "Temperature";
    public int DecimalPlaces { get; set; } = 1;
    public string IconColorSchemeName { get; set; } = "";
    public MetricType Metric { get; set; }
    public ValueTileDisplayMode DisplayMode { get; set; } = ValueTileDisplayMode.TextOnly;
    public bool ShowIcon { get; set; } = true;
    public bool ShowTopValueGradient { get; set; }
    public bool ShowUnit { get; set; } = true;
    public string TextColorSchemeName { get; set; } = "";
    public override TileType Type => TileType.Value;
    public string ValueSchemeName { get; set; } = "";

    #endregion Public Properties
}
public class ChartTileConfig : TileConfig {

    #region Public Properties

    [JsonIgnore]
    public TimeSpan? CustomAggregationInterval { get; set; }
    [JsonIgnore]
    public TimeSpan? CustomPeriodDuration { get; set; }
    [JsonIgnore]
    public DateTime? CustomEndTime { get; set; }
    [JsonIgnore]
    public DateTime? CustomStartTime { get; set; }
    public List<MetricAggregation> MetricAggregations { get; set; } = [];
    public ChartLinkGroup LinkGroup { get; set; } = ChartLinkGroup.Alpha;
    [JsonIgnore]
    public ChartPeriod Period { get; set; } = ChartPeriod.Last24Hours;
    public string PeriodPresetUid { get; set; } = "";
    public bool ShowGrid { get; set; } = true;
    public bool ShowInspector { get; set; }
    public bool ShowLegend { get; set; } = true;
    [JsonIgnore]
    public bool StaticModePaused { get; set; }
    public override TileType Type => TileType.Chart;

    #endregion Public Properties
}
