using DataStorage;
using System.Text;

namespace SafetyMonitor.Services;

public static class StorageValidationMessageFormatter {

    #region Private Fields

    private const int MaxDisplayedIssues = 8;

    #endregion Private Fields

    #region Public Methods

    public static string BuildMessage(IEnumerable<DataStorage.DataStorage.StorageValidationIssue> issues, string intro, string actionHint) {
        var normalizedIssues = issues
            .Select(x => x.Message?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var builder = new StringBuilder();
        builder.Append(intro);

        if (normalizedIssues.Count > 0) {
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("Detected issues:");

            foreach (var issue in normalizedIssues.Take(MaxDisplayedIssues)) {
                builder.AppendLine($"• {issue}");
            }

            var remaining = normalizedIssues.Count - MaxDisplayedIssues;
            if (remaining > 0) {
                builder.AppendLine($"• ... and {remaining} more issue(s).");
            }
        }

        else {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append("No detailed validation issues were provided.");
        }

        if (!string.IsNullOrWhiteSpace(actionHint)) {
            builder.AppendLine();
            builder.AppendLine();
            builder.Append(actionHint);
        }

        return builder.ToString();
    }

    #endregion Public Methods
}
