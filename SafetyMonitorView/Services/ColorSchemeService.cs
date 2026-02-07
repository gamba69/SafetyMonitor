using SafetyMonitorView.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafetyMonitorView.Services;

/// <summary>
/// JSON converter for System.Drawing.Color (not natively supported by System.Text.Json).
/// Stores as "#AARRGGBB" hex string.
/// </summary>
public class ColorJsonConverter : JsonConverter<Color> {
    #region Public Methods

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.String) {
            var hex = reader.GetString();
            if (string.IsNullOrEmpty(hex)) {
                return Color.Gray;
            }

            try {
                // Handle "#AARRGGBB" (9 chars) — ColorTranslator doesn't support alpha
                if (hex.StartsWith('#') && hex.Length == 9) {
                    var a = Convert.ToInt32(hex.Substring(1, 2), 16);
                    var r = Convert.ToInt32(hex.Substring(3, 2), 16);
                    var g = Convert.ToInt32(hex.Substring(5, 2), 16);
                    var b = Convert.ToInt32(hex.Substring(7, 2), 16);
                    return Color.FromArgb(a, r, g, b);
                }
                return ColorTranslator.FromHtml(hex);
            } catch { return Color.Gray; }
        }
        // Handle legacy object format: {"R":255,"G":0,"B":0,"A":255,...}
        if (reader.TokenType == JsonTokenType.StartObject) {
            int r = 0, g = 0, b = 0, a = 255;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
                if (reader.TokenType == JsonTokenType.PropertyName) {
                    var prop = reader.GetString();
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number) {
                        switch (prop) {
                            case "R": r = reader.GetInt32(); break;
                            case "G": g = reader.GetInt32(); break;
                            case "B": b = reader.GetInt32(); break;
                            case "A": a = reader.GetInt32(); break;
                        }
                    }
                }
            }
            return Color.FromArgb(a, r, g, b);
        }
        return Color.Gray;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }

    #endregion Public Methods
}

public class ColorSchemeService {
    #region Private Fields

    // Names of built-in schemes that can't be deleted
    private static readonly HashSet<string> BuiltInNames = ["Temperature", "Humidity", "Wind Speed", "Cloud Cover"];

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _schemesPath;

    #endregion Private Fields

    #region Public Constructors

    public ColorSchemeService() {
        _schemesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitorView", "ColorSchemes");
        Directory.CreateDirectory(_schemesPath);

        _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Converters = { new ColorJsonConverter() }
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

    public List<ColorScheme> LoadSchemes() {
        var schemes = new List<ColorScheme>
        {
            CreateTemperatureScheme(),
            CreateHumidityScheme(),
            CreateWindSpeedScheme(),
            CreateCloudCoverScheme()
        };

        foreach (var file in Directory.GetFiles(_schemesPath, "*.json")) {
            try {
                var json = File.ReadAllText(file);
                var scheme = JsonSerializer.Deserialize<ColorScheme>(json, _jsonOptions);
                if (scheme != null) {
                    // If user saved over a built-in name, replace the built-in
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
    public void SaveScheme(ColorScheme scheme) {
        var safeName = string.Join("_", scheme.Name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        var json = JsonSerializer.Serialize(scheme, _jsonOptions);
        File.WriteAllText(path, json);
    }

    #endregion Public Methods

    #region Private Methods

    private static ColorScheme CreateCloudCoverScheme() => new() {
        Name = "Cloud Cover",
        Stops =
        [
            new() { MaxValue = 10, Color = Color.LightGreen, Description = "Clear" },
            new() { MinValue = 10, MaxValue = 30, Color = Color.Green, Description = "Few clouds" },
            new() { MinValue = 30, MaxValue = 70, Color = Color.Gray, Description = "Cloudy" },
            new() { MinValue = 70, Color = Color.DarkGray, Description = "Overcast" }
        ]
    };

    private static ColorScheme CreateHumidityScheme() => new() {
        Name = "Humidity",
        Stops =
        [
            new() { MaxValue = 30, Color = Color.Orange, Description = "Very dry" },
            new() { MinValue = 30, MaxValue = 40, Color = Color.Yellow, Description = "Dry" },
            new() { MinValue = 40, MaxValue = 60, Color = Color.Green, Description = "Comfortable" },
            new() { MinValue = 60, MaxValue = 80, Color = Color.LightBlue, Description = "Humid" },
            new() { MinValue = 80, Color = Color.Blue, Description = "Very humid" }
        ]
    };

    private static ColorScheme CreateTemperatureScheme() => new() {
        Name = "Temperature",
        IsGradient = true,
        Stops =
        [
            new() { MaxValue = -20, Color = Color.FromArgb(0, 0, 139), Description = "Very cold" },
            new() { MinValue = -20, MaxValue = -10, Color = Color.Blue, Description = "Cold" },
            new() { MinValue = -10, MaxValue = 0, Color = Color.LightBlue, Description = "Cool" },
            new() { MinValue = 0, MaxValue = 10, Color = Color.Cyan, Description = "Fresh" },
            new() { MinValue = 10, MaxValue = 20, Color = Color.Green, Description = "Comfortable" },
            new() { MinValue = 20, MaxValue = 25, Color = Color.YellowGreen, Description = "Warm" },
            new() { MinValue = 25, MaxValue = 30, Color = Color.Yellow, Description = "Hot" },
            new() { MinValue = 30, MaxValue = 35, Color = Color.Orange, Description = "Very hot" },
            new() { MinValue = 35, Color = Color.Red, Description = "Extremely hot" }
        ]
    };
    private static ColorScheme CreateWindSpeedScheme() => new() {
        Name = "Wind Speed",
        Stops =
        [
            new() { MaxValue = 2, Color = Color.Green, Description = "Calm" },
            new() { MinValue = 2, MaxValue = 5, Color = Color.YellowGreen, Description = "Light" },
            new() { MinValue = 5, MaxValue = 10, Color = Color.Yellow, Description = "Moderate" },
            new() { MinValue = 10, MaxValue = 15, Color = Color.Orange, Description = "Strong" },
            new() { MinValue = 15, Color = Color.Red, Description = "Very strong" }
        ]
    };

    #endregion Private Methods
}
