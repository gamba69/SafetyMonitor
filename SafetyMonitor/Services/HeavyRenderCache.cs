namespace SafetyMonitor.Services;

/// <summary>
/// Represents heavy render cache and encapsulates its related behavior and state.
/// </summary>
internal static class HeavyRenderCache {
    private static readonly Dictionary<string, Bitmap> _bitmapCache = [];
    private static readonly Lock _sync = new();

    /// <summary>
    /// Gets the bitmap for heavy render cache.
    /// </summary>
    /// <param name="key">Input value for key.</param>
    /// <returns>The result of the operation.</returns>
    public static Bitmap? GetBitmap(string key) {
        lock (_sync) {
            return _bitmapCache.TryGetValue(key, out var value)
                ? (Bitmap)value.Clone()
                : null;
        }
    }

    /// <summary>
    /// Executes put bitmap as part of heavy render cache processing.
    /// </summary>
    /// <param name="key">Input value for key.</param>
    /// <param name="bitmap">Input value for bitmap.</param>
    public static void PutBitmap(string key, Bitmap bitmap) {
        lock (_sync) {
            if (_bitmapCache.Remove(key, out var existing)) {
                existing.Dispose();
            }

            _bitmapCache[key] = (Bitmap)bitmap.Clone();
        }
    }

    /// <summary>
    /// Executes clear as part of heavy render cache processing.
    /// </summary>
    public static void Clear() {
        lock (_sync) {
            foreach (var item in _bitmapCache.Values) {
                item.Dispose();
            }

            _bitmapCache.Clear();
        }
    }
}
