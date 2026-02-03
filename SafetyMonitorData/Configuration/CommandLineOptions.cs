namespace SafetyMonitorData.Configuration;

/// <summary>
/// Command line options for ASCOM Alpaca Data Collector
/// </summary>
public class CommandLineOptions {
    // ========== ObservingConditions Device ==========

    /// <summary>
    /// ObservingConditions device name for discovery
    /// </summary>
    public string? OcName { get; set; }

    /// <summary>
    /// ObservingConditions device IP address
    /// </summary>
    public string? OcAddress { get; set; }

    /// <summary>
    /// ObservingConditions device port
    /// </summary>
    public int? OcPort { get; set; }

    /// <summary>
    /// ObservingConditions device number (default: 0)
    /// </summary>
    public int OcDeviceNumber { get; set; } = 0;

    // ========== SafetyMonitor Device ==========

    /// <summary>
    /// SafetyMonitor device name for discovery
    /// </summary>
    public string? SmName { get; set; }

    /// <summary>
    /// SafetyMonitor device IP address
    /// </summary>
    public string? SmAddress { get; set; }

    /// <summary>
    /// SafetyMonitor device port
    /// </summary>
    public int? SmPort { get; set; }

    /// <summary>
    /// SafetyMonitor device number (default: 0)
    /// </summary>
    public int SmDeviceNumber { get; set; } = 0;

    // ========== Retry Settings ==========

    /// <summary>
    /// Number of discovery retry attempts (default: 3)
    /// </summary>
    public int DiscoveryRetries { get; set; } = 3;

    /// <summary>
    /// Number of data retrieval retry attempts (default: 3)
    /// </summary>
    public int DataRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in milliseconds (default: 1000)
    /// </summary>
    public int RetryDelay { get; set; } = 1000;

    // ========== Output Settings ==========

    /// <summary>
    /// Suppress data output to console
    /// </summary>
    public bool Quiet { get; set; }

    // ========== Database Settings ==========

    /// <summary>
    /// Path to database storage root directory
    /// </summary>
    public string? StoragePath { get; set; }

    /// <summary>
    /// Database user name (default: SYSDBA)
    /// </summary>
    public string? DbUser { get; set; } = "SYSDBA";

    /// <summary>
    /// Database password (default: masterkey)
    /// </summary>
    public string? DbPassword { get; set; } = "masterkey";

    // ========== Continuous Mode Settings ==========

    /// <summary>
    /// Enable continuous data collection mode
    /// </summary>
    public bool Continuous { get; set; }

    /// <summary>
    /// Interval between data collections in seconds (default: 3)
    /// </summary>
    public int Interval { get; set; } = 3;

    /// <summary>
    /// Delay in seconds after fatal error in continuous mode (default: 30)
    /// </summary>
    public int ErrorRetryDelay { get; set; } = 30;

    // ========== Validation Methods ==========

    /// <summary>
    /// Validate that ObservingConditions device configuration is valid
    /// </summary>
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
    /// Validate that SafetyMonitor device configuration is valid
    /// </summary>
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

    /// <summary>
    /// Check if ObservingConditions uses discovery (name-based)
    /// </summary>
    public bool OcUsesDiscovery => !string.IsNullOrWhiteSpace(OcName);

    /// <summary>
    /// Check if SafetyMonitor uses discovery (name-based)
    /// </summary>
    public bool SmUsesDiscovery => !string.IsNullOrWhiteSpace(SmName);
}
