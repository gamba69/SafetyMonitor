namespace SafetyMonitor.Models;

public enum MetricType { Temperature, Apparent, Humidity, Pressure, DewPoint, CloudCover, SkyTemperature, SkyBrightness, SkyQualitySQM, SkyQualityNELM, RainRate, WindSpeed, WindGust, WindDirection, StarFwhm, IsSafe }
/// <summary>
/// Represents metric type extensions and encapsulates its related behavior and state.
/// </summary>
public static class MetricTypeExtensions {

    #region Private Fields

    private static readonly Dictionary<MetricType, DerivedMetricDefinition> DerivedMetrics =
        new() {
            {
                MetricType.SkyQualityNELM,
                new DerivedMetricDefinition([MetricType.SkyQualitySQM], static values => {
                    if (!values.TryGetValue(MetricType.SkyQualitySQM, out var sqm) || !sqm.HasValue || double.IsNaN(sqm.Value)) {
                        return null;
                    }

                    // NELM ≈ 7.93 − 5 × log10(10^((21.58 − SQM)/2.5) + 1)
                    var nelm = 7.93 - (5 * Math.Log10(Math.Pow(10, (21.58 - sqm.Value) / 2.5) + 1));
                    return double.IsFinite(nelm) ? nelm : null;
                })
            },
            {
                MetricType.Apparent,
                new DerivedMetricDefinition([MetricType.Temperature, MetricType.Humidity, MetricType.WindSpeed], static values => {
                    if (!values.TryGetValue(MetricType.Temperature, out var temperature)
                        || !temperature.HasValue
                        || double.IsNaN(temperature.Value)
                        || !values.TryGetValue(MetricType.Humidity, out var humidity)
                        || !humidity.HasValue
                        || double.IsNaN(humidity.Value)
                        || !values.TryGetValue(MetricType.WindSpeed, out var windSpeed)
                        || !windSpeed.HasValue
                        || double.IsNaN(windSpeed.Value)) {
                        return null;
                    }

                    // E = 6.105 × exp(17.27 × Ta / (237.7 + Ta))
                    var saturationVaporPressure = 6.105 * Math.Exp((17.27 * temperature.Value) / (237.7 + temperature.Value));
                    // e = rh / 100 × E
                    var vaporPressure = (humidity.Value / 100d) * saturationVaporPressure;
                    // AT = Ta + 0.33 × e − 0.70 × ws − 4.00
                    var apparentTemperature = temperature.Value + (0.33 * vaporPressure) - (0.70 * windSpeed.Value) - 4.00;

                    return double.IsFinite(apparentTemperature) ? apparentTemperature : null;
                })
            }
        };

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Gets the display name for metric type extensions.
    /// </summary>
    /// <param name="type">Input value for type.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetDisplayName(this MetricType type) => type switch {
        MetricType.Temperature => "Temperature",
        MetricType.Apparent => "Apparent",
        MetricType.Humidity => "Humidity",
        MetricType.Pressure => "Pressure",
        MetricType.DewPoint => "Dew Point",
        MetricType.CloudCover => "Cloud Cover",
        MetricType.SkyTemperature => "Sky Temperature",
        MetricType.SkyBrightness => "Sky Brightness",
        MetricType.SkyQualitySQM => "Sky Quality (SQM)",
        MetricType.SkyQualityNELM => "Sky Quality (NELM)",
        MetricType.RainRate => "Rain Rate",
        MetricType.WindSpeed => "Wind Speed",
        MetricType.WindGust => "Wind Gust",
        MetricType.WindDirection => "Wind Direction",
        MetricType.StarFwhm => "Star FWHM",
        MetricType.IsSafe => "Safety",
        _ => type.ToString()
    };

    /// <summary>
    /// Gets the short name for metric type extensions.
    /// </summary>
    /// <param name="type">Input value for type.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetShortName(this MetricType type) => type switch {
        MetricType.Temperature => "TEMP",
        MetricType.Apparent => "APPT",
        MetricType.Humidity => "RH",
        MetricType.Pressure => "PRES",
        MetricType.DewPoint => "DEW",
        MetricType.CloudCover => "CC",
        MetricType.SkyTemperature => "SKYT",
        MetricType.SkyBrightness => "SKYB",
        MetricType.SkyQualitySQM => "SQM",
        MetricType.SkyQualityNELM => "NELM",
        MetricType.RainRate => "RAIN",
        MetricType.WindSpeed => "WSPD",
        MetricType.WindGust => "WGST",
        MetricType.WindDirection => "WDIR",
        MetricType.StarFwhm => "FWHM",
        MetricType.IsSafe => "SAFE",
        _ => type.ToString().ToUpperInvariant()
    };
    /// <summary>
    /// Gets the unit for metric type extensions.
    /// </summary>
    /// <param name="type">Input value for type.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetUnit(this MetricType type) => type switch {
        MetricType.Temperature => "°C",
        MetricType.Apparent => "°C",
        MetricType.Humidity => "%",
        MetricType.Pressure => "hPa",
        MetricType.DewPoint => "°C",
        MetricType.CloudCover => "%",
        MetricType.SkyTemperature => "°C",
        MetricType.SkyBrightness => "lux",
        MetricType.SkyQualitySQM => "mpsas",
        MetricType.SkyQualityNELM => "mag",
        MetricType.RainRate => "mm/hr",
        MetricType.WindSpeed => "m/s",
        MetricType.WindGust => "m/s",
        MetricType.WindDirection => "°",
        MetricType.StarFwhm => "arcsec",
        MetricType.IsSafe => "%",
        _ => ""
    };
    /// <summary>
    /// Gets the value for metric type extensions.
    /// </summary>
    /// <param name="type">Input value for type.</param>
    /// <param name="data">Input value for data.</param>
    /// <returns>The result of the operation.</returns>
    public static double? GetValue(this MetricType type, DataStorage.Models.ObservingData data) {
        if (DerivedMetrics.TryGetValue(type, out var definition)) {
            var values = new Dictionary<MetricType, double?>(definition.Dependencies.Length);
            foreach (var dependency in definition.Dependencies) {
                values[dependency] = GetRawValue(dependency, data);
            }

            return definition.Formula(values);
        }

        return GetRawValue(type, data);
    }

    #endregion Public Methods

    #region Private Methods

    private static double? GetRawValue(MetricType type, DataStorage.Models.ObservingData data) => type switch {
        MetricType.Temperature => data.Temperature,
        MetricType.Humidity => data.Humidity,
        MetricType.Pressure => data.Pressure,
        MetricType.DewPoint => data.DewPoint,
        MetricType.CloudCover => data.CloudCover,
        MetricType.SkyTemperature => data.SkyTemperature,
        MetricType.SkyBrightness => data.SkyBrightness,
        MetricType.SkyQualitySQM => data.SkyQuality,
        MetricType.RainRate => data.RainRate,
        MetricType.WindSpeed => data.WindSpeed,
        MetricType.WindGust => data.WindGust,
        MetricType.WindDirection => data.WindDirection,
        MetricType.StarFwhm => data.StarFwhm,
        MetricType.IsSafe => data.IsSafeInt ?? data.SafePercentage,
        _ => null
    };

    #endregion Private Methods

    #region Private Types

    private sealed record DerivedMetricDefinition(
        MetricType[] Dependencies,
        Func<IReadOnlyDictionary<MetricType, double?>, double?> Formula);

    #endregion Private Types
}
