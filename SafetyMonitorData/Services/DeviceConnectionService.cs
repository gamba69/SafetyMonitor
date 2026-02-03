using ASCOM.Alpaca.Clients;
using ASCOM.Alpaca.Discovery;
using ASCOM.Common.DeviceInterfaces;
using ASCOM.Common;
using SafetyMonitorData.Configuration;
using SafetyMonitorData.Utilities;

namespace SafetyMonitorData.Services;

/// <summary>
/// Service for discovering and connecting to ASCOM Alpaca devices
/// </summary>
public class DeviceConnectionService(
    CommandLineOptions options) {
    private readonly CommandLineOptions _options = options;

    /// <summary>
    /// Connect to ObservingConditions device
    /// </summary>
    public async Task<IObservingConditions> ConnectObservingConditionsAsync(
        CancellationToken cancellationToken = default) {
        if (_options.OcUsesDiscovery) {
            return await DiscoverAndConnectAsync<IObservingConditions>(
                _options.OcName!,
                DeviceTypes.ObservingConditions,
                _options.OcDeviceNumber,
                cancellationToken);
        } else {
            return await ConnectByAddressAsync<IObservingConditions>(
                _options.OcAddress!,
                _options.OcPort!.Value,
                DeviceTypes.ObservingConditions,
                _options.OcDeviceNumber,
                cancellationToken);
        }
    }

    /// <summary>
    /// Connect to SafetyMonitor device
    /// </summary>
    public async Task<ISafetyMonitor> ConnectSafetyMonitorAsync(
        CancellationToken cancellationToken = default) {
        if (_options.SmUsesDiscovery) {
            return await DiscoverAndConnectAsync<ISafetyMonitor>(
                _options.SmName!,
                DeviceTypes.SafetyMonitor,
                _options.SmDeviceNumber,
                cancellationToken);
        } else {
            return await ConnectByAddressAsync<ISafetyMonitor>(
                _options.SmAddress!,
                _options.SmPort!.Value,
                DeviceTypes.SafetyMonitor,
                _options.SmDeviceNumber,
                cancellationToken);
        }
    }

    /// <summary>
    /// Discover and connect to device by name
    /// </summary>
    private async Task<T> DiscoverAndConnectAsync<T>(
        string deviceName,
        DeviceTypes deviceType,
        int deviceNumber,
        CancellationToken cancellationToken) where T : IAscomDevice {
        ConsoleOutput.Info($"Starting discovery for {deviceType} device '{deviceName}'...");

        // Perform discovery with retries
        AscomDevice? foundDevice = null;

        for (int attempt = 1; attempt <= _options.DiscoveryRetries; attempt++) {
            try {
                ConsoleOutput.Info($"Discovery attempt {attempt}/{_options.DiscoveryRetries}...");

                foundDevice = await DiscoverDeviceAsync(deviceName, deviceType, cancellationToken);

                if (foundDevice != null) {
                    ConsoleOutput.Info(
                        $"Device '{deviceName}' found at {foundDevice.IpAddress}:{foundDevice.IpPort}");
                    break;
                }

                ConsoleOutput.Warning($"Device '{deviceName}' not found in discovery results");
            } catch (Exception ex) {
                ConsoleOutput.Error($"Discovery attempt {attempt} failed: {ex.Message}");
            }

            if (attempt < _options.DiscoveryRetries) {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.RetryDelay), cancellationToken);
            }
        }

        if (foundDevice == null) {
            throw new Exception($"Failed to discover {deviceType} device '{deviceName}' after {_options.DiscoveryRetries} attempts");
        }

        // Create and connect client
        var client = CreateClient<T>(deviceType, foundDevice.IpAddress, foundDevice.IpPort, deviceNumber);
        await ConnectClientAsync(client, cancellationToken);

        return client;
    }

    /// <summary>
    /// Connect to device by address and port
    /// </summary>
    private async Task<T> ConnectByAddressAsync<T>(
        string address,
        int port,
        DeviceTypes deviceType,
        int deviceNumber,
        CancellationToken cancellationToken) where T : IAscomDevice {
        ConsoleOutput.Info($"Connecting to {deviceType} device at {address}:{port}...");

        var client = CreateClient<T>(deviceType, address, port, deviceNumber);
        await ConnectClientAsync(client, cancellationToken);

        return client;
    }

    /// <summary>
    /// Perform Alpaca discovery to find a device using AlpacaDiscovery static methods
    /// </summary>
    private static async Task<AscomDevice?> DiscoverDeviceAsync(
        string deviceName,
        DeviceTypes deviceType,
        CancellationToken cancellationToken) {
        // Use static discovery method from AlpacaDiscovery class
        // Discovery duration default is 2 seconds, which should be sufficient
        var devices = await AlpacaDiscovery.GetAscomDevicesAsync(
            deviceTypes: deviceType,
            discoveryPort: 32227,
            discoveryDuration: 2.0,
            cancellationToken: cancellationToken);

        // Find matching device by name
        var matchingDevice = devices.FirstOrDefault(d =>
            d.AscomDeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase));

        return matchingDevice;
    }

    /// <summary>
    /// Create Alpaca client for the specified device type
    /// </summary>
    private static T CreateClient<T>(DeviceTypes deviceType, string host, int port, int deviceNumber) where T : IAscomDevice {
        // Create client configuration (default settings)
        var config = new AlpacaConfiguration {
            IpAddressString = host,
            PortNumber = port,
            RemoteDeviceNumber = deviceNumber
        };

        IAscomDevice client = deviceType switch {
            DeviceTypes.ObservingConditions => new AlpacaObservingConditions(config),
            DeviceTypes.SafetyMonitor => new AlpacaSafetyMonitor(config),
            _ => throw new ArgumentException($"Unsupported device type: {deviceType}")
        };

        return (T)client;
    }

    /// <summary>
    /// Connect to the device with retry logic
    /// </summary>
    private async Task ConnectClientAsync(IAscomDevice client, CancellationToken cancellationToken) {
        await RetryPolicy.ExecuteAsync(
            async () => {
                // Set Connected property
                client.Connected = true;

                // Verify connection
                if (!client.Connected) {
                    throw new Exception("Failed to set Connected = true");
                }

                ConsoleOutput.Info($"Successfully connected to {client.Name}");

                return true;
            },
            _options.DataRetries,
            TimeSpan.FromMilliseconds(_options.RetryDelay),
            (attempt, ex) => {
                ConsoleOutput.Warning($"Connection attempt {attempt}/{_options.DataRetries} failed: {ex.Message}");
            },
            cancellationToken);
    }
}
