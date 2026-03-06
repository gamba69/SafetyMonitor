namespace DataStorage.Models {
    /// <summary>
    /// Represents observing data and encapsulates its related behavior and state.
    /// </summary>
    public class ObservingData {

        #region Public Properties

        /// <summary>
        /// Gets or sets the cloud cover for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? CloudCover { get; set; }

        // ObservingConditions properties
        /// <summary>
        /// Gets or sets the dew point for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? DewPoint { get; set; }

        /// <summary>
        /// Gets or sets the humidity for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? Humidity { get; set; }

        /// <summary>
        /// Gets or sets the id for observing data. Identifies the related entity and is used for lookups, linking, or persistence.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Indicates if this is aggregated data.
        /// </summary>
        public bool IsAggregated => TimestampEnd.HasValue;

        /// <summary>
        /// Safety monitor safe status (C# bool wrapper).
        /// </summary>
        public bool? IsSafe {
            get => IsSafeInt.HasValue ? IsSafeInt.Value == 100 : null;
            set => IsSafeInt = value.HasValue ? (value.Value ? 100 : 0) : null;
        }

        /// <summary>
        /// Gets or sets the is safe int for observing data. Represents a state flag that enables or disables related behavior.
        /// </summary>
        public int? IsSafeInt { get; set; }

        /// <summary>
        /// Gets or sets the notes for observing data. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the pressure for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? Pressure { get; set; }

        /// <summary>
        /// Gets or sets the rain rate for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? RainRate { get; set; }

        /// <summary>
        /// Gets or sets the record count for observing data. Specifies sizing or boundary constraints used by runtime calculations.
        /// </summary>
        public int RecordCount { get; set; } = 1;

        // SafetyMonitor properties
        /// <summary>
        /// Gets or sets the safe percentage for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? SafePercentage { get; set; }

        /// <summary>
        /// Gets or sets the sky brightness for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? SkyBrightness { get; set; }

        /// <summary>
        /// Gets or sets the sky quality for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? SkyQuality { get; set; }

        /// <summary>
        /// Gets or sets the sky temperature for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? SkyTemperature { get; set; }

        /// <summary>
        /// Gets or sets the star fwhm for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? StarFwhm { get; set; }

        /// <summary>
        /// Gets or sets the temperature for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Gets or sets the timestamp for observing data. Stores a timestamp used for ordering, filtering, or range calculations.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the timestamp end for observing data. Stores a timestamp used for ordering, filtering, or range calculations.
        /// </summary>
        public DateTime? TimestampEnd { get; set; }
        /// <summary>
        /// Gets or sets the wind direction for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? WindDirection { get; set; }

        /// <summary>
        /// Gets or sets the wind gust for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? WindGust { get; set; }

        /// <summary>
        /// Gets or sets the wind speed for observing data. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double? WindSpeed { get; set; }

        #endregion Public Properties
    }
}
