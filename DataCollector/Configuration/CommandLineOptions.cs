namespace DataCollector.Configuration;

/// <summary>
/// Represents command line options and encapsulates its related behavior and state.
/// </summary>
public class CommandLineOptions {
    // ObservingConditions device options.

    #region Public Properties

    /// <summary>
    /// Gets or sets the continuous for command line options. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool Continuous { get; set; }

    /// <summary>
    /// Gets or sets the data retries for command line options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int DataRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the db password for command line options. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string? DbPassword { get; set; } = "masterkey";

    /// <summary>
    /// Gets or sets the db user for command line options. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string? DbUser { get; set; } = "SYSDBA";

    /// <summary>
    /// Gets or sets the discovery retries for command line options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int DiscoveryRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the error retry delay for command line options. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int ErrorRetryDelay { get; set; } = 30;

    // Continuous mode options.
    /// <summary>
    /// Gets or sets the interval for command line options. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int Interval { get; set; } = 3;

    /// <summary>
    /// Gets or sets the oc address for command line options. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string? OcAddress { get; set; }

    /// <summary>
    /// Gets or sets the oc device number for command line options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int OcDeviceNumber { get; set; } = 0;

    /// <summary>
    /// Gets or sets the oc name for command line options. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string? OcName { get; set; }
    /// <summary>
    /// Gets or sets the oc port for command line options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int? OcPort { get; set; }
    // SafetyMonitor device options.

    /// <summary>
    /// Check if ObservingConditions uses discovery (name-based).
    /// </summary>
    public bool OcUsesDiscovery => !string.IsNullOrWhiteSpace(OcName);

    /// <summary>
    /// Gets or sets the quiet for command line options. Represents a state flag that enables or disables related behavior.
    /// </summary>
    public bool Quiet { get; set; }

    // Retry options.
    /// <summary>
    /// Gets or sets the retry delay for command line options. Defines timing behavior that affects refresh cadence, scheduling, or time-window processing.
    /// </summary>
    public int RetryDelay { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the sm address for command line options. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string? SmAddress { get; set; }

    /// <summary>
    /// Gets or sets the sm device number for command line options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int SmDeviceNumber { get; set; } = 0;

    /// <summary>
    /// Gets or sets the sm name for command line options. Stores textual configuration or display metadata used by application flows.
    /// </summary>
    public string? SmName { get; set; }
    /// <summary>
    /// Gets or sets the sm port for command line options. Stores a numeric value used by calculations, thresholds, or telemetry display.
    /// </summary>
    public int? SmPort { get; set; }
    // Output options.
    // Database options.

    /// <summary>
    /// Check if SafetyMonitor uses discovery (name-based).
    /// </summary>
    public bool SmUsesDiscovery => !string.IsNullOrWhiteSpace(SmName);

    /// <summary>
    /// Gets or sets the storage path for command line options. Specifies a filesystem location used to load or persist application data.
    /// </summary>
    public string? StoragePath { get; set; }

    #endregion Public Properties

    // Validation methods.

    #region Public Methods

    /// <summary>
    /// Determines whether has valid oc configuration for command line options.
    /// </summary>
    /// <param name="error">Input value for error.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    public bool HasValidOcConfiguration(out string? error) {
        // Must have either name OR (address AND port)
        bool hasName = !string.IsNullOrWhiteSpace(OcName);
        bool hasAddress = !string.IsNullOrWhiteSpace(OcAddress) && OcPort.HasValue;

        if (!hasName && !hasAddress) {
            error = "ObservingConditions device must be specified either by name (--oc-name) or by address and port (--oc-address and --oc-port)";
            return false;
        }

        if (hasName && hasAddress) {
            error = "ObservingConditions device cannot be specified by both name and address - choose one method";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Determines whether has valid sm configuration for command line options.
    /// </summary>
    /// <param name="error">Input value for error.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    public bool HasValidSmConfiguration(out string? error) {
        // Must have either name OR (address AND port)
        bool hasName = !string.IsNullOrWhiteSpace(SmName);
        bool hasAddress = !string.IsNullOrWhiteSpace(SmAddress) && SmPort.HasValue;

        if (!hasName && !hasAddress) {
            error = "SafetyMonitor device must be specified either by name (--sm-name) or by address and port (--sm-address and --sm-port)";
            return false;
        }

        if (hasName && hasAddress) {
            error = "SafetyMonitor device cannot be specified by both name and address - choose one method";
            return false;
        }

        error = null;
        return true;
    }

    #endregion Public Methods
}
