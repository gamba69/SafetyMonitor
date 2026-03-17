using SafetyMonitor.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents color json converter and encapsulates its related behavior and state.
/// </summary>
public class ColorJsonConverter : JsonConverter<Color> {
    #region Public Methods

    /// <summary>
    /// Executes read as part of color json converter processing.
    /// </summary>
    /// <param name="reader">Input value for reader.</param>
    /// <param name="typeToConvert">Input value for type to convert.</param>
    /// <param name="options">Input value for options.</param>
    /// <returns>The result of the operation.</returns>
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
        // Handle legacy object format: {"R":255,"G":0,"B":0,"A":255,…}
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

    /// <summary>
    /// Executes write as part of color json converter processing.
    /// </summary>
    /// <param name="writer">Input value for writer.</param>
    /// <param name="value">Input value for value.</param>
    /// <param name="options">Input value for options.</param>
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
        if (value.IsEmpty) {
            writer.WriteStringValue(string.Empty);
            return;
        }

        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }

    #endregion Public Methods
}

/// <summary>
/// Represents color scheme service and encapsulates its related behavior and state.
/// </summary>
public class ColorSchemeService {
    #region Private Fields

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _schemesPath;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorSchemeService"/> class.
    /// </summary>
    public ColorSchemeService() {
        _schemesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitor", "ColorSchemes");
        EnsureSchemesDirectoryExists();

        _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Converters = { new ColorJsonConverter() }
        };

