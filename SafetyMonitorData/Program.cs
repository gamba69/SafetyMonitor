using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ASCOM.Common.DeviceInterfaces;
using SafetyMonitorData.Configuration;
using SafetyMonitorData.Services;
using SafetyMonitorData.Utilities;

namespace SafetyMonitorData;

class Program {
    static async Task<int> Main(string[] args) {
        // Parse command line arguments
        var options = ParseCommandLine(args);
        if (options == null) {
            return 1;
        }

        // Validate options
        if (!ValidateOptions(options, out var errors)) {
            foreach (var error in errors) {
                ConsoleOutput.Error(error);
            }
            return 1;
        }

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, options);
        var serviceProvider = services.BuildServiceProvider();

        ConsoleOutput.Info("ASCOM Alpaca Data Collector starting...");

        // Initialize database storage if configured
        DataStorage.DataStorage? storage = null;
        if (!string.IsNullOrEmpty(options.StoragePath)) {
            try {
                storage = new DataStorage.DataStorage(
                    options.StoragePath,
                    options.DbUser,
                    options.DbPassword);

                ConsoleOutput.Info($"Database storage initialized at: {options.StoragePath}");
            } catch (Exception ex) {
                ConsoleOutput.Error($"Failed to initialize database storage: {ex.Message}");
                return 1;
            }
        } else {
            ConsoleOutput.Warning("No storage path specified - data will not be saved to database");
        }

        IObservingConditions? ocDevice = null;
        ISafetyMonitor? smDevice = null;

