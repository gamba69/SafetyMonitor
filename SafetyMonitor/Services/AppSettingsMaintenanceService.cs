using SafetyMonitor.Models;
using System.IO.Compression;

namespace SafetyMonitor.Services;

/// <summary>
/// Represents app settings maintenance service and encapsulates its related behavior and state.
/// </summary>
public class AppSettingsMaintenanceService {
    private const string BackupDirectoryName = "Backup";
    private readonly AppSettingsService _appSettingsService;
    private readonly DashboardService _dashboardService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppSettingsMaintenanceService"/> class.
    /// </summary>
    /// <param name="appSettingsService">Input value for app settings service.</param>
    /// <param name="dashboardService">Input value for dashboard service.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public AppSettingsMaintenanceService(AppSettingsService appSettingsService, DashboardService dashboardService) {
        _appSettingsService = appSettingsService;
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Gets the default backup file path for app settings maintenance service.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    public string GetDefaultBackupFilePath() {
        return GetNextBackupFilePath();
    }

    /// <summary>
    /// Gets the next backup file path for app settings maintenance service.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    public string GetNextBackupFilePath() {
        var backupDirectory = Path.Combine(_appSettingsService.AppDataFolderPath, BackupDirectoryName);
        Directory.CreateDirectory(backupDirectory);

        var backupDatePrefix = DateTime.Now.ToString("yyyy-MM-dd");
        var existingOrdinals = Directory
            .EnumerateFiles(backupDirectory, $"{backupDatePrefix}#*.zip", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name!.Split('#'))
            .Where(static parts => parts.Length == 2
                && int.TryParse(parts[1], out var parsedOrdinal)
                && parsedOrdinal > 0)
            .Select(static parts => int.Parse(parts[1]))
            .ToList();

        var nextOrdinal = existingOrdinals.Count == 0 ? 1 : existingOrdinals.Max() + 1;
        var fileName = $"{backupDatePrefix}#{nextOrdinal:00}.zip";
        return Path.Combine(backupDirectory, fileName);
    }

    /// <summary>
    /// Ensures the settings exists for app settings maintenance service.
    /// </summary>
    public void EnsureSettingsExists() {
        if (File.Exists(_appSettingsService.SettingsPath)) {
            return;
        }

        ResetToDefaults();
    }

    /// <summary>
    /// Exports the to archive for app settings maintenance service.
    /// </summary>
    /// <param name="archivePath">Path value for archive path.</param>
    public void ExportToArchive(string archivePath) {
        if (string.IsNullOrWhiteSpace(archivePath)) {
            throw new ArgumentException("Archive path is empty.", nameof(archivePath));
        }

        EnsureSettingsExists();

        var targetDirectory = Path.GetDirectoryName(archivePath);
        if (!string.IsNullOrWhiteSpace(targetDirectory)) {
            Directory.CreateDirectory(targetDirectory);
        }

        if (File.Exists(archivePath)) {
            File.Delete(archivePath);
        }

        using var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        AddDirectoryToArchive(zip, _appSettingsService.AppDataFolderPath, string.Empty);
    }

    /// <summary>
    /// Imports the from archive for app settings maintenance service.
    /// </summary>
    /// <param name="archivePath">Path value for archive path.</param>
    public void ImportFromArchive(string archivePath) {
        if (!File.Exists(archivePath)) {
            throw new FileNotFoundException("Settings archive not found.", archivePath);
        }

        var appDataPath = _appSettingsService.AppDataFolderPath;
        Directory.CreateDirectory(appDataPath);
        CleanupCurrentSettings(appDataPath);

        ZipFile.ExtractToDirectory(archivePath, appDataPath, overwriteFiles: true);
    }

    /// <summary>
    /// Resets the to defaults for app settings maintenance service.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public AppSettings ResetToDefaults() {
        var appDataPath = _appSettingsService.AppDataFolderPath;
        Directory.CreateDirectory(appDataPath);
        CleanupCurrentSettings(appDataPath);

        var defaults = AppSettingsDefaultsService.CreateDefaults();
        _appSettingsService.SaveSettings(defaults);
        _dashboardService.ResetToSingleDefaultDashboard();

        return defaults;
    }

    /// <summary>
    /// Executes cleanup current settings as part of app settings maintenance service processing.
    /// </summary>
    /// <param name="appDataPath">Path value for app data path.</param>
    private static void CleanupCurrentSettings(string appDataPath) {
        foreach (var filePath in Directory.EnumerateFiles(appDataPath, "*", SearchOption.TopDirectoryOnly)) {
            File.Delete(filePath);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(appDataPath, "*", SearchOption.TopDirectoryOnly)) {
            if (string.Equals(Path.GetFileName(directoryPath), BackupDirectoryName, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            Directory.Delete(directoryPath, recursive: true);
        }
    }

    /// <summary>
    /// Adds the directory to archive for app settings maintenance service.
    /// </summary>
    /// <param name="zip">Input value for zip.</param>
    /// <param name="directoryPath">Path value for directory path.</param>
    /// <param name="relativePath">Path value for relative path.</param>
    private static void AddDirectoryToArchive(ZipArchive zip, string directoryPath, string relativePath) {
        foreach (var directory in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly)) {
            var directoryName = Path.GetFileName(directory);
            if (string.Equals(directoryName, BackupDirectoryName, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            var childRelativePath = string.IsNullOrEmpty(relativePath)
                ? directoryName
                : Path.Combine(relativePath, directoryName);
            AddDirectoryToArchive(zip, directory, childRelativePath);
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)) {
            var fileName = Path.GetFileName(filePath);
            var entryPath = string.IsNullOrEmpty(relativePath)
                ? fileName
                : Path.Combine(relativePath, fileName);
            var normalizedEntryPath = entryPath.Replace(Path.DirectorySeparatorChar, '/');
            zip.CreateEntryFromFile(filePath, normalizedEntryPath, CompressionLevel.Optimal);
        }
    }
}
