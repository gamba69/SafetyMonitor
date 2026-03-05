namespace SafetyMonitor.Services;

/// <summary>
/// Process-wide cache for expensive render artifacts.
/// Stores canonical bitmap instances and returns clones for safe callers usage.
/// </summary>
internal static class HeavyRenderCache {
    private static readonly Dictionary<string, Bitmap> _bitmapCache = [];
    private static readonly Lock _sync = new();

    public static Bitmap? GetBitmap(string key) {
        lock (_sync) {
            return _bitmapCache.TryGetValue(key, out var value)
                ? (Bitmap)value.Clone()
                : null;
        }
    }

    public static void PutBitmap(string key, Bitmap bitmap) {
        lock (_sync) {
            if (_bitmapCache.Remove(key, out var existing)) {
                existing.Dispose();
            }

            _bitmapCache[key] = (Bitmap)bitmap.Clone();
        }
    }

    public static void Clear() {
        lock (_sync) {
            foreach (var item in _bitmapCache.Values) {
                item.Dispose();
            }

            _bitmapCache.Clear();
        }
    }
}
