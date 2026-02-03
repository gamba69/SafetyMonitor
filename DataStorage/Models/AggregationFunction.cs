namespace DataStorage.Models {
    /// <summary>
    /// Aggregation functions for time-slot integration
    /// </summary>
    public enum AggregationFunction {
        /// <summary>
        /// Calculate average value
        /// </summary>
        Average,

        /// <summary>
        /// Get minimum value
        /// </summary>
        Minimum,

        /// <summary>
        /// Get maximum value
        /// </summary>
        Maximum,

        /// <summary>
        /// Calculate sum of values
        /// </summary>
        Sum,

        /// <summary>
        /// Count number of records
        /// </summary>
        Count,

        /// <summary>
        /// Get first value in time slot (ordered by timestamp ascending)
        /// </summary>
        First,

        /// <summary>
        /// Get last value in time slot (ordered by timestamp descending)
        /// </summary>
        Last
    }
}
