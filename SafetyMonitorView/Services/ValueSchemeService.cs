using SafetyMonitorView.Models;
using System.Text.Json;

namespace SafetyMonitorView.Services;

public class ValueSchemeService {
    #region Private Fields

    private static readonly HashSet<string> BuiltInNames = ["Safety"];

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _schemesPath;

    #endregion Private Fields

    #region Public Constructors

    public ValueSchemeService() {
        _schemesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitorView", "ValueSchemes");
        Directory.CreateDirectory(_schemesPath);

        _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        };
    }

    #endregion Public Constructors

    #region Public Methods

    public static bool IsBuiltIn(string name) => BuiltInNames.Contains(name);

    public void DeleteScheme(string name) {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public List<ValueScheme> LoadSchemes() {
        var schemes = new List<ValueScheme>
        {
            CreateSafetyScheme()
        };

        foreach (var file in Directory.GetFiles(_schemesPath, "*.json")) {
            try {
                var json = File.ReadAllText(file);
                var scheme = JsonSerializer.Deserialize<ValueScheme>(json, _jsonOptions);
                if (scheme != null) {
                    var existing = schemes.FindIndex(s => s.Name == scheme.Name);
                    if (existing >= 0) {
                        schemes[existing] = scheme;
                    } else {
                        schemes.Add(scheme);
                    }
                }
            } catch { }
        }

        return schemes;
    }

    public void SaveScheme(ValueScheme scheme) {
        var safeName = string.Join("_", scheme.Name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        var json = JsonSerializer.Serialize(scheme, _jsonOptions);
        File.WriteAllText(path, json);
    }

    #endregion Public Methods

    #region Private Methods

    private static ValueScheme CreateSafetyScheme() => new() {
        Name = "Safety",
        Descending = true,
        Stops =
        [
            new() { Value = 100, Text = "SAFE", Description = "Safe condition (100%)" },
            new() { Value = 0, Text = "UNSAFE", Description = "Unsafe or partially unsafe condition (<100%)" }
        ]
    };

    #endregion Private Methods
}
