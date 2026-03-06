using SafetyMonitor.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace SafetyMonitor.Services;

/// <summary>
/// Represents dashboard service and encapsulates its related behavior and state.
/// </summary>
public class DashboardService {

    #region Private Fields

    private readonly string _configDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardService"/> class.
    /// </summary>
    public DashboardService() {
        _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SafetyMonitor", "Dashboards");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter(), new ColorJsonConverter() } };
        Directory.CreateDirectory(_configDirectory);
    }

    #endregion Public Constructors

    #region Public Methods
    /// <summary>
    /// Deletes the dashboard for dashboard service.
    /// </summary>
    /// <param name="dashboard">Input value for dashboard.</param>
    public void DeleteDashboard(Dashboard dashboard) {
        EnsureConfigDirectoryExists();
        var path = Path.Combine(_configDirectory, $"{dashboard.Id}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Executes duplicate dashboard as part of dashboard service processing.
    /// </summary>
    /// <param name="source">Input value for source.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Loads the dashboards for dashboard service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public List<Dashboard> LoadDashboards() {
        EnsureConfigDirectoryExists();
        var dashboards = new List<Dashboard>();
        try {
            foreach (var file in Directory.GetFiles(_configDirectory, "*.json")) {
                try {
                    var db = JsonSerializer.Deserialize<Dashboard>(File.ReadAllText(file), _jsonOptions);
                    if (db != null) {
                        var updated = EnsureDashboardDefaults(db);
                        if (updated) {
                            File.WriteAllText(file, JsonSerializer.Serialize(db, _jsonOptions));
                        }

                        dashboards.Add(db);
                    }
                } catch { }
            }
        } catch { }
        if (dashboards.Count == 0) {
            var defaults = Dashboard.CreateDefaultSet();
            foreach (var dashboard in defaults) {
                SaveDashboard(dashboard);
            }
            dashboards.AddRange(defaults);
        }
        return [.. dashboards
            .OrderByDescending(d => d.IsQuickAccess)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.Name)];
    }

    /// <summary>
    /// Resets the to single default dashboard for dashboard service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Dashboard ResetToSingleDefaultDashboard() {
        EnsureConfigDirectoryExists();

        foreach (var dashboardPath in Directory.GetFiles(_configDirectory, "*.json")) {
            File.Delete(dashboardPath);
        }

        var defaults = Dashboard.CreateDefaultSet();
        foreach (var dashboard in defaults) {
            SaveDashboard(dashboard);
        }

        return defaults.First();
    }

    /// <summary>
    /// Saves the dashboard for dashboard service.
    /// </summary>
    /// <param name="dashboard">Input value for dashboard.</param>
    public void SaveDashboard(Dashboard dashboard) {
        EnsureConfigDirectoryExists();
        dashboard.ModifiedAt = DateTime.Now;
        File.WriteAllText(Path.Combine(_configDirectory, $"{dashboard.Id}.json"), JsonSerializer.Serialize(dashboard, _jsonOptions));
    }

    /// <summary>
    /// Ensures the dashboard defaults for dashboard service.
    /// </summary>
    /// <param name="dashboard">Input value for dashboard.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool EnsureDashboardDefaults(Dashboard dashboard) {
        var hadAllLinkGroupPeriodDefaults = ChartLinkGroupInfo.All
            .All(group => dashboard.LinkGroupPeriodPresetUids.TryGetValue(group, out var uid)
                && !string.IsNullOrWhiteSpace(uid));

        dashboard.EnsureLinkGroupPeriodDefaults();
        var hadValidLinkGroupConfiguration = !dashboard.EnsureLinkGroupConfiguration();

        return !hadAllLinkGroupPeriodDefaults || !hadValidLinkGroupConfiguration;
    }

    /// <summary>
    /// Ensures the config directory exists for dashboard service.
    /// </summary>
    private void EnsureConfigDirectoryExists() {
        Directory.CreateDirectory(_configDirectory);
    }

    #endregion Public Methods
}
