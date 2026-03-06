using System.Diagnostics;
using System.CommandLine;

namespace DataAggregator;

/// <summary>
/// Represents program and encapsulates its related behavior and state.
/// </summary>
internal static class Program {

    /// <summary>
    /// Defines the application entry point and startup workflow.
    /// </summary>
    /// <param name="args">Input value for args.</param>
    /// <returns>A task that returns the operation result.</returns>
    private static async Task<int> Main(string[] args) {
        if (args.Any(arg => arg is "--version" or "-v")) {
            Console.WriteLine(GetVersionString());
            return 0;
        }

        var storagePathOption = new Option<string>("--storage-path") {
            Description = "Path to database storage root directory",
            Required = true
        };

        var startOption = new Option<string?>("--start") {
            Description = "Start timestamp (e.g. 2024-01-15T00:00:00)"
        };

        var endOption = new Option<string?>("--end") {
            Description = "End timestamp (e.g. 2024-01-16T00:00:00)"
        };

        var dbUserOption = new Option<string>("--db-user") {
            Description = "Database user name",
            DefaultValueFactory = _ => "SYSDBA"
        };

        var dbPasswordOption = new Option<string>("--db-password") {
            Description = "Database password",
            DefaultValueFactory = _ => "masterkey"
        };

        var batchSizeOption = new Option<int>("--batch-size") {
            Description = "Aggregation upsert batch size",
            DefaultValueFactory = _ => 1000
        };

        var rootCommand = new RootCommand("Recalculate predefined aggregations from raw data");
        rootCommand.Options.Add(storagePathOption);
        rootCommand.Options.Add(startOption);
        rootCommand.Options.Add(endOption);
        rootCommand.Options.Add(dbUserOption);
        rootCommand.Options.Add(dbPasswordOption);
        rootCommand.Options.Add(batchSizeOption);

        var options = new AggregatorOptions();

        rootCommand.SetAction(parseResult => {
            options.StoragePath = parseResult.GetValue(storagePathOption)!;
            options.StartRaw = parseResult.GetValue(startOption);
            options.EndRaw = parseResult.GetValue(endOption);
            options.DbUser = parseResult.GetValue(dbUserOption)!;
            options.DbPassword = parseResult.GetValue(dbPasswordOption)!;
            options.BatchSize = parseResult.GetValue(batchSizeOption);
        });

        var parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Any()) {
            foreach (var error in parseResult.Errors) {
                Console.Error.WriteLine(error.Message);
            }
            return 1;
        }

        parseResult.Invoke();

        if (args.Any(arg => arg is "--help" or "-h")) {
            return 0;
        }

        return await RecalculateAsync(options);
    }

    /// <summary>
    /// Executes recalculate async as part of program processing.
    /// </summary>
    /// <param name="options">Input value for options.</param>
    /// <returns>A task that returns the operation result.</returns>
    private static Task<int> RecalculateAsync(AggregatorOptions options) {
        var startTime = ParseTimestamp(options.StartRaw) ?? DateTime.UtcNow.AddDays(-1);
        var endTime = ParseTimestamp(options.EndRaw) ?? DateTime.UtcNow;

        if (endTime < startTime) {
            Console.Error.WriteLine("End time must be greater than or equal to start time.");
            return Task.FromResult(1);
        }

        var storage = new DataStorage.DataStorage(options.StoragePath, options.DbUser, options.DbPassword);
        var stopwatch = Stopwatch.StartNew();
        storage.RecalculateAggregations(startTime, endTime, progress => {
            var percent = progress.TotalBuckets <= 0 ? 100d : (progress.ProcessedBuckets * 100d / progress.TotalBuckets);
            var eta = CalculateRemainingTime(stopwatch.Elapsed, progress.ProcessedBuckets, progress.TotalBuckets);
            Console.Write($"\rAggregated {progress.ProcessedBuckets}/{progress.TotalBuckets} ({percent:0.00}%). Elapsed: {FormatDuration(stopwatch.Elapsed)}. Remaining: {FormatDuration(eta)}.");
        }, options.BatchSize);

        Console.WriteLine();
        return Task.FromResult(0);
    }

    /// <summary>
    /// Parses the timestamp for program.
    /// </summary>
    /// <param name="raw">Input value for raw.</param>
    /// <returns>The result of the operation.</returns>
    private static DateTime? ParseTimestamp(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return null;
        }

        if (DateTime.TryParse(raw, out var parsed)) {
            return parsed;
        }

        Console.Error.WriteLine($"Unable to parse timestamp '{raw}'. Use ISO format, e.g. 2024-01-01T12:00:00.");
        return null;
    }


    /// <summary>
    /// Calculates the remaining time for program.
    /// </summary>
    /// <param name="elapsed">Input value for elapsed.</param>
    /// <param name="done">Input value for done.</param>
    /// <param name="total">Input value for total.</param>
    /// <returns>The result of the operation.</returns>
    private static TimeSpan CalculateRemainingTime(TimeSpan elapsed, int done, int total) {
        if (done <= 0 || total <= done) {
            return TimeSpan.Zero;
        }

        var avgPerItem = elapsed.TotalSeconds / done;
        return TimeSpan.FromSeconds(avgPerItem * (total - done));
    }

    /// <summary>
    /// Formats the duration for program.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <returns>The resulting string value.</returns>
    private static string FormatDuration(TimeSpan duration) {
        if (duration < TimeSpan.Zero) {
            duration = TimeSpan.Zero;
        }

        return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    /// <summary>
    /// Represents aggregator options and encapsulates its related behavior and state.
    /// </summary>
    private sealed class AggregatorOptions {
        /// <summary>
        /// Gets or sets the storage path for aggregator options. Specifies a filesystem location used to load or persist application data.
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the start raw for aggregator options. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string? StartRaw { get; set; }
        /// <summary>
        /// Gets or sets the end raw for aggregator options. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string? EndRaw { get; set; }
        /// <summary>
        /// Gets or sets the db user for aggregator options. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string DbUser { get; set; } = "SYSDBA";
        /// <summary>
        /// Gets or sets the db password for aggregator options. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string DbPassword { get; set; } = "masterkey";
        /// <summary>
        /// Gets or sets the batch size for aggregator options. Specifies sizing or boundary constraints used by runtime calculations.
        /// </summary>
        public int BatchSize { get; set; } = 1000;
    }

    /// <summary>
    /// Gets the version string for aggregator options.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    private static string GetVersionString() {
        return $"v{SafetyMonitor.Versioning.BuildVersion.Major}.{SafetyMonitor.Versioning.BuildVersion.Minor}.{SafetyMonitor.Versioning.BuildVersion.Patch} build {SafetyMonitor.Versioning.BuildVersion.Build} {SafetyMonitor.Versioning.BuildVersion.BuildDateUtc:dd.MM.yyyy}";
    }
}
