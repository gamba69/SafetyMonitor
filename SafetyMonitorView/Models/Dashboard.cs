namespace SafetyMonitorView.Models;

public class Dashboard {

    #region Public Properties
    public int Columns { get; set; } = 4;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsQuickAccess { get; set; } = false;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    public string Name { get; set; } = "New Dashboard";
    public int Rows { get; set; } = 4;
    public List<TileConfig> Tiles { get; set; } = [];

    #endregion Public Properties

    #region Public Methods

    public static Dashboard CreateDefault() {
        var d = new Dashboard { Name = "Main Dashboard", Rows = 3, Columns = 4, IsQuickAccess = true };
        d.Tiles.Add(new ValueTileConfig { Title = "Temperature", Metric = MetricType.Temperature, Row = 0, Column = 0 });
        d.Tiles.Add(new ValueTileConfig { Title = "Humidity", Metric = MetricType.Humidity, Row = 0, Column = 1 });
        d.Tiles.Add(new ChartTileConfig { Title = "Chart", Row = 1, Column = 0, RowSpan = 2, ColumnSpan = 4 });
        return d;
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
}
