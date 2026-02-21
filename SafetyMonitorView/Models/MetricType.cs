namespace SafetyMonitorView.Models;

public enum MetricType { Temperature, Humidity, Pressure, DewPoint, CloudCover, SkyTemperature, SkyBrightness, SkyQuality, RainRate, WindSpeed, WindGust, WindDirection, StarFwhm, IsSafe }
public static class MetricTypeExtensions {

    #region Public Methods

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
    public static string GetUnit(this MetricType type) => type switch {
        MetricType.Temperature => "°C",
        MetricType.Humidity => "%",
        MetricType.Pressure => "hPa",
        MetricType.DewPoint => "°C",
        MetricType.CloudCover => "%",
        MetricType.SkyTemperature => "°C",
        MetricType.SkyBrightness => "Lux",
        MetricType.SkyQuality => "mpsas",
        MetricType.RainRate => "mm/hr",
        MetricType.WindSpeed => "m/s",
        MetricType.WindGust => "m/s",
        MetricType.WindDirection => "°",
        MetricType.StarFwhm => "arcsec",
        _ => ""
    };
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
        MetricType.IsSafe => data.IsSafeInt.HasValue
            ? data.IsSafeInt.Value
            : (data.SafePercentage.HasValue ? data.SafePercentage.Value / 100.0 : null),
        _ => null
    };

    #endregion Public Methods
}