        EnsureDefaultSchemesExist();
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Gets the default scheme name for color scheme service.
    /// </summary>
    /// <param name="metric">Input value for metric.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetDefaultSchemeName(MetricType metric) => metric switch {
        MetricType.IsSafe => "Safety",
        MetricType.Temperature => "Temperature",
        MetricType.Apparent => "Temperature",
        MetricType.Humidity => "Humidity",
        MetricType.Pressure => "Pressure",
        MetricType.CloudCover => "Cloud Cover",
        MetricType.SkyBrightness => "Sky Brightness",
        MetricType.SkyQualitySQM => "Sky Quality (SQM)",
        MetricType.SkyQualityNELM => "Sky Quality (NELM)",
        MetricType.RainRate => "Rain Rate",
        MetricType.WindSpeed => "Wind Speed",
        MetricType.WindGust => "Wind Gust",
        _ => string.Empty
    };

    /// <summary>
    /// Deletes the scheme for color scheme service.
    /// </summary>
    /// <param name="name">Input value for name.</param>
    public void DeleteScheme(string name) {
        EnsureSchemesDirectoryExists();
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Loads the schemes for color scheme service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public List<ColorScheme> LoadSchemes() {
        var schemes = new List<ColorScheme>();
        EnsureSchemesDirectoryExists();

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
    /// <summary>
    /// Saves the scheme for color scheme service.
    /// </summary>
    /// <param name="scheme">Input value for scheme.</param>
    public void SaveScheme(ColorScheme scheme) {
        EnsureSchemesDirectoryExists();
        var safeName = string.Join("_", scheme.Name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        var json = JsonSerializer.Serialize(scheme, _jsonOptions);
        File.WriteAllText(path, json);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Ensures the default schemes exist for color scheme service.
    /// </summary>
    private void EnsureDefaultSchemesExist() {
        EnsureSchemesDirectoryExists();

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

    /// <summary>
    /// Ensures the schemes directory exists for color scheme service.
    /// </summary>
    private void EnsureSchemesDirectoryExists() => Directory.CreateDirectory(_schemesPath);

    /// <summary>
    /// Gets the default schemes for color scheme service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
        yield return CreateNelmScheme();
        yield return CreateRainRateScheme();
    }

    /// <summary>
    /// Creates the safety scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ColorScheme CreateSafetyScheme() => new() {
        Name = "Safety",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0, Color = Color.Red, Description = "Unsafe" },
            new() { Value = 100, Color = Color.LightGreen, Description = "Safe" }
        ]
    };

    /// <summary>
    /// Creates the cloud cover scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ColorScheme CreateCloudCoverScheme() => new() {
        Name = "Cloud Cover",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0, Color = Color.LightGreen, Description = "Clear (0%)" },
            new() { Value = 25, Color = Color.Green, Description = "Few (up to 25%)" },
            new() { Value = 50, Color = Color.YellowGreen, Description = "Scatter (up to 50%)" },
            new() { Value = 99, Color = Color.Gray, Description = "Broken (up to 99%)" },
            new() { Value = 100, Color = Color.DarkGray, Description = "Overcast (100%)" }
        ]
    };

    /// <summary>
    /// Creates the humidity scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates the temperature scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
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
    /// <summary>
    /// Creates the wind speed scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ColorScheme CreateWindSpeedScheme() => new() {
        Name = "Wind Speed",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0.0, Color = Color.FromArgb(46, 125, 50), Description = "Calm" },
            new() { Value = 0.2, Color = Color.FromArgb(56, 142, 60), Description = "Wispy" },
            new() { Value = 1.6, Color = Color.FromArgb(76, 175, 80), Description = "Light" },
            new() { Value = 3.4, Color = Color.FromArgb(139, 195, 74), Description = "Gentle" },
            new() { Value = 5.5, Color = Color.FromArgb(205, 220, 57), Description = "Moderate" },
            new() { Value = 8.0, Color = Color.FromArgb(255, 235, 59), Description = "Fresh" },
            new() { Value = 10.8, Color = Color.FromArgb(255, 193, 7), Description = "Strong" },
            new() { Value = 13.9, Color = Color.FromArgb(255, 152, 0), Description = "High" },
            new() { Value = 17.2, Color = Color.FromArgb(251, 140, 0), Description = "Gale" },
            new() { Value = 20.8, Color = Color.FromArgb(244, 81, 30), Description = "Severe" },
            new() { Value = 24.5, Color = Color.FromArgb(229, 57, 53), Description = "Storm" },
            new() { Value = 28.5, Color = Color.FromArgb(198, 40, 40), Description = "Violent" },
            new() { Value = 32.7, Color = Color.FromArgb(74, 20, 140), Description = "Hurricane" }
        ]
    };

    /// <summary>
    /// Creates the pressure scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates the sky brightness scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates the sky quality scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ColorScheme CreateSkyQualityScheme() => new() {
        Name = "Sky Quality (SQM)",
        IsGradient = true,
        Stops =
        [
            new() { Value = 17.5, Color = Color.FromArgb(198, 40, 40), Description = "INNERCITY" },
            new() { Value = 18.0, Color = Color.FromArgb(229, 57, 53), Description = "URBAN" },
            new() { Value = 18.5, Color = Color.FromArgb(251, 140, 0), Description = "SEMIURBAN" },
            new() { Value = 19.5, Color = Color.FromArgb(255, 193, 7), Description = "SEMISUBURB" },
            new() { Value = 20.4, Color = Color.FromArgb(205, 220, 57), Description = "SUBURBAN" },
            new() { Value = 21.3, Color = Color.FromArgb(139, 195, 74), Description = "SEMIRURAL" },
            new() { Value = 21.5, Color = Color.FromArgb(76, 175, 80), Description = "RURAL" },
            new() { Value = 21.7, Color = Color.FromArgb(56, 142, 60), Description = "DARK" },
            new() { Value = 22.0, Color = Color.FromArgb(15, 23, 42), Description = "PRISTINE" }
        ]
    };


    /// <summary>
    /// Creates the nelm scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ColorScheme CreateNelmScheme() => new() {
        Name = "Sky Quality (NELM)",
        IsGradient = true,
        Stops =
        [
            new() { Value = -0.2801, Color = Color.FromArgb(198, 40, 40), Description = "INNERCITY" },
            new() { Value = 0.6911, Color = Color.FromArgb(229, 57, 53), Description = "URBAN" },
            new() { Value = 1.6463, Color = Color.FromArgb(251, 140, 0), Description = "SEMIURBAN" },
            new() { Value = 3.4717, Color = Color.FromArgb(255, 193, 7), Description = "SEMISUBURB" },
            new() { Value = 4.9389, Color = Color.FromArgb(205, 220, 57), Description = "SUBURBAN" },
            new() { Value = 6.1268, Color = Color.FromArgb(139, 195, 74), Description = "SEMIRURAL" },
            new() { Value = 6.3434, Color = Color.FromArgb(76, 175, 80), Description = "RURAL" },
            new() { Value = 6.5415, Color = Color.FromArgb(56, 142, 60), Description = "DARK" },
            new() { Value = 6.8045, Color = Color.FromArgb(15, 23, 42), Description = "PRISTINE" }
        ]
    };

    /// <summary>
    /// Creates the rain rate scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates the wind gust scheme for color scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ColorScheme CreateWindGustScheme() => new() {
        Name = "Wind Gust",
        IsGradient = true,
        Stops =
        [
            new() { Value = 0.0, Color = Color.FromArgb(46, 125, 50), Description = "Calm" },
            new() { Value = 0.2, Color = Color.FromArgb(56, 142, 60), Description = "Wispy" },
            new() { Value = 1.6, Color = Color.FromArgb(76, 175, 80), Description = "Light" },
            new() { Value = 3.4, Color = Color.FromArgb(139, 195, 74), Description = "Gentle" },
            new() { Value = 5.5, Color = Color.FromArgb(205, 220, 57), Description = "Moderate" },
            new() { Value = 8.0, Color = Color.FromArgb(255, 235, 59), Description = "Fresh" },
            new() { Value = 10.8, Color = Color.FromArgb(255, 193, 7), Description = "Strong" },
            new() { Value = 13.9, Color = Color.FromArgb(255, 152, 0), Description = "High" },
            new() { Value = 17.2, Color = Color.FromArgb(251, 140, 0), Description = "Gale" },
            new() { Value = 20.8, Color = Color.FromArgb(244, 81, 30), Description = "Severe" },
            new() { Value = 24.5, Color = Color.FromArgb(229, 57, 53), Description = "Storm" },
            new() { Value = 28.5, Color = Color.FromArgb(198, 40, 40), Description = "Violent" },
            new() { Value = 32.7, Color = Color.FromArgb(74, 20, 140), Description = "Hurricane" }
        ]
    };

    #endregion Private Methods
}
