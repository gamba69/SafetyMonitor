using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var repoRoot = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Directory.GetCurrentDirectory();

var buildDir = Path.Combine(repoRoot, "build");
var generatedDir = Path.Combine(buildDir, "generated");
Directory.CreateDirectory(generatedDir);

var basePath = Path.Combine(buildDir, "version.base.json");
var statePath = Path.Combine(buildDir, "version.state.json");
var lockPath = Path.Combine(buildDir, "version.state.lock");
var generatedVersionPath = Path.Combine(generatedDir, "BuildVersion.g.cs");

await WaitForLock(lockPath);
try
{
    var baseVersion = ReadBaseVersion(basePath);
    var state = ReadState(statePath);

    var currentBaseVersion = $"{baseVersion.Major}.{baseVersion.Minor}.{baseVersion.Patch}";
    if (!string.Equals(state.LastBaseVersion, currentBaseVersion, StringComparison.Ordinal))
    {
        state.CurrentPatch = baseVersion.Patch;
        state.LastBaseVersion = currentBaseVersion;
    }

    var sourceHash = ComputeSourceHash(repoRoot);
    var sourcesChanged = !string.Equals(sourceHash, state.LastSourceHash, StringComparison.Ordinal);

    if (sourcesChanged)
    {
        state.CurrentPatch += 1;
        state.BuildCounter += 1;
        state.LastSourceHash = sourceHash;
        state.LastBuildDateUtc = DateTime.UtcNow;
    }

    // Keep version aligned when state file was manually edited.
    if (state.CurrentPatch < baseVersion.Patch)
    {
        state.CurrentPatch = baseVersion.Patch;
    }

    var buildDateUtc = state.LastBuildDateUtc ?? DateTime.UtcNow;
    var assemblyVersion = $"{baseVersion.Major}.{baseVersion.Minor}.0.0";
    var fileVersion = $"{baseVersion.Major}.{baseVersion.Minor}.{state.CurrentPatch}.{state.BuildCounter}";
    var informationalVersion = $"{baseVersion.Major}.{baseVersion.Minor}.{state.CurrentPatch}+build.{state.BuildCounter}";

    WriteGeneratedVersionFile(generatedVersionPath, new GeneratedVersion(
        baseVersion.Major,
        baseVersion.Minor,
        state.CurrentPatch,
        state.BuildCounter,
        assemblyVersion,
        fileVersion,
        informationalVersion,
        sourceHash,
        buildDateUtc));

    WriteState(statePath, state);

    Console.WriteLine($"Version calculated: {fileVersion} (changed: {sourcesChanged})");
}
finally
{
    File.Delete(lockPath);
}

return;

static async Task WaitForLock(string lockPath)
{
    const int retries = 120;

    for (var i = 0; i < retries; i++)
    {
        try
        {
            await using var stream = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync($"{Environment.ProcessId}");
            await writer.FlushAsync();
            return;
        }
        catch (IOException)
        {
            await Task.Delay(100);
        }
    }

    throw new InvalidOperationException("Unable to acquire version state lock.");
}

static VersionBase ReadBaseVersion(string path)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException("Version base file was not found.", path);
    }

    var result = JsonSerializer.Deserialize<VersionBase>(File.ReadAllText(path), JsonOptions())
                 ?? throw new InvalidOperationException("Unable to read base version file.");

    if (result.Major < 0 || result.Minor < 0 || result.Patch < 0)
    {
        throw new InvalidOperationException("Version base values must be non-negative.");
    }

    return result;
}

static VersionState ReadState(string path)
{
    if (!File.Exists(path))
    {
        return new VersionState();
    }

    return JsonSerializer.Deserialize<VersionState>(File.ReadAllText(path), JsonOptions()) ?? new VersionState();
}

static void WriteState(string path, VersionState state)
{
    if (state.CurrentPatch < 0 || state.BuildCounter < 0)
    {
        throw new InvalidOperationException("Version state values must be non-negative.");
    }

    var json = JsonSerializer.Serialize(state, JsonOptionsIndented());
    File.WriteAllText(path, json + Environment.NewLine);
}

static string ComputeSourceHash(string repoRoot)
{
    var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".props", ".targets", ".resx", ".json", ".sln", ".slnx"
    };

    var ignoredPaths = new[]
    {
        Path.Combine(repoRoot, ".git"),
        Path.Combine(repoRoot, "build", "generated")
    };

    var files = Directory
        .EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
        .Where(path =>
        {
            var normalizedPath = Path.GetFullPath(path);

            if (normalizedPath.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (ignoredPaths.Any(prefix => normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var fileName = Path.GetFileName(normalizedPath);
            if (string.Equals(fileName, "version.state.json", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "version.state.lock", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return extensions.Contains(Path.GetExtension(normalizedPath));
        })
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();

    using var sha256 = SHA256.Create();
    using var finalStream = new MemoryStream();

    foreach (var file in files)
    {
        var relative = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
        var relativeBytes = Encoding.UTF8.GetBytes(relative);
        finalStream.Write(relativeBytes);
        finalStream.WriteByte(0);

        var content = File.ReadAllBytes(file);
        finalStream.Write(content);
        finalStream.WriteByte(0);
    }

    finalStream.Position = 0;
    var hash = sha256.ComputeHash(finalStream);
    return Convert.ToHexString(hash);
}

static void WriteGeneratedVersionFile(string path, GeneratedVersion version)
{
    var source = $$"""
// <auto-generated />
using System;
using System.Reflection;

#if WINDOWS
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif

[assembly: AssemblyVersion("{{version.AssemblyVersion}}")]
[assembly: AssemblyFileVersion("{{version.FileVersion}}")]
[assembly: AssemblyInformationalVersion("{{version.InformationalVersion}}")]

namespace SafetyMonitor.Versioning;

public static class BuildVersion
{
    public const int Major = {{version.Major}};
    public const int Minor = {{version.Minor}};
    public const int Patch = {{version.Patch}};
    public const int Build = {{version.Build}};
    public const string Version = "{{version.FileVersion}}";
    public const string InformationalVersion = "{{version.InformationalVersion}}";
    public const string SourceHash = "{{version.SourceHash}}";
    public static readonly DateTime BuildDateUtc = DateTime.Parse("{{version.BuildDateUtcIso}}", null, System.Globalization.DateTimeStyles.RoundtripKind);
    public const string BuildDateUtcIso = "{{version.BuildDateUtcIso}}";
}
""";

    File.WriteAllText(path, source);
}

static JsonSerializerOptions JsonOptions() => new()
{
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.Strict
};

static JsonSerializerOptions JsonOptionsIndented() => new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

internal sealed class VersionBase
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
}

internal sealed class VersionState
{
    public int CurrentPatch { get; set; }
    public int BuildCounter { get; set; }
    public string LastSourceHash { get; set; } = string.Empty;
    public string LastBaseVersion { get; set; } = string.Empty;
    public DateTime? LastBuildDateUtc { get; set; }
}

internal sealed record GeneratedVersion(
    int Major,
    int Minor,
    int Patch,
    int Build,
    string AssemblyVersion,
    string FileVersion,
    string InformationalVersion,
    string SourceHash,
    DateTime BuildDateUtc)
{
    public string BuildDateUtcIso => BuildDateUtc.ToString("O");
}
