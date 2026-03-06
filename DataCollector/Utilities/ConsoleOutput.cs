using DataStorage.Models;

namespace DataCollector.Utilities;

/// <summary>
/// Represents console output and encapsulates its related behavior and state.
/// </summary>
public static class ConsoleOutput {

    #region Public Methods

    /// <summary>
    /// Executes error as part of console output processing.
    /// </summary>
    /// <param name="message">Input value for message.</param>
    public static void Error(string message) {
        Console.Error.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}");
    }

    /// <summary>
    /// Executes info as part of console output processing.
    /// </summary>
    /// <param name="message">Input value for message.</param>
    public static void Info(string message) {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}");
    }

    /// <summary>
    /// Executes print data as part of console output processing.
    /// </summary>
    /// <param name="data">Input value for data.</param>
    public static void PrintData(ObservingData data) {
        Console.WriteLine($"[{data.Timestamp:yyyy-MM-dd HH:mm:ss} UTC] DATA:");

        // Temperature and humidity
        if (data.Temperature.HasValue) {
            Console.WriteLine($"  Temperature:      {data.Temperature:F2}°C");
        }

        if (data.Humidity.HasValue) {
            Console.WriteLine($"  Humidity:         {data.Humidity:F1}%");
        }

        if (data.DewPoint.HasValue) {
            Console.WriteLine($"  Dew Point:        {data.DewPoint:F2}°C");
        }

        // Pressure
        if (data.Pressure.HasValue) {
            Console.WriteLine($"  Pressure:         {data.Pressure:F2} hPa");
        }

        // Sky conditions
        if (data.CloudCover.HasValue) {
            Console.WriteLine($"  Cloud Cover:      {data.CloudCover:F1}%");
        }

        if (data.SkyTemperature.HasValue) {
            Console.WriteLine($"  Sky Temperature:  {data.SkyTemperature:F2}°C");
        }

        if (data.SkyBrightness.HasValue) {
            Console.WriteLine($"  Sky Brightness:   {data.SkyBrightness:F2} lux");
        }

        if (data.SkyQuality.HasValue) {
            Console.WriteLine($"  Sky Quality:      {data.SkyQuality:F2} mpsas");
        }

        // Rain
        if (data.RainRate.HasValue) {
            Console.WriteLine($"  Rain Rate:        {data.RainRate:F2} mm/hr");
        }

        // Wind
        if (data.WindSpeed.HasValue) {
            Console.WriteLine($"  Wind Speed:       {data.WindSpeed:F2} m/s");
        }

        if (data.WindGust.HasValue) {
            Console.WriteLine($"  Wind Gust:        {data.WindGust:F2} m/s");
        }

        if (data.WindDirection.HasValue) {
            Console.WriteLine($"  Wind Direction:   {data.WindDirection:F0}° ({GetWindDirectionName(data.WindDirection.Value)})");
        }

        // Star seeing
        if (data.StarFwhm.HasValue) {
            Console.WriteLine($"  Star FWHM:        {data.StarFwhm:F2} arcsec");
        }

        // Safety - without color
        if (data.IsSafe.HasValue) {
            var safetyText = data.IsSafe.Value ? "SAFE" : "UNSAFE";
            Console.WriteLine($"  Safety Status:    {safetyText}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Executes warning as part of console output processing.
    /// </summary>
    /// <param name="message">Input value for message.</param>
    public static void Warning(string message) {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Gets the wind direction name for console output.
    /// </summary>
    /// <param name="degrees">Input value for degrees.</param>
    /// <returns>The resulting string value.</returns>
    private static string GetWindDirectionName(double degrees) {
        // Normalize to 0-360
        degrees %= 360;
        if (degrees < 0) {
            degrees += 360;
        }

        return degrees switch {
            >= 337.5 or < 22.5 => "N",
            >= 22.5 and < 67.5 => "NE",
            >= 67.5 and < 112.5 => "E",
            >= 112.5 and < 157.5 => "SE",
            >= 157.5 and < 202.5 => "S",
            >= 202.5 and < 247.5 => "SW",
            >= 247.5 and < 292.5 => "W",
            >= 292.5 and < 337.5 => "NW",
            _ => "?"
        };
    }

    #endregion Private Methods
}
