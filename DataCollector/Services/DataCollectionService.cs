using ASCOM.Common.DeviceInterfaces;
using DataCollector.Configuration;
using DataStorage.Models;
using SafetyMonitorData.Utilities;

namespace DataCollector.Services;

/// <summary>
/// Represents data collection service and encapsulates its related behavior and state.
/// </summary>
public class DataCollectionService(
    CommandLineOptions options) {

    #region Private Fields

    private readonly CommandLineOptions _options = options;
    private IObservingConditions? _ocDevice;
    private ISafetyMonitor? _smDevice;

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Collects the data async for data collection service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used to cancel the operation.</param>
    /// <returns>A task that returns the operation result.</returns>
    public async Task<ObservingData> CollectDataAsync(CancellationToken cancellationToken = default) {
        if (_ocDevice == null || _smDevice == null) {
            throw new InvalidOperationException("Devices not initialized. Call Initialize() first.");
        }

        var data = new ObservingData {
            Timestamp = DateTime.UtcNow
        };

        // Collect from both devices in parallel
        await Task.WhenAll(
            CollectObservingConditionsDataAsync(data, cancellationToken),
            CollectSafetyMonitorDataAsync(data, cancellationToken)
        );

        return data;
    }

    /// <summary>
    /// Initializes data collection service state and required resources.
    /// </summary>
    /// <param name="ocDevice">Input value for oc device.</param>
    /// <param name="smDevice">Input value for sm device.</param>
    public void Initialize(IObservingConditions ocDevice, ISafetyMonitor smDevice) {
        _ocDevice = ocDevice;
        _smDevice = smDevice;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Collects the observing conditions data async for data collection service.
    /// </summary>
    /// <param name="data">Input value for data.</param>
    /// <param name="cancellationToken">Cancellation token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CollectObservingConditionsDataAsync(
        ObservingData data,
        CancellationToken cancellationToken) {
        if (_ocDevice == null) {
            return;
        }

        try {
            // Ensure device is connected
            if (!_ocDevice.Connected) {
                ConsoleOutput.Warning("ObservingConditions device not connected, attempting to reconnect…");
                await RetryPolicy.ExecuteAsync(
                    () => { _ocDevice.Connected = true; return Task.CompletedTask; },
                    _options.DataRetries,
                    TimeSpan.FromMilliseconds(_options.RetryDelay),
                    (attempt, ex) => ConsoleOutput.Warning($"Reconnect attempt {attempt}: {ex.Message}"),
                    cancellationToken);
            }

            // Collect all properties with retry
            data.CloudCover = await GetPropertyAsync(() => _ocDevice.CloudCover, "CloudCover", cancellationToken);
            data.DewPoint = await GetPropertyAsync(() => _ocDevice.DewPoint, "DewPoint", cancellationToken);
            data.Humidity = await GetPropertyAsync(() => _ocDevice.Humidity, "Humidity", cancellationToken);
            data.Pressure = await GetPropertyAsync(() => _ocDevice.Pressure, "Pressure", cancellationToken);
            data.RainRate = await GetPropertyAsync(() => _ocDevice.RainRate, "RainRate", cancellationToken);
            data.SkyBrightness = await GetPropertyAsync(() => _ocDevice.SkyBrightness, "SkyBrightness", cancellationToken);
            data.SkyQuality = await GetPropertyAsync(() => _ocDevice.SkyQuality, "SkyQuality", cancellationToken);
            data.SkyTemperature = await GetPropertyAsync(() => _ocDevice.SkyTemperature, "SkyTemperature", cancellationToken);
            data.StarFwhm = await GetPropertyAsync(() => _ocDevice.StarFWHM, "StarFWHM", cancellationToken);
            data.Temperature = await GetPropertyAsync(() => _ocDevice.Temperature, "Temperature", cancellationToken);
            data.WindDirection = await GetPropertyAsync(() => _ocDevice.WindDirection, "WindDirection", cancellationToken);
            data.WindGust = await GetPropertyAsync(() => _ocDevice.WindGust, "WindGust", cancellationToken);
            data.WindSpeed = await GetPropertyAsync(() => _ocDevice.WindSpeed, "WindSpeed", cancellationToken);

            // ConsoleOutput.Info("ObservingConditions data collected successfully");
        } catch (Exception ex) {
            ConsoleOutput.Error($"Failed to collect ObservingConditions data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Collects the safety monitor data async for data collection service.
    /// </summary>
    /// <param name="data">Input value for data.</param>
    /// <param name="cancellationToken">Cancellation token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CollectSafetyMonitorDataAsync(
        ObservingData data,
        CancellationToken cancellationToken) {
        if (_smDevice == null) {
            return;
        }

        try {
            // Ensure device is connected
            if (!_smDevice.Connected) {
                ConsoleOutput.Warning("SafetyMonitor device not connected, attempting to reconnect…");
                await RetryPolicy.ExecuteAsync(
                    () => { _smDevice.Connected = true; return Task.CompletedTask; },
                    _options.DataRetries,
                    TimeSpan.FromMilliseconds(_options.RetryDelay),
                    (attempt, ex) => ConsoleOutput.Warning($"Reconnect attempt {attempt}: {ex.Message}"),
                    cancellationToken);
            }

            // Get IsSafe property with retry
            var isSafe = await RetryPolicy.ExecuteAsync(
                () => _smDevice.IsSafe,
                _options.DataRetries,
                TimeSpan.FromMilliseconds(_options.RetryDelay),
                (attempt, ex) => ConsoleOutput.Warning($"Get IsSafe attempt {attempt}: {ex.Message}"),
                cancellationToken);

            data.IsSafe = isSafe;

            // ConsoleOutput.Info($"SafetyMonitor data collected: IsSafe = {isSafe}");
        } catch (Exception ex) {
            ConsoleOutput.Error($"Failed to collect SafetyMonitor data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the property async for data collection service.
    /// </summary>
    /// <param name="getter">Input value for getter.</param>
    /// <param name="propertyName">Input value for property name.</param>
    /// <param name="cancellationToken">Cancellation token used to cancel the operation.</param>
    /// <returns>A task that returns the operation result.</returns>
    private async Task<double?> GetPropertyAsync(
        Func<double> getter,
        string propertyName,
        CancellationToken cancellationToken) {
        // First try - check if property is implemented
        try {
            return await Task.Run(getter, cancellationToken);
        } catch (ASCOM.PropertyNotImplementedException) {
            // Property not supported by this device - this is normal, not an error
            return null;
        } catch (ASCOM.NotImplementedException) {
            // Property not supported by this device - this is normal, not an error
            return null;
        } catch (Exception) {
            // Property is implemented but failed - retry
            try {
                return await RetryPolicy.ExecuteAsync(
                    getter,
                    _options.DataRetries,
                    TimeSpan.FromMilliseconds(_options.RetryDelay),
                    (attempt, ex) => {
                        ConsoleOutput.Warning($"Get {propertyName} attempt {attempt}: {ex.Message}");
                    },
                    cancellationToken);
            } catch (Exception ex) {
                ConsoleOutput.Warning($"Failed to get {propertyName} after retries: {ex.Message}");
                return null;
            }
        }
    }

    #endregion Private Methods
}
