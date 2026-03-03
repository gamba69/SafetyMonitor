using DataStorage.Models;

namespace SafetyMonitor.Models;

public class Dashboard {

    #region Public Properties
    public int Columns { get; set; } = 4;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsQuickAccess { get; set; } = false;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public string Name { get; set; } = "New Dashboard";
    public int Rows { get; set; } = 4;
    public int SortOrder { get; set; }
    public List<TileConfig> Tiles { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

    public static Dashboard CreateDefault() => CreateDefaultSet().First();

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

    #endregion Public Methods

    #region Private Methods

    private static Dashboard CreateOverviewDashboard() {
        var dashboard = new Dashboard {
            Name = "Overview",
            Rows = 4,
            Columns = 4,
            IsQuickAccess = true
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0),
            ValueTile("Cloud Cover", MetricType.CloudCover, 0, 1),
            ValueTile("Rain Rate", MetricType.RainRate, 0, 2),
            ValueTile("Wind Speed", MetricType.WindSpeed, 0, 3),
            ChartTile("Temperature / Dew Point (24h)", 1, 0, 3, 2, ChartPeriod.Last24Hours,
                Aggregation(MetricType.Temperature, AggregationFunction.Average, Color.FromArgb(255, 112, 67), "Temperature"),
                Aggregation(MetricType.DewPoint, AggregationFunction.Average, Color.FromArgb(66, 165, 245), "Dew Point")),
            ChartTile("Humidity / Pressure (24h)", 1, 2, 2, 2, ChartPeriod.Last24Hours,
                Aggregation(MetricType.Humidity, AggregationFunction.Average, Color.FromArgb(38, 166, 154), "Humidity"),
                Aggregation(MetricType.Pressure, AggregationFunction.Average, Color.FromArgb(171, 71, 188), "Pressure")),
            ChartTile("Wind & Rain (6h)", 3, 2, 1, 2, ChartPeriod.Last6Hours,
                Aggregation(MetricType.WindSpeed, AggregationFunction.Maximum, Color.FromArgb(255, 202, 40), "Wind Max"),
                Aggregation(MetricType.RainRate, AggregationFunction.Maximum, Color.FromArgb(239, 83, 80), "Rain Max"))
        ]);

        return dashboard;
    }

    private static Dashboard CreateNowDashboard() {
        var dashboard = new Dashboard {
            Name = "Now",
            Rows = 3,
            Columns = 5,
            IsQuickAccess = true
        };

        dashboard.Tiles.AddRange([
            ValueTile("Safety", MetricType.IsSafe, 0, 0),
            ValueTile("Temperature", MetricType.Temperature, 0, 1),
            ValueTile("Humidity", MetricType.Humidity, 0, 2),
            ValueTile("Pressure", MetricType.Pressure, 0, 3),
            ValueTile("Dew Point", MetricType.DewPoint, 0, 4),
            ValueTile("Cloud Cover", MetricType.CloudCover, 1, 0),
            ValueTile("Sky Temp", MetricType.SkyTemperature, 1, 1),
            ValueTile("Sky Brightness", MetricType.SkyBrightness, 1, 2),
            ValueTile("Sky Quality", MetricType.SkyQuality, 1, 3),
            ValueTile("Rain Rate", MetricType.RainRate, 1, 4),
            ValueTile("Wind Speed", MetricType.WindSpeed, 2, 0),
            ValueTile("Wind Gust", MetricType.WindGust, 2, 1),
            ValueTile("Wind Direction", MetricType.WindDirection, 2, 2),
            ValueTile("Star FWHM", MetricType.StarFwhm, 2, 3)
        ]);

        return dashboard;
    }

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

    private static ValueTileConfig ValueTile(string title, MetricType metric, int row, int column) {
        return new ValueTileConfig {
            Title = title,
            Metric = metric,
            Row = row,
            Column = column,
            DisplayMode = ValueTileDisplayMode.TextAndValue
        };
    }

    private static ChartTileConfig ChartTile(string title, int row, int column, int rowSpan, int columnSpan, ChartPeriod period, params MetricAggregation[] series) {
        return new ChartTileConfig {
            Title = title,
            Row = row,
            Column = column,
            RowSpan = rowSpan,
            ColumnSpan = columnSpan,
            Period = period,
            ShowInspector = true,
            MetricAggregations = [.. series]
        };
    }

    private static MetricAggregation Aggregation(MetricType metric, AggregationFunction function, Color color, string label) {
        return new MetricAggregation {
            Metric = metric,
            Function = function,
            Color = color,
            DarkThemeColor = CreateDarkThemeSeriesColor(color),
            Label = label
        };
    }

    private static Color CreateDarkThemeSeriesColor(Color lightThemeColor) {
        var hue = lightThemeColor.GetHue();
        var saturation = lightThemeColor.GetSaturation();
        var brightness = lightThemeColor.GetBrightness();

        const double darkThemeValue = 0.82;
        var adjustedBrightness = Math.Max(brightness, darkThemeValue);
        return ColorFromHsv(hue, saturation, adjustedBrightness);
    }

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
