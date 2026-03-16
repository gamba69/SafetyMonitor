using SafetyMonitor.Models;
using System.Text.Json;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents value scheme service and encapsulates its related behavior and state.
/// </summary>
public class ValueSchemeService {
    #region Private Fields

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _schemesPath;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSchemeService"/> class.
    /// </summary>
    public ValueSchemeService() {
        _schemesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SafetyMonitor", "ValueSchemes");
        Directory.CreateDirectory(_schemesPath);

        _jsonOptions = new JsonSerializerOptions {
            WriteIndented = true
        };

        EnsureDefaultSchemesExist();
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Gets the default scheme name for value scheme service.
    /// </summary>
    /// <param name="metric">Input value for metric.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetDefaultSchemeName(MetricType metric) => metric switch {
        MetricType.Temperature => "Temperature",
        MetricType.Humidity => "Humidity",
        MetricType.Pressure => "Pressure",
        MetricType.CloudCover => "Cloud Cover",
        MetricType.SkyBrightness => "Sky Brightness",
        MetricType.SkyQuality => "Sky Quality (SQM)",
        MetricType.Nelm => "Naked Eye (NELM)",
        MetricType.RainRate => "Rain Rate",
        MetricType.WindSpeed => "Wind Speed",
        MetricType.WindGust => "Wind Gust",
        MetricType.WindDirection => "Wind Direction",
        MetricType.IsSafe => "Safety",
        _ => string.Empty
    };

    /// <summary>
    /// Deletes the scheme for value scheme service.
    /// </summary>
    /// <param name="name">Input value for name.</param>
    public void DeleteScheme(string name) {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Loads the schemes for value scheme service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public List<ValueScheme> LoadSchemes() {
        var schemes = new List<ValueScheme>();

        foreach (var file in Directory.GetFiles(_schemesPath, "*.json")) {
            try {
                var json = File.ReadAllText(file);
                var scheme = JsonSerializer.Deserialize<ValueScheme>(json, _jsonOptions);
                if (scheme != null) {
                    schemes.Add(scheme);
                }
            } catch { }
        }

        return schemes;
    }

    /// <summary>
    /// Saves the scheme for value scheme service.
    /// </summary>
    /// <param name="scheme">Input value for scheme.</param>
    public void SaveScheme(ValueScheme scheme) {
        var safeName = string.Join("_", scheme.Name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        var json = JsonSerializer.Serialize(scheme, _jsonOptions);
        File.WriteAllText(path, json);
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Ensures the default schemes exist for value scheme service.
    /// </summary>
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

    /// <summary>
    /// Gets the default schemes for value scheme service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static IEnumerable<ValueScheme> GetDefaultSchemes() {
        yield return CreateSafetyStatusScheme();
        yield return CreateTemperatureScheme();
        yield return CreateHumidityScheme();
        yield return CreatePressureScheme();
        yield return CreateCloudCoverScheme();
        yield return CreateSkyBrightnessScheme();
        yield return CreateSkyQualityScheme();
        yield return CreateNelmScheme();
        yield return CreateRainRateScheme();
        yield return CreateWindSpeedScheme();
        yield return CreateWindGustScheme();
        yield return CreateWindDirectionScheme();
    }

    /// <summary>
    /// Creates the safety status scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateSafetyStatusScheme() => new() {
        Name = "Safety",
        Descending = true,
        Stops =
        [
            new() { Value = 100, Text = "SAFE", Description = "Safe condition (100%)" },
            new() { Value = 0, Text = "UNSAFE", Description = "Unsafe or partially unsafe condition (<100%)" }
        ]
    };

    /// <summary>
    /// Creates the temperature scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateTemperatureScheme() => new() {
        Name = "Temperature",
        Descending = true,
        Stops =
        [
            new() { Value = 35, Text = "SCORCH", Description = "Scorching (> 35°C)" },
            new() { Value = 30, Text = "HOT", Description = "Hot (30–35°C)" },
            new() { Value = 22, Text = "WARM", Description = "Warm (22–30°C)" },
            new() { Value = 10, Text = "MILD", Description = "Mild (10–22°C)" },
            new() { Value = 0, Text = "COLD", Description = "Cold (0–10°C)" },
            new() { Value = -10, Text = "FREEZE", Description = "Freezing (-10–0°C)" },
            new() { Value = -20, Text = "FRIGID", Description = "Frigid (-20 to -10°C)" },
            new() { Value = -273, Text = "ARCTIC", Description = "Arctic cold (< -20°C)" }
        ]
    };

