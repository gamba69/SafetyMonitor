using System.Globalization;
using SafetyMonitor.Versioning;

namespace SafetyMonitor.Services;

internal static class AppBuildInfoHelper {

    #region Public Properties

    public static string BuildDateDisplay => BuildVersion.BuildDateUtc.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static int CopyrightYear => BuildVersion.BuildDateUtc.Year;

    public static string CopyrightLine => $"©{CopyrightYear} DreamSky Observatory, Igor K. Dulevich (gamba69)";

    public static string ProductVersionWithBuild => $"{BuildVersion.Major}.{BuildVersion.Minor}.{BuildVersion.Patch} build {BuildVersion.Build}";

    #endregion Public Properties
}
