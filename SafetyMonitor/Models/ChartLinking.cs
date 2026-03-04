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

    public static string GetDisplayName(this ChartLinkGroup group) => group.ToString();
}
