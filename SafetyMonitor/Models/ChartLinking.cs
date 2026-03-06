namespace SafetyMonitor.Models;

public enum DashboardChartLinkMode {
    Disabled,
    Grouped,
    Full
}

public enum ChartLinkGroup {
    Alpha,
    Bravo,
    Charlie,
    Delta,
    Echo,
    Foxtrot
}

/// <summary>
/// Represents chart link group info and encapsulates its related behavior and state.
/// </summary>
public static class ChartLinkGroupInfo {
    private static readonly ChartLinkGroup[] _all = [
        ChartLinkGroup.Alpha,
        ChartLinkGroup.Bravo,
        ChartLinkGroup.Charlie,
        ChartLinkGroup.Delta,
        ChartLinkGroup.Echo,
        ChartLinkGroup.Foxtrot
    ];

    public static IReadOnlyList<ChartLinkGroup> All => _all;

    public const int MinUsedGroups = 1;
    public static int MaxUsedGroups => _all.Length;

    public static int NormalizeUsedGroups(int usedGroups) => Math.Clamp(usedGroups, MinUsedGroups, MaxUsedGroups);

    /// <summary>
    /// Gets the available for chart link group info.
    /// </summary>
    /// <param name="usedGroups">Input value for used groups.</param>
    /// <returns>The result of the operation.</returns>
    public static IReadOnlyList<ChartLinkGroup> GetAvailable(int usedGroups) {
        var normalized = NormalizeUsedGroups(usedGroups);
        return _all.Take(normalized).ToList();
    }

    /// <summary>
    /// Normalizes the group for chart link group info.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <param name="usedGroups">Input value for used groups.</param>
    /// <returns>The result of the operation.</returns>
    public static ChartLinkGroup NormalizeGroup(ChartLinkGroup group, int usedGroups) {
        var available = GetAvailable(usedGroups);
        return available.Contains(group) ? group : available[0];
    }

    public static int GetGroupNumber(this ChartLinkGroup group) => Array.IndexOf(_all, group) + 1;

    /// <summary>
    /// Gets the circled number for chart link group info.
    /// </summary>
    /// <param name="group)">Input value for group.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetCircledNumber(this ChartLinkGroup group) => group.GetGroupNumber() switch {
        1 => "①",
        2 => "②",
        3 => "③",
        4 => "④",
        5 => "⑤",
        6 => "⑥",
        _ => "•"
    };

    /// <summary>
    /// Gets the display name for chart link group info.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <param name="periodShortName">Input value for period short name.</param>
    /// <param name="includeCircledNumber">Input value for include circled number.</param>
    /// <returns>The resulting string value.</returns>
    public static string GetDisplayName(this ChartLinkGroup group, string? periodShortName = null, bool includeCircledNumber = false) {
        var prefix = includeCircledNumber ? $"{group.GetCircledNumber()} " : string.Empty;
        var suffix = string.IsNullOrWhiteSpace(periodShortName) ? string.Empty : $" ({periodShortName.Trim()})";
        return $"{prefix}{group}{suffix}";
    }
}