        // Setup cancellation token for graceful shutdown
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) => {
            eventArgs.Cancel = true;
            ConsoleOutput.Info("Shutdown requested (Ctrl+C pressed)...");
            cts.Cancel();
        };

        try {
            if (options.Continuous) {
                // Continuous mode - never give up, retry forever
                await RunContinuousModeWithRetryAsync(serviceProvider, storage, options, cts.Token);
            } else {
                // Single run mode - fail on errors
                var connectionService = serviceProvider.GetRequiredService<DeviceConnectionService>();

                ConsoleOutput.Info("Connecting to ObservingConditions device...");
                ocDevice = await connectionService.ConnectObservingConditionsAsync();
                ConsoleOutput.Info($"Connected to ObservingConditions: {ocDevice.Name}");

                ConsoleOutput.Info("Connecting to SafetyMonitor device...");
                smDevice = await connectionService.ConnectSafetyMonitorAsync();
                ConsoleOutput.Info($"Connected to SafetyMonitor: {smDevice.Name}");

                var collectionService = serviceProvider.GetRequiredService<DataCollectionService>();
                collectionService.Initialize(ocDevice, smDevice);

                await RunOnceModeAsync(collectionService, storage, options, cts.Token);

                // Disconnect devices
                DisconnectDevice(ocDevice, "ObservingConditions");
                DisconnectDevice(smDevice, "SafetyMonitor");
            }

            ConsoleOutput.Info("Data collection completed successfully");
            return 0;
        } catch (OperationCanceledException) {
            ConsoleOutput.Info("Operation cancelled by user");
            return 0;
        } catch (Exception ex) {
            ConsoleOutput.Error($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Disconnect from device safely
    /// </summary>
    static void DisconnectDevice(IAscomDevice? device, string deviceName) {
        if (device != null) {
            try {
                if (device.Connected) {
                    device.Connected = false;
                    ConsoleOutput.Info($"Disconnected from {deviceName}");
                }
            } catch (Exception) {
                // Ignore disconnect errors
            }
        }
    }

    /// <summary>
    /// Run continuous mode with automatic retry on any error
    /// </summary>
    static async Task RunContinuousModeWithRetryAsync(
        IServiceProvider serviceProvider,
        DataStorage.DataStorage? storage,
        CommandLineOptions options,
        CancellationToken cancellationToken) {

        ConsoleOutput.Info($"Starting continuous data collection (interval: {options.Interval} seconds)");
        ConsoleOutput.Info($"Error retry delay: {options.ErrorRetryDelay} seconds");
        ConsoleOutput.Info("Press Ctrl+C to stop");

        int collectionCount = 0;
        IObservingConditions? ocDevice = null;
        ISafetyMonitor? smDevice = null;
        DataCollectionService? collectionService = null;
        bool devicesConnected = false;

        while (!cancellationToken.IsCancellationRequested) {
            try {
                // Try to connect to devices if not connected
                if (!devicesConnected) {
                    try {
                        ConsoleOutput.Info("Attempting to connect to devices...");

                        var connectionService = serviceProvider.GetRequiredService<DeviceConnectionService>();

                        ConsoleOutput.Info("Connecting to ObservingConditions device...");
                        ocDevice = await connectionService.ConnectObservingConditionsAsync(cancellationToken);
                        ConsoleOutput.Info($"Connected to ObservingConditions: {ocDevice.Name}");

                        ConsoleOutput.Info("Connecting to SafetyMonitor device...");
                        smDevice = await connectionService.ConnectSafetyMonitorAsync(cancellationToken);
                        ConsoleOutput.Info($"Connected to SafetyMonitor: {smDevice.Name}");

                        collectionService = serviceProvider.GetRequiredService<DataCollectionService>();
                        collectionService.Initialize(ocDevice, smDevice);

                        devicesConnected = true;
                        ConsoleOutput.Info("Devices connected successfully");
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception ex) {
                        ConsoleOutput.Error($"Failed to connect to devices: {ex.Message}");
                        ConsoleOutput.Warning($"Will retry after {options.ErrorRetryDelay} seconds...");

                        // Clean up
                        DisconnectDevice(ocDevice, "ObservingConditions");
                        DisconnectDevice(smDevice, "SafetyMonitor");
                        ocDevice = null;
                        smDevice = null;
                        collectionService = null;

                        await Task.Delay(TimeSpan.FromSeconds(options.ErrorRetryDelay), cancellationToken);
                        continue;
                    }
                }

                // Try to collect data
                if (collectionService != null) {
                    collectionCount++;

                    var data = await collectionService.CollectDataAsync(cancellationToken);

                    // Output to console
                    if (!options.Quiet) {
                        Console.WriteLine($"--- Collection #{collectionCount} ---");
                        ConsoleOutput.PrintData(data);
                    }

                    // Save to database
                    if (storage != null) {
                        storage.AddData(data);

                        if (!options.Quiet) {
                            ConsoleOutput.Info($"Data saved to database (collection #{collectionCount})");
                        }
                    }

                    // Wait for next interval
                    await Task.Delay(TimeSpan.FromSeconds(options.Interval), cancellationToken);
                }
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                ConsoleOutput.Error($"Error during data collection: {ex.Message}");
                ConsoleOutput.Warning($"Will retry after {options.ErrorRetryDelay} seconds...");

                // Mark devices as disconnected to force reconnection
                devicesConnected = false;

                // Clean up
                DisconnectDevice(ocDevice, "ObservingConditions");
                DisconnectDevice(smDevice, "SafetyMonitor");
                ocDevice = null;
                smDevice = null;
                collectionService = null;

                try {
                    await Task.Delay(TimeSpan.FromSeconds(options.ErrorRetryDelay), cancellationToken);
                } catch (OperationCanceledException) {
                    break;
                }
            }
        }

        // Final cleanup
        DisconnectDevice(ocDevice, "ObservingConditions");
        DisconnectDevice(smDevice, "SafetyMonitor");

        ConsoleOutput.Info($"Continuous mode stopped. Total collections: {collectionCount}");
    }

    /// <summary>
    /// Run single data collection
    /// </summary>
    static async Task RunOnceModeAsync(
        DataCollectionService collectionService,
        DataStorage.DataStorage? storage,
        CommandLineOptions options,
        CancellationToken cancellationToken) {
        ConsoleOutput.Info("Running single data collection...");

        var data = await collectionService.CollectDataAsync(cancellationToken);

        // Output to console
        if (!options.Quiet) {
            ConsoleOutput.PrintData(data);
        }

        // Save to database
        if (storage != null) {
            storage.AddData(data);
            ConsoleOutput.Info("Data saved to database");
        }
    }

    /// <summary>
    /// Run continuous data collection
    /// </summary>
    /// <summary>
    /// Parse command line arguments
    /// </summary>
    static CommandLineOptions? ParseCommandLine(string[] args) {
        var options = new CommandLineOptions();

        // ObservingConditions device options
        var ocNameOption = new Option<string?>("--oc-name") { Description = "ObservingConditions device name for discovery" };
        var ocAddressOption = new Option<string?>("--oc-address") { Description = "ObservingConditions device IP address" };
        var ocPortOption = new Option<int?>("--oc-port") { Description = "ObservingConditions device port" };
        var ocDeviceNumberOption = new Option<int>("--oc-device-number") {
            Description = "ObservingConditions device number",
            DefaultValueFactory = (result) => 0
        };

        // SafetyMonitor device options
        var smNameOption = new Option<string?>("--sm-name") { Description = "SafetyMonitor device name for discovery" };
        var smAddressOption = new Option<string?>("--sm-address") { Description = "SafetyMonitor device IP address" };
        var smPortOption = new Option<int?>("--sm-port") { Description = "SafetyMonitor device port" };
        var smDeviceNumberOption = new Option<int>("--sm-device-number") {
            Description = "SafetyMonitor device number",
            DefaultValueFactory = (result) => 0
        };

        // Retry options
        var discoveryRetriesOption = new Option<int>("--discovery-retries") {
            Description = "Number of discovery retry attempts",
            DefaultValueFactory = (result) => 3
        };
        var dataRetriesOption = new Option<int>("--data-retries") {
            Description = "Number of data retrieval retry attempts",
            DefaultValueFactory = (result) => 3
        };
        var retryDelayOption = new Option<int>("--retry-delay") {
            Description = "Delay between retries in milliseconds",
            DefaultValueFactory = (result) => 1000
        };

        // Output options
        var quietOption = new Option<bool>("--quiet") { Description = "Suppress data output to console" };

        // Database options
        var storagePathOption = new Option<string?>("--storage-path") { Description = "Path to database storage root directory" };
        var dbUserOption = new Option<string>("--db-user") {
            Description = "Database user name",
            DefaultValueFactory = (result) => "SYSDBA"
        };
        var dbPasswordOption = new Option<string>("--db-password") {
            Description = "Database password",
            DefaultValueFactory = (result) => "masterkey"
        };

        // Continuous mode options
        var continuousOption = new Option<bool>("--continuous") { Description = "Enable continuous data collection mode" };
        var intervalOption = new Option<int>("--interval") {
            Description = "Interval between collections in seconds",
            DefaultValueFactory = (result) => 3
        };
        var errorRetryDelayOption = new Option<int>("--error-retry-delay") {
            Description = "Delay in seconds after fatal error in continuous mode",
            DefaultValueFactory = (result) => 30
        };

        var rootCommand = new RootCommand("ASCOM Alpaca Data Collector - Collect data from ObservingConditions and SafetyMonitor devices");

        rootCommand.Options.Add(ocNameOption);
        rootCommand.Options.Add(ocAddressOption);
        rootCommand.Options.Add(ocPortOption);
        rootCommand.Options.Add(ocDeviceNumberOption);
        rootCommand.Options.Add(smNameOption);
        rootCommand.Options.Add(smAddressOption);
        rootCommand.Options.Add(smPortOption);
        rootCommand.Options.Add(smDeviceNumberOption);
        rootCommand.Options.Add(discoveryRetriesOption);
        rootCommand.Options.Add(dataRetriesOption);
        rootCommand.Options.Add(retryDelayOption);
        rootCommand.Options.Add(quietOption);
        rootCommand.Options.Add(storagePathOption);
        rootCommand.Options.Add(dbUserOption);
        rootCommand.Options.Add(dbPasswordOption);
        rootCommand.Options.Add(continuousOption);
        rootCommand.Options.Add(intervalOption);
        rootCommand.Options.Add(errorRetryDelayOption);

        rootCommand.SetAction(parseResult => {
            options.OcName = parseResult.GetValue(ocNameOption);
            options.OcAddress = parseResult.GetValue(ocAddressOption);
            options.OcPort = parseResult.GetValue(ocPortOption);
            options.OcDeviceNumber = parseResult.GetValue(ocDeviceNumberOption);
            options.SmName = parseResult.GetValue(smNameOption);
            options.SmAddress = parseResult.GetValue(smAddressOption);
            options.SmPort = parseResult.GetValue(smPortOption);
            options.SmDeviceNumber = parseResult.GetValue(smDeviceNumberOption);
            options.DiscoveryRetries = parseResult.GetValue(discoveryRetriesOption);
            options.DataRetries = parseResult.GetValue(dataRetriesOption);
            options.RetryDelay = parseResult.GetValue(retryDelayOption);
            options.Quiet = parseResult.GetValue(quietOption);
            options.StoragePath = parseResult.GetValue(storagePathOption);
            options.DbUser = parseResult.GetValue(dbUserOption);
            options.DbPassword = parseResult.GetValue(dbPasswordOption);
            options.Continuous = parseResult.GetValue(continuousOption);
            options.Interval = parseResult.GetValue(intervalOption);
            options.ErrorRetryDelay = parseResult.GetValue(errorRetryDelayOption);
        });

        var parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Any()) {
            foreach (var error in parseResult.Errors) {
                Console.Error.WriteLine(error.Message);
            }
            return null;
        }

        parseResult.Invoke();

        return options;
    }

    /// <summary>
    /// Validate command line options
    /// </summary>
    static bool ValidateOptions(CommandLineOptions options, out List<string> errors) {
        errors = [];

        // Validate ObservingConditions configuration
        if (!options.HasValidOcConfiguration(out var ocError)) {
            errors.Add(ocError!);
        }

        // Validate SafetyMonitor configuration
        if (!options.HasValidSmConfiguration(out var smError)) {
            errors.Add(smError!);
        }

        // Validate retry settings
        if (options.DiscoveryRetries < 1) {
            errors.Add("Discovery retries must be at least 1");
        }

        if (options.DataRetries < 1) {
            errors.Add("Data retries must be at least 1");
        }

        if (options.RetryDelay < 0) {
            errors.Add("Retry delay must be non-negative");
        }

        // Validate interval
        if (options.Interval < 1) {
            errors.Add("Interval must be at least 1 second");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Configure dependency injection services
    /// </summary>
    static void ConfigureServices(IServiceCollection services, CommandLineOptions options) {
        // Add options as singleton
        services.AddSingleton(options);

        // Add services
        services.AddSingleton<DeviceConnectionService>();
        services.AddSingleton<DataCollectionService>();
    }
}
