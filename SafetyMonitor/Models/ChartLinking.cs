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

    public static IReadOnlyList<ChartLinkGroup> GetAvailable(int usedGroups) {
        var normalized = NormalizeUsedGroups(usedGroups);
        return _all.Take(normalized).ToList();
    }

    public static ChartLinkGroup NormalizeGroup(ChartLinkGroup group, int usedGroups) {
        var available = GetAvailable(usedGroups);
        return available.Contains(group) ? group : available[0];
    }

    public static string GetDisplayName(this ChartLinkGroup group) => group.ToString();
}