    /// <summary>
    /// Creates the humidity scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateHumidityScheme() => new() {
        Name = "Humidity",
        Descending = true,
        Stops =
        [
            new() { Value = 85, Text = "SATURATED", Description = "Very humid (> 85%)" },
            new() { Value = 70, Text = "HUMID", Description = "Humid (70–85%)" },
            new() { Value = 40, Text = "COMFORT", Description = "Comfort range (40–70%)" },
            new() { Value = 20, Text = "DRY", Description = "Dry air (20–40%)" },
            new() { Value = 0, Text = "ARID", Description = "Very dry (< 20%)" }
        ]
    };

    /// <summary>
    /// Creates the pressure scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreatePressureScheme() => new() {
        Name = "Pressure",
        Descending = true,
        Stops =
        [
            new() { Value = 1030, Text = "EXTREME", Description = "Extreme pressure (> 1030 hPa)" },
            new() { Value = 1015, Text = "HIGH", Description = "High pressure (1015–1030 hPa)" },
            new() { Value = 1000, Text = "NORMAL", Description = "Normal pressure (1000–1015 hPa)" },
            new() { Value = 985, Text = "LOW", Description = "Low pressure (985–1000 hPa)" },
            new() { Value = 0, Text = "STORM", Description = "Storm-like low pressure (< 985 hPa)" }
        ]
    };

    /// <summary>
    /// Creates the cloud cover scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateCloudCoverScheme() => new() {
        Name = "Cloud Cover",
        Descending = false,
        Stops =
        [
            new() { Value = 0, Text = "CLEAR", Description = "Clear sky (0%)" },
            new() { Value = 25, Text = "FEW", Description = "Few clouds (1–25%)" },
            new() { Value = 50, Text = "SCATTER", Description = "Scattered clouds (26–50%)" },
            new() { Value = 99, Text = "BROKEN", Description = "Broken clouds (51–99%)" },
            new() { Value = 100, Text = "OVERCAST", Description = "Overcast sky (100%)" }
        ]
    };

    /// <summary>
    /// Creates the sky brightness scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateSkyBrightnessScheme() => new() {
        Name = "Sky Brightness",
        Descending = true,
        Stops =
        [
            new() { Value = 20000, Text = "DAYLIGHT", Description = "Daylight level (> 20000 lux)" },
            new() { Value = 1000, Text = "TWILIGHT", Description = "Twilight/bright moonlight (1000–20000 lux)" },
            new() { Value = 50, Text = "DUSK", Description = "Dusk range (50–1000 lux)" },
            new() { Value = 1, Text = "DARK", Description = "Dark sky (1–50 lux)" },
            new() { Value = 0, Text = "NIGHT", Description = "Very dark night (< 1 lux)" }
        ]
    };

    /// <summary>
    /// Creates the sky quality scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateSkyQualityScheme() => new() {
        Name = "Sky Quality (SQM)",
        Descending = false,
        Stops =
        [
            new() { Value = 17.5, Text = "INNERCITY", Description = "Inner-city sky quality (<= 17.5 mpsas)" },
            new() { Value = 18.0, Text = "URBAN", Description = "Urban sky quality (17.5–18.0 mpsas)" },
            new() { Value = 18.5, Text = "SEMIURBAN", Description = "Semiurban sky quality (18.0–18.5 mpsas)" },
            new() { Value = 19.5, Text = "SEMISUBURB", Description = "Semisuburb sky quality (18.5–19.5 mpsas)" },
            new() { Value = 20.4, Text = "SUBURBAN", Description = "Suburban sky quality (19.5–20.4 mpsas)" },
            new() { Value = 21.3, Text = "SEMIRURAL", Description = "Semirural sky quality (20.4–21.3 mpsas)" },
            new() { Value = 21.5, Text = "RURAL", Description = "Rural sky quality (21.3–21.5 mpsas)" },
            new() { Value = 21.7, Text = "DARK", Description = "Dark sky quality (21.5–21.7 mpsas)" },
            new() { Value = 22.0, Text = "PRISTINE", Description = "Pristine sky quality (>= 22.0 mpsas)" }
        ]
    };


    /// <summary>
    /// Creates the nelm scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateNelmScheme() => new() {
        Name = "Naked Eye (NELM)",
        Descending = false,
        Stops =
        [
            new() { Value = -0.2801, Text = "INNERCITY", Description = "Inner-city sky quality (<= -0.2801 NELM)" },
            new() { Value = 0.6911, Text = "URBAN", Description = "Urban sky quality (-0.2801–0.6911 NELM)" },
            new() { Value = 1.6463, Text = "SEMIURBAN", Description = "Semiurban sky quality (0.6911–1.6463 NELM)" },
            new() { Value = 3.4717, Text = "SEMISUBURB", Description = "Semisuburb sky quality (1.6463–3.4717 NELM)" },
            new() { Value = 4.9389, Text = "SUBURBAN", Description = "Suburban sky quality (3.4717–4.9389 NELM)" },
            new() { Value = 6.1268, Text = "SEMIRURAL", Description = "Semirural sky quality (4.9389–6.1268 NELM)" },
            new() { Value = 6.3434, Text = "RURAL", Description = "Rural sky quality (6.1268–6.3434 NELM)" },
            new() { Value = 6.5415, Text = "DARK", Description = "Dark sky quality (6.3434–6.5415 NELM)" },
            new() { Value = 6.8045, Text = "PRISTINE", Description = "Pristine sky quality (>= 6.8045 NELM)" }
        ]
    };

    /// <summary>
    /// Creates the rain rate scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateRainRateScheme() => new() {
        Name = "Rain Rate",
        Descending = true,
        Stops =
        [
            new() { Value = 15, Text = "DOWNPOUR", Description = "Downpour (> 15 mm/hr)" },
            new() { Value = 6, Text = "HEAVY", Description = "Heavy rain (6–15 mm/hr)" },
            new() { Value = 2, Text = "RAIN", Description = "Steady rain (2–6 mm/hr)" },
            new() { Value = 0.001, Text = "DRIZZLE", Description = "Light drizzle (0.001–2 mm/hr)" },
            new() { Value = 0, Text = "DRY", Description = "No rain (exactly 0 mm/hr)" }
        ]
    };

    /// <summary>
    /// Creates the wind speed scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateWindSpeedScheme() => new() {
        Name = "Wind Speed",
        Descending = true,
        Stops =
        [
            new() { Value = 32.7, Text = "HURRICANE", Description = "Hurricane (>= 32.7 m/s)" },
            new() { Value = 28.5, Text = "VIOLENT", Description = "Violent (>= 28.5 m/s)" },
            new() { Value = 24.5, Text = "STORM", Description = "Storm (>= 24.5 m/s)" },
            new() { Value = 20.8, Text = "SEVERE", Description = "Severe (>= 20.8 m/s)" },
            new() { Value = 17.2, Text = "GALE", Description = "Gale (>= 17.2 m/s)" },
            new() { Value = 13.9, Text = "HIGH", Description = "High (>= 13.9 m/s)" },
            new() { Value = 10.8, Text = "STRONG", Description = "Strong (>= 10.8 m/s)" },
            new() { Value = 8.0, Text = "FRESH", Description = "Fresh (>= 8.0 m/s)" },
            new() { Value = 5.5, Text = "MODERATE", Description = "Moderate (>= 5.5 m/s)" },
            new() { Value = 3.4, Text = "GENTLE", Description = "Gentle (>= 3.4 m/s)" },
            new() { Value = 1.6, Text = "LIGHT", Description = "Light (>= 1.6 m/s)" },
            new() { Value = 0.2, Text = "WISPY", Description = "Wispy (>= 0.2 m/s)" },
            new() { Value = 0.0, Text = "CALM", Description = "Calm (>= 0.0 m/s)" }
        ]
    };

    /// <summary>
    /// Creates the wind gust scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateWindGustScheme() => new() {
        Name = "Wind Gust",
        Descending = true,
        Stops =
        [
            new() { Value = 32.7, Text = "HURRICANE", Description = "Hurricane (>= 32.7 m/s)" },
            new() { Value = 28.5, Text = "VIOLENT", Description = "Violent (>= 28.5 m/s)" },
            new() { Value = 24.5, Text = "STORM", Description = "Storm (>= 24.5 m/s)" },
            new() { Value = 20.8, Text = "SEVERE", Description = "Severe (>= 20.8 m/s)" },
            new() { Value = 17.2, Text = "GALE", Description = "Gale (>= 17.2 m/s)" },
            new() { Value = 13.9, Text = "HIGH", Description = "High (>= 13.9 m/s)" },
            new() { Value = 10.8, Text = "STRONG", Description = "Strong (>= 10.8 m/s)" },
            new() { Value = 8.0, Text = "FRESH", Description = "Fresh (>= 8.0 m/s)" },
            new() { Value = 5.5, Text = "MODERATE", Description = "Moderate (>= 5.5 m/s)" },
            new() { Value = 3.4, Text = "GENTLE", Description = "Gentle (>= 3.4 m/s)" },
            new() { Value = 1.6, Text = "LIGHT", Description = "Light (>= 1.6 m/s)" },
            new() { Value = 0.2, Text = "WISPY", Description = "Wispy (>= 0.2 m/s)" },
            new() { Value = 0.0, Text = "CALM", Description = "Calm (>= 0.0 m/s)" }
        ]
    };

    /// <summary>
    /// Creates the wind direction scheme for value scheme service.
    /// </summary>
    /// <param name=")">Input value for .</param>
    /// <returns>The result of the operation.</returns>
    private static ValueScheme CreateWindDirectionScheme() => new() {
        Name = "Wind Direction",
        Descending = false,
        Stops =
        [
            new() { Value = 11.25, Text = "N", Description = "North (348.75°–11.25°)" },
            new() { Value = 33.75, Text = "NNE", Description = "North-northeast (11.25°–33.75°)" },
            new() { Value = 56.25, Text = "NE", Description = "Northeast (33.75°–56.25°)" },
            new() { Value = 78.75, Text = "ENE", Description = "East-northeast (56.25°–78.75°)" },
            new() { Value = 101.25, Text = "E", Description = "East (78.75°–101.25°)" },
            new() { Value = 123.75, Text = "ESE", Description = "East-southeast (101.25°–123.75°)" },
            new() { Value = 146.25, Text = "SE", Description = "Southeast (123.75°–146.25°)" },
            new() { Value = 168.75, Text = "SSE", Description = "South-southeast (146.25°–168.75°)" },
            new() { Value = 191.25, Text = "S", Description = "South (168.75°–191.25°)" },
            new() { Value = 213.75, Text = "SSW", Description = "South-southwest (191.25°–213.75°)" },
            new() { Value = 236.25, Text = "SW", Description = "Southwest (213.75°–236.25°)" },
            new() { Value = 258.75, Text = "WSW", Description = "West-southwest (236.25°–258.75°)" },
            new() { Value = 281.25, Text = "W", Description = "West (258.75°–281.25°)" },
            new() { Value = 303.75, Text = "WNW", Description = "West-northwest (281.25°–303.75°)" },
            new() { Value = 326.25, Text = "NW", Description = "Northwest (303.75°–326.25°)" },
            new() { Value = 348.75, Text = "NNW", Description = "North-northwest (326.25°–348.75°)" },
            new() { Value = 360, Text = "N", Description = "North (348.75°–360°)" }
        ]
    };


    #endregion Private Methods
}
