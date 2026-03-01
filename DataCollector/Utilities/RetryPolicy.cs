namespace SafetyMonitorData.Utilities;

/// <summary>
/// Provides retry policy for operations that may fail temporarily
/// </summary>
public static class RetryPolicy {

    #region Public Methods

    /// <summary>
    /// Execute an async operation with retry logic
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="maxRetries">Maximum number of retry attempts</param>
    /// <param name="delay">Delay between retry attempts</param>
    /// <param name="onRetry">Optional callback executed on each retry attempt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation, or default(T) if all retries fail</returns>
    public static async Task<T?> ExecuteAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        TimeSpan delay,
        Action<int, Exception>? onRetry = null,
        CancellationToken cancellationToken = default) {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++) {
            try {
                return await operation();
            } catch (OperationCanceledException) {
                throw; // Don't retry on cancellation
            } catch (Exception ex) {
                lastException = ex;

                if (attempt < maxRetries) {
                    onRetry?.Invoke(attempt, ex);

                    try {
                        await Task.Delay(delay, cancellationToken);
                    } catch (OperationCanceledException) {
                        throw; // Don't continue if cancelled during delay
                    }
                }
            }
        }

        // All retries exhausted
        if (lastException != null) {
            throw new Exception($"Operation failed after {maxRetries} attempts", lastException);
        }

        return default;
    }

    /// <summary>
    /// Execute a synchronous operation with retry logic (wraps in Task)
    /// </summary>
    public static async Task<T?> ExecuteAsync<T>(
        Func<T> operation,
        int maxRetries,
        TimeSpan delay,
        Action<int, Exception>? onRetry = null,
        CancellationToken cancellationToken = default) {
        return await ExecuteAsync(
            () => Task.Run(operation, cancellationToken),
            maxRetries,
            delay,
            onRetry,
            cancellationToken);
    }

    /// <summary>
    /// Execute an operation without a return value
    /// </summary>
    public static async Task ExecuteAsync(
        Func<Task> operation,
        int maxRetries,
        TimeSpan delay,
        Action<int, Exception>? onRetry = null,
        CancellationToken cancellationToken = default) {
        await ExecuteAsync<bool>(
            async () => {
                await operation();
                return true;
            },
            maxRetries,
            delay,
            onRetry,
            cancellationToken);
    }

    #endregion Public Methods
}
