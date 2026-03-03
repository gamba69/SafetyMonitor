using SafetyMonitor.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafetyMonitor.Services;

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
                return Color.Empty;
            }

            try {
                // Handle "#AARRGGBB" (9 chars) — ColorTranslator doesn't support alpha
                if (hex.StartsWith('#') && hex.Length == 9) {
                    var a = Convert.ToInt32(hex.Substring(1, 2), 16);
                    var r = Convert.ToInt32(hex.Substring(3, 2), 16);
                    var g = Convert.ToInt32(hex.Substring(5, 2), 16);
                    var b = Convert.ToInt32(hex.Substring(7, 2), 16);
                    if (a == 0 && r == 0 && g == 0 && b == 0) {
                        return Color.Empty;
                    }

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
        if (value.IsEmpty) {
            writer.WriteStringValue(string.Empty);
            return;
        }

        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }

    #endregion Public Methods
}

public class ColorSchemeService {
    #region Private Fields

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _schemesPath;

    #endregion Private Fields

    #region Public Constructors

    public ColorSchemeService() {
        _schemesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitor", "ColorSchemes");
        Directory.CreateDirectory(_schemesPath);

        _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Converters = { new ColorJsonConverter() }
        };

        EnsureDefaultSchemesExist();
    }

    #endregion Public Constructors

    #region Public Methods

    public static string GetDefaultSchemeName(MetricType metric) => metric switch {
        MetricType.IsSafe => "Safety",
        MetricType.Temperature => "Temperature",
        MetricType.Humidity => "Humidity",
        MetricType.Pressure => "Pressure",
        MetricType.CloudCover => "Cloud Cover",
        MetricType.SkyBrightness => "Sky Brightness",
        MetricType.SkyQuality => "Sky Quality",
        MetricType.RainRate => "Rain Rate",
        MetricType.WindSpeed => "Wind Speed",
        MetricType.WindGust => "Wind Gust",
        _ => string.Empty
    };

    public void DeleteScheme(string name) {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    public List<ColorScheme> LoadSchemes() {
        var schemes = new List<ColorScheme>();

        foreach (var file in Directory.GetFiles(_schemesPath, "*.json")) {
            try {
                var json = File.ReadAllText(file);
                var scheme = JsonSerializer.Deserialize<ColorScheme>(json, _jsonOptions);
                if (scheme != null) {
                    schemes.Add(scheme);
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

    private void EnsureDefaultSchemesExist() {
        foreach (var scheme in GetDefaultSchemes()) {
            var safeName = string.Join("_", scheme.Name.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(_schemesPath, $"{safeName}.json");
            if (File.Exists(path)) {
                continue;
            }

            var json = JsonSerializer.Serialize(scheme, _jsonOptions);
            File.WriteAllText(path, json);
        }
    }

    private static IEnumerable<ColorScheme> GetDefaultSchemes() {
        yield return CreateSafetyScheme();
        yield return CreateTemperatureScheme();
        yield return CreateHumidityScheme();
        yield return CreatePressureScheme();
        yield return CreateWindSpeedScheme();
        yield return CreateWindGustScheme();
        yield return CreateCloudCoverScheme();
        yield return CreateSkyBrightnessScheme();
        yield return CreateSkyQualityScheme();
        yield return CreateRainRateScheme();
    }

    private static ColorScheme CreateSafetyScheme() => new() {
        Name = "Safety",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0, Color = Color.Red, Description = "Unsafe" },
            new() { Value = 100, Color = Color.LightGreen, Description = "Safe" }
        ]
    };

    private static ColorScheme CreateCloudCoverScheme() => new() {
        Name = "Cloud Cover",
        IsGradient = true,
        Stops =
        [
            new() { Value = 10, Color = Color.LightGreen, Description = "Clear" },
            new() { Value = 30, Color = Color.Green, Description = "Few clouds" },
            new() { Value = 70, Color = Color.Gray, Description = "Cloudy" },
            new() { Value = 100, Color = Color.DarkGray, Description = "Overcast" }
        ]
    };

    private static ColorScheme CreateHumidityScheme() => new() {
        Name = "Humidity",
        IsGradient = true,
        Stops =
        [
            new() { Value = 30, Color = Color.Orange, Description = "Very dry" },
            new() { Value = 40, Color = Color.Yellow, Description = "Dry" },
            new() { Value = 60, Color = Color.Green, Description = "Comfortable" },
            new() { Value = 80, Color = Color.LightBlue, Description = "Humid" },
            new() { Value = 100, Color = Color.Blue, Description = "Very humid" }
        ]
    };

    private static ColorScheme CreateTemperatureScheme() => new() {
        Name = "Temperature",
        IsGradient = true,
        Stops =
        [
            new() { Value = -20, Color = Color.FromArgb(0, 0, 139), Description = "Very cold" },
            new() { Value = -10, Color = Color.Blue, Description = "Cold" },
            new() { Value = 0, Color = Color.LightBlue, Description = "Cool" },
            new() { Value = 10, Color = Color.Cyan, Description = "Fresh" },
            new() { Value = 20, Color = Color.Green, Description = "Comfortable" },
            new() { Value = 25, Color = Color.YellowGreen, Description = "Warm" },
            new() { Value = 30, Color = Color.Yellow, Description = "Hot" },
            new() { Value = 35, Color = Color.Orange, Description = "Very hot" },
            new() { Value = 45, Color = Color.Red, Description = "Extremely hot" }
        ]
    };
    private static ColorScheme CreateWindSpeedScheme() => new() {
        Name = "Wind Speed",
        IsGradient = true,
        Stops =
        [
            new() { Value = 2, Color = Color.Green, Description = "Calm" },
            new() { Value = 5, Color = Color.YellowGreen, Description = "Light" },
            new() { Value = 10, Color = Color.Yellow, Description = "Moderate" },
            new() { Value = 15, Color = Color.Orange, Description = "Strong" },
            new() { Value = 20, Color = Color.Red, Description = "Very strong" }
        ]
    };

    private static ColorScheme CreatePressureScheme() => new() {
        Name = "Pressure",
        IsGradient = true,
        Stops =
        [
            new() { Value = 985, Color = Color.OrangeRed, Description = "Very low" },
            new() { Value = 1000, Color = Color.Gold, Description = "Low" },
            new() { Value = 1015, Color = Color.LightGreen, Description = "Normal" },
            new() { Value = 1030, Color = Color.SteelBlue, Description = "High" },
            new() { Value = 1045, Color = Color.MediumPurple, Description = "Very high" }
        ]
    };

    private static ColorScheme CreateSkyBrightnessScheme() => new() {
        Name = "Sky Brightness",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0, Color = Color.FromArgb(15, 23, 42), Description = "Dark night" },
            new() { Value = 1, Color = Color.FromArgb(30, 58, 138), Description = "Night" },
            new() { Value = 50, Color = Color.FromArgb(59, 130, 246), Description = "Dusk" },
            new() { Value = 1000, Color = Color.FromArgb(253, 224, 71), Description = "Twilight" },
            new() { Value = 20000, Color = Color.FromArgb(255, 255, 255), Description = "Daylight" }
        ]
    };

    private static ColorScheme CreateSkyQualityScheme() => new() {
        Name = "Sky Quality",
        IsGradient = true,
        Stops =
        [
            new() { Value = 18.5, Color = Color.OrangeRed, Description = "Poor" },
            new() { Value = 19.5, Color = Color.Gold, Description = "Fair" },
            new() { Value = 20.5, Color = Color.LightGreen, Description = "Good" },
            new() { Value = 21.5, Color = Color.DeepSkyBlue, Description = "Excellent" },
            new() { Value = 22.5, Color = Color.MediumPurple, Description = "Outstanding" }
        ]
    };

    private static ColorScheme CreateRainRateScheme() => new() {
        Name = "Rain Rate",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0, Color = Color.LightGreen, Description = "Dry" },
            new() { Value = 0.001, Color = Color.LightSkyBlue, Description = "Drizzle" },
            new() { Value = 2, Color = Color.DodgerBlue, Description = "Rain" },
            new() { Value = 6, Color = Color.Blue, Description = "Heavy rain" },
            new() { Value = 15, Color = Color.DarkBlue, Description = "Downpour" }
        ]
    };

    private static ColorScheme CreateWindGustScheme() => new() {
        Name = "Wind Gust",
        IsGradient = true,
        Stops =
        [
            new() { Value = 2, Color = Color.LightGreen, Description = "Light" },
            new() { Value = 5, Color = Color.YellowGreen, Description = "Breeze" },
            new() { Value = 8, Color = Color.Gold, Description = "Gusty" },
            new() { Value = 12, Color = Color.Orange, Description = "Strong gust" },
            new() { Value = 20, Color = Color.Red, Description = "Severe gust" }
        ]
    };

    #endregion Private Methods
}
