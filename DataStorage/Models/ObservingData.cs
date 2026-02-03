using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataStorage.Models {
    /// <summary>
    /// Data model for ASCOM ObservingConditions and SafetyMonitor
    /// Can represent both raw observations and aggregated data
    /// </summary>
    public class ObservingData {
        /// <summary>
        /// Unique identifier (auto-generated, only for raw data)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Timestamp of the observation (for raw data) or start of time slot (for aggregated data)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// End of time slot (only for aggregated data, null for raw data)
        /// </summary>
        public DateTime? TimestampEnd { get; set; }

        /// <summary>
        /// Number of records (only for aggregated data, 1 for raw data)
        /// </summary>
        public int RecordCount { get; set; } = 1;

        /// <summary>
        /// Indicates if this is aggregated data
        /// </summary>
        public bool IsAggregated => TimestampEnd.HasValue;

        // ObservingConditions properties

        /// <summary>
        /// Cloud cover (%) - 0 = clear, 100 = completely overcast
        /// </summary>
        public double? CloudCover { get; set; }

        /// <summary>
        /// Dew point (°C)
        /// </summary>
        public double? DewPoint { get; set; }

        /// <summary>
        /// Humidity (%)
        /// </summary>
        public double? Humidity { get; set; }

        /// <summary>
        /// Atmospheric pressure (hPa)
        /// </summary>
        public double? Pressure { get; set; }

        /// <summary>
        /// Rain rate (mm/hr)
        /// </summary>
        public double? RainRate { get; set; }

        /// <summary>
        /// Sky brightness (Lux)
        /// </summary>
        public double? SkyBrightness { get; set; }

        /// <summary>
        /// Sky quality (mag/arcsec²)
        /// </summary>
        public double? SkyQuality { get; set; }

        /// <summary>
        /// Sky temperature (°C)
        /// </summary>
        public double? SkyTemperature { get; set; }

        /// <summary>
        /// Star full width half maximum (arcsec)
        /// </summary>
        public double? StarFwhm { get; set; }

        /// <summary>
        /// Temperature (°C)
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Wind direction (degrees, 0 = North, 90 = East)
        /// </summary>
        public double? WindDirection { get; set; }

        /// <summary>
        /// Wind gust (m/s)
        /// </summary>
        public double? WindGust { get; set; }

        /// <summary>
        /// Wind speed (m/s)
        /// </summary>
        public double? WindSpeed { get; set; }

        // SafetyMonitor properties

        /// <summary>
        /// Safety monitor safe status (for raw data)
        /// For aggregated data, use SafePercentage instead
        /// Stored as INTEGER in database (0/1/null)
        /// </summary>
        public int? IsSafeInt { get; set; }

        /// <summary>
        /// Safety monitor safe status (C# bool wrapper)
        /// </summary>
        public bool? IsSafe {
            get => IsSafeInt.HasValue ? IsSafeInt.Value == 1 : null;
            set => IsSafeInt = value.HasValue ? (value.Value ? 1 : 0) : null;
        }

        /// <summary>
        /// Percentage of records where IsSafe was true (0-100)
        /// Only for aggregated data, null for raw data
        /// </summary>
        public double? SafePercentage { get; set; }

        /// <summary>
        /// Additional notes or metadata
        /// </summary>
        public string? Notes { get; set; }
    }
}
