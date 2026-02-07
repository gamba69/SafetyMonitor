using SafetyMonitorView.Models;
using System.Text.Json;

namespace SafetyMonitorView.Services;

public class AppSettingsService {
    #region Private Fields

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _settingsPath;

    #endregion Private Fields

    #region Public Constructors

    public AppSettingsService() {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitorView"
        );
        Directory.CreateDirectory(appDataFolder);

        _settingsPath = Path.Combine(appDataFolder, "settings.json");
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    #endregion Public Constructors

    #region Public Methods

    public AppSettings LoadSettings() {
        try {
            if (File.Exists(_settingsPath)) {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                return settings ?? new AppSettings();
            }
        } catch {
            // If loading fails, return defaults
        }

        return new AppSettings();
    }

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
