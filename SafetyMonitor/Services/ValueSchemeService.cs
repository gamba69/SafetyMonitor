using SafetyMonitor.Models;
using System.Text.Json;

namespace SafetyMonitor.Services;

public class ValueSchemeService {
    #region Private Fields

    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _schemesPath;

    #endregion Private Fields

    #region Public Constructors

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

    public static string GetDefaultSchemeName(MetricType metric) => metric switch {
        MetricType.Temperature => "Temperature",
        MetricType.Humidity => "Humidity",
        MetricType.Pressure => "Pressure",
        MetricType.CloudCover => "Cloud Cover",
        MetricType.SkyBrightness => "Sky Brightness",
        MetricType.SkyQuality => "Sky Quality",
        MetricType.RainRate => "Rain Rate",
        MetricType.WindSpeed => "Wind Speed",
        MetricType.WindGust => "Wind Gust",
        MetricType.WindDirection => "Wind Direction",
        MetricType.IsSafe => "Safety",
        _ => string.Empty
    };

    public void DeleteScheme(string name) {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        var path = Path.Combine(_schemesPath, $"{safeName}.json");
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }

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

    public void SaveScheme(ValueScheme scheme) {
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

    private static IEnumerable<ValueScheme> GetDefaultSchemes() {
        yield return CreateSafetyStatusScheme();
        yield return CreateTemperatureScheme();
        yield return CreateHumidityScheme();
        yield return CreatePressureScheme();
        yield return CreateCloudCoverScheme();
        yield return CreateSkyBrightnessScheme();
        yield return CreateSkyQualityScheme();
        yield return CreateRainRateScheme();
        yield return CreateWindSpeedScheme();
        yield return CreateWindGustScheme();
        yield return CreateWindDirectionScheme();
    }

    private static ValueScheme CreateSafetyStatusScheme() => new() {
        Name = "Safety",
        Descending = true,
        Stops =
        [
            new() { Value = 100, Text = "SAFE", Description = "Safe condition (100%)" },
            new() { Value = 0, Text = "UNSAFE", Description = "Unsafe or partially unsafe condition (<100%)" }
        ]
    };

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

    private static ValueScheme CreateCloudCoverScheme() => new() {
        Name = "Cloud Cover",
        Descending = true,
        Stops =
        [
            new() { Value = 90, Text = "OVERCAST", Description = "Overcast sky (90–100%)" },
            new() { Value = 60, Text = "CLOUDY", Description = "Mostly cloudy (60–90%)" },
            new() { Value = 30, Text = "PARTIAL", Description = "Partial cloudiness (30–60%)" },
            new() { Value = 10, Text = "SPARSE", Description = "Sparse clouds (10–30%)" },
            new() { Value = 0, Text = "CLEAR", Description = "Clear sky (0–10%)" }
        ]
    };

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

    private static ValueScheme CreateSkyQualityScheme() => new() {
        Name = "Sky Quality",
        Descending = true,
        Stops =
        [
            new() { Value = 21.5, Text = "EXCELLENT", Description = "Excellent dark sky (>= 21.5 mpsas)" },
            new() { Value = 20.5, Text = "GOOD", Description = "Good sky quality (20.5–21.5 mpsas)" },
            new() { Value = 19.5, Text = "FAIR", Description = "Average sky quality (19.5–20.5 mpsas)" },
            new() { Value = 18.5, Text = "POOR", Description = "Poor sky quality (18.5–19.5 mpsas)" },
            new() { Value = 0, Text = "BAD", Description = "Poor / bright sky (< 18.5 mpsas)" }
        ]
    };

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

    private static ValueScheme CreateWindSpeedScheme() => new() {
        Name = "Wind Speed",
        Descending = true,
        Stops =
        [
            new() { Value = 32.7, Text = "HURRICANE", Description = "Hurricane (> 32.7 m/s)" },
            new() { Value = 28.4, Text = "STORM", Description = "Storm (28.4–32.7 m/s)" },
            new() { Value = 13.8, Text = "STRONG", Description = "Strong (13.8–28.4 m/s)" },
            new() { Value = 8.0, Text = "FRESH", Description = "Fresh (8.0–13.8 m/s)" },
            new() { Value = 5.5, Text = "MODERATE", Description = "Moderate (5.5–7.9 m/s)" },
            new() { Value = 3.4, Text = "GENTLE", Description = "Gentle (3.4–5.4 m/s)" },
            new() { Value = 0.3, Text = "LIGHT", Description = "Light (0.3–3.3 m/s)" },
            new() { Value = 0, Text = "CALM", Description = "Calm (0–0.2 m/s)" }
        ]
    };

    private static ValueScheme CreateWindGustScheme() => new() {
        Name = "Wind Gust",
        Descending = true,
        Stops =
        [
            new() { Value = 20, Text = "BLAST", Description = "Blast (> 20 m/s)" },
            new() { Value = 12, Text = "SQUALL", Description = "Squall (12–20 m/s)" },
            new() { Value = 8, Text = "GUST", Description = "Gust (8–12 m/s)" },
            new() { Value = 5, Text = "BREEZE", Description = "Breeze (5–8 m/s)" },
            new() { Value = 2, Text = "PUFF", Description = "Puff (2–5 m/s)" },
            new() { Value = 0, Text = "BREATH", Description = "Breath (from 0 m/s)" }
        ]
    };
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
