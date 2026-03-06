using SafetyMonitor.Models;
using System.Text.Json;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents app settings service and encapsulates its related behavior and state.
/// </summary>
public class AppSettingsService {
    #region Private Fields

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _settingsPath;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettingsService"/> class.
    /// </summary>
    public AppSettingsService() {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitor"
        );
        Directory.CreateDirectory(appDataFolder);

        _settingsPath = Path.Combine(appDataFolder, "settings.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    #endregion Public Constructors

    #region Public Methods

    public string AppDataFolderPath => Path.GetDirectoryName(_settingsPath) ?? string.Empty;
    public string SettingsPath => _settingsPath;

    /// <summary>
    /// Loads the settings for app settings service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public AppSettings LoadSettings() {
        try {
            if (File.Exists(_settingsPath)) {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                return AppSettingsDefaultsService.Normalize(settings);
            }
        } catch {
            // If loading fails, return defaults
        }

        return AppSettingsDefaultsService.CreateDefaults();
    }

    /// <summary>
    /// Saves the settings for app settings service.
    /// </summary>
    /// <param name="settings">Input value for settings.</param>
    public void SaveSettings(AppSettings settings) {
        try {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        } catch {
            // Ignore save errors
        }
    }

    #endregion Public Methods
}
