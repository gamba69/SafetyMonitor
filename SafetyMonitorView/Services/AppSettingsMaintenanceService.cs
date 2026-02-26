using SafetyMonitorView.Models;
using System.IO.Compression;

namespace SafetyMonitorView.Services;

public class AppSettingsMaintenanceService {
    private const string BackupDirectoryName = "Backup";
    private readonly AppSettingsService _appSettingsService;
    private readonly DashboardService _dashboardService;

    public AppSettingsMaintenanceService(AppSettingsService appSettingsService, DashboardService dashboardService) {
        _appSettingsService = appSettingsService;
        _dashboardService = dashboardService;
    }

    public string GetDefaultBackupFilePath() {
        var backupDirectory = Path.Combine(_appSettingsService.AppDataFolderPath, BackupDirectoryName);
        Directory.CreateDirectory(backupDirectory);
        var fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".zip";
        return Path.Combine(backupDirectory, fileName);
    }

    public void EnsureSettingsExists() {
        if (File.Exists(_appSettingsService.SettingsPath)) {
            return;
        }

        ResetToDefaults();
    }

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

    public void ImportFromArchive(string archivePath) {
        if (!File.Exists(archivePath)) {
            throw new FileNotFoundException("Settings archive not found.", archivePath);
        }

        var appDataPath = _appSettingsService.AppDataFolderPath;
        Directory.CreateDirectory(appDataPath);
        CleanupCurrentSettings(appDataPath);

        ZipFile.ExtractToDirectory(archivePath, appDataPath, overwriteFiles: true);
    }

    public AppSettings ResetToDefaults() {
        var appDataPath = _appSettingsService.AppDataFolderPath;
        Directory.CreateDirectory(appDataPath);
        CleanupCurrentSettings(appDataPath);

        var defaults = AppSettingsDefaultsService.CreateDefaults();
        _appSettingsService.SaveSettings(defaults);

        var defaultDashboard = Dashboard.CreateDefault();
        defaultDashboard.SortOrder = 0;
        _dashboardService.SaveDashboard(defaultDashboard);

        return defaults;
    }

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
