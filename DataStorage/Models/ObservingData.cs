using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataStorage.Models {
    /// <summary>
    /// Data model for ASCOM ObservingConditions and SafetyMonitor
    /// Can represent both raw observations and aggregated data
    /// Maps directly to DATA table in FireBird database
    /// </summary>
    [Table("DATA")]
    public class ObservingData {
        /// <summary>
        /// Unique identifier (auto-generated, only for raw data)
        /// </summary>
        [Key]
        [Column("ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// Timestamp of the observation (for raw data) or start of time slot (for aggregated data)
        /// </summary>
        [Column("TIMESTAMP")]
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// End of time slot (only for aggregated data, null for raw data)
        /// </summary>
        [NotMapped]
        public DateTime? TimestampEnd { get; set; }

        /// <summary>
        /// Number of records (only for aggregated data, 1 for raw data)
        /// </summary>
        [NotMapped]
        public int RecordCount { get; set; } = 1;

        /// <summary>
        /// Indicates if this is aggregated data
        /// </summary>
        [NotMapped]
        public bool IsAggregated => TimestampEnd.HasValue;

        // ObservingConditions properties

        /// <summary>
        /// Cloud cover (%) - 0 = clear, 100 = completely overcast
        /// </summary>
        [Column("CLOUD_COVER")]
        public double? CloudCover { get; set; }

        /// <summary>
        /// Dew point (°C)
        /// </summary>
        [Column("DEW_POINT")]
        public double? DewPoint { get; set; }

        /// <summary>
        /// Humidity (%)
        /// </summary>
        [Column("HUMIDITY")]
        public double? Humidity { get; set; }

        /// <summary>
        /// Atmospheric pressure (hPa)
        /// </summary>
        [Column("PRESSURE")]
        public double? Pressure { get; set; }

        /// <summary>
        /// Rain rate (mm/hr)
        /// </summary>
        [Column("RAIN_RATE")]
        public double? RainRate { get; set; }

        /// <summary>
        /// Sky brightness (Lux)
        /// </summary>
        [Column("SKY_BRIGHTNESS")]
        public double? SkyBrightness { get; set; }

        /// <summary>
        /// Sky quality (mag/arcsec²)
        /// </summary>
        [Column("SKY_QUALITY")]
        public double? SkyQuality { get; set; }

        /// <summary>
        /// Sky temperature (°C)
        /// </summary>
        [Column("SKY_TEMPERATURE")]
        public double? SkyTemperature { get; set; }

        /// <summary>
        /// Star full width half maximum (arcsec)
        /// </summary>
        [Column("STAR_FWHM")]
        public double? StarFwhm { get; set; }

        /// <summary>
        /// Temperature (°C)
        /// </summary>
        [Column("TEMPERATURE")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Wind direction (degrees, 0 = North, 90 = East)
        /// </summary>
        [Column("WIND_DIRECTION")]
        public double? WindDirection { get; set; }

        /// <summary>
        /// Wind gust (m/s)
        /// </summary>
        [Column("WIND_GUST")]
        public double? WindGust { get; set; }

        /// <summary>
        /// Wind speed (m/s)
        /// </summary>
        [Column("WIND_SPEED")]
        public double? WindSpeed { get; set; }

        // SafetyMonitor properties

        /// <summary>
        /// Safety monitor safe status (for raw data)
        /// For aggregated data, use SafePercentage instead
        /// Stored as INTEGER in database (0/1/null)
        /// </summary>
        [Column("IS_SAFE")]
        public int? IsSafeInt { get; set; }

        /// <summary>
        /// Safety monitor safe status (C# bool wrapper)
        /// </summary>
        [NotMapped]
        public bool? IsSafe {
            get => IsSafeInt.HasValue ? IsSafeInt.Value == 1 : null;
            set => IsSafeInt = value.HasValue ? (value.Value ? 1 : 0) : null;
        }

        /// <summary>
        /// Percentage of records where IsSafe was true (0-100)
        /// Only for aggregated data, null for raw data
        /// </summary>
        [NotMapped]
        public double? SafePercentage { get; set; }

        /// <summary>
        /// Additional notes or metadata
        /// </summary>
        [Column("NOTES")]
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
