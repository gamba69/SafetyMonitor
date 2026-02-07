using SafetyMonitorView.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace SafetyMonitorView.Services;

public class DashboardService {

    #region Private Fields

    private readonly string _configDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion Private Fields

    #region Public Constructors

    public DashboardService() {
        _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SafetyMonitorView", "Dashboards");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter(), new ColorJsonConverter() } };
        Directory.CreateDirectory(_configDirectory);
    }

    #endregion Public Constructors

    #region Public Methods
    public void DeleteDashboard(Dashboard dashboard) {
        var path = Path.Combine(_configDirectory, $"{dashboard.Id}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public Dashboard DuplicateDashboard(Dashboard source) {
        var copy = JsonSerializer.Deserialize<Dashboard>(JsonSerializer.Serialize(source, _jsonOptions), _jsonOptions)!;
        copy.Id = Guid.NewGuid();
        copy.Name = $"{source.Name} (Copy)";
        copy.CreatedAt = DateTime.Now;
        foreach (var tile in copy.Tiles) {

            tile.Id = Guid.NewGuid();

        }
        SaveDashboard(copy);
        return copy;
    }

    public List<Dashboard> LoadDashboards() {
        var dashboards = new List<Dashboard>();
        try {
            foreach (var file in Directory.GetFiles(_configDirectory, "*.json")) {
                try {
                    var db = JsonSerializer.Deserialize<Dashboard>(File.ReadAllText(file), _jsonOptions);
                    if (db != null) {
                        dashboards.Add(db);
                    }
                } catch { }
            }
        } catch { }
        if (dashboards.Count == 0) {
            var def = Dashboard.CreateDefault();
            SaveDashboard(def);
            dashboards.Add(def);
        }
        return [.. dashboards.OrderBy(d => d.Name)];
    }

    public void SaveDashboard(Dashboard dashboard) {
        dashboard.ModifiedAt = DateTime.Now;
        File.WriteAllText(Path.Combine(_configDirectory, $"{dashboard.Id}.json"), JsonSerializer.Serialize(dashboard, _jsonOptions));
    }

    #endregion Public Methods
}
