namespace SafetyMonitor.Models;

public enum MetricType { Temperature, Humidity, Pressure, DewPoint, CloudCover, SkyTemperature, SkyBrightness, SkyQuality, RainRate, WindSpeed, WindGust, WindDirection, StarFwhm, IsSafe }
/// <summary>
/// Represents metric type extensions and encapsulates its related behavior and state.
/// </summary>
public static class MetricTypeExtensions {

    #region Public Methods

    /// <summary>
    /// Gets the display name for metric type extensions.
    /// </summary>
    /// <param name="type">Input value for type.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetDisplayName(this MetricType type) => type switch {
        MetricType.Temperature => "Temperature",
        MetricType.Humidity => "Humidity",
        MetricType.Pressure => "Pressure",
        MetricType.DewPoint => "Dew Point",
        MetricType.CloudCover => "Cloud Cover",
        MetricType.SkyTemperature => "Sky Temperature",
        MetricType.SkyBrightness => "Sky Brightness",
        MetricType.SkyQuality => "Sky Quality",
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
        MetricType.Humidity => "RH",
        MetricType.Pressure => "PRES",
        MetricType.DewPoint => "DEW",
        MetricType.CloudCover => "CC",
        MetricType.SkyTemperature => "SKYT",
        MetricType.SkyBrightness => "SKYB",
        MetricType.SkyQuality => "SQM",
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
        MetricType.Humidity => "%",
        MetricType.Pressure => "hPa",
        MetricType.DewPoint => "°C",
        MetricType.CloudCover => "%",
        MetricType.SkyTemperature => "°C",
        MetricType.SkyBrightness => "lux",
        MetricType.SkyQuality => "mpsas",
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
    public static double? GetValue(this MetricType type, DataStorage.Models.ObservingData data) => type switch {
        MetricType.Temperature => data.Temperature,
        MetricType.Humidity => data.Humidity,
        MetricType.Pressure => data.Pressure,
        MetricType.DewPoint => data.DewPoint,
        MetricType.CloudCover => data.CloudCover,
        MetricType.SkyTemperature => data.SkyTemperature,
        MetricType.SkyBrightness => data.SkyBrightness,
        MetricType.SkyQuality => data.SkyQuality,
        MetricType.RainRate => data.RainRate,
        MetricType.WindSpeed => data.WindSpeed,
        MetricType.WindGust => data.WindGust,
        MetricType.WindDirection => data.WindDirection,
        MetricType.StarFwhm => data.StarFwhm,
        MetricType.IsSafe => data.IsSafeInt ?? data.SafePercentage,
        _ => null
    };

    #endregion Public Methods
}
