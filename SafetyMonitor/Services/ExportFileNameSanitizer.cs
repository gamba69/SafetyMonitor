using System.Globalization;
using System.Text;

namespace SafetyMonitor.Services;

/// <summary>
/// Provides helpers to build safe export file names from user-facing titles.
/// </summary>
public static class ExportFileNameSanitizer {
    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase) {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Sanitizes a file name stem for export operations.
    /// </summary>
    /// <param name="input">Input value for input.</param>
    /// <param name="fallback">Input value for fallback.</param>
    /// <param name="maxLength">Input value for max length.</param>
    /// <returns>The resulting string value.</returns>
    public static string SanitizeStem(string? input, string fallback = "export", int maxLength = 120) {
        var normalized = (input ?? string.Empty).Normalize(NormalizationForm.FormKC);

        var builder = new StringBuilder(normalized.Length);
        var lastWasSeparator = false;

        foreach (var c in normalized) {
            if (IsAllowed(c)) {
                builder.Append(c);
                lastWasSeparator = false;
                continue;
            }

            if (char.IsWhiteSpace(c) || IsBlocked(c)) {
                if (!lastWasSeparator) {
                    builder.Append('_');
                    lastWasSeparator = true;
                }
            }
        }

        var candidate = builder
            .ToString()
            .Trim(' ', '.', '_', '-')
            .Trim();

        if (string.IsNullOrWhiteSpace(candidate)) {
            candidate = fallback;
        }

        if (ReservedWindowsNames.Contains(candidate)) {
            candidate = $"_{candidate}";
        }

        if (candidate.Length > maxLength) {
            candidate = candidate[..maxLength].TrimEnd(' ', '.', '_', '-');
        }

        if (string.IsNullOrWhiteSpace(candidate)) {
            candidate = fallback;
        }

        return candidate;
    }

    private static bool IsAllowed(char c) {
        if (char.IsLetterOrDigit(c)) {
            return true;
        }

        return c is '-' or '_' or '.';
    }

    private static bool IsBlocked(char c) {
        if (c is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|') {
            return true;
        }

        return Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0
            || char.GetUnicodeCategory(c) == UnicodeCategory.Control
            || char.GetUnicodeCategory(c) == UnicodeCategory.Format;
    }
}
