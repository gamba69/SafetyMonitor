using System.Diagnostics;
using System.CommandLine;

namespace DataAggregator;

internal static class Program {

    private static async Task<int> Main(string[] args) {
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
            Console.Write($"\rRecalc {progress.ProcessedBuckets}/{progress.TotalBuckets} ({percent:0.0}%). Level: {progress.Level}. ETA: {FormatDuration(eta)}   ");
        }, options.BatchSize);

        Console.WriteLine();
        Console.WriteLine($"Recalculated aggregations for range {startTime:O} - {endTime:O}");
        return Task.FromResult(0);
    }

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


    private static TimeSpan CalculateRemainingTime(TimeSpan elapsed, int done, int total) {
        if (done <= 0 || total <= done) {
            return TimeSpan.Zero;
        }

        var avgPerItem = elapsed.TotalSeconds / done;
        return TimeSpan.FromSeconds(avgPerItem * (total - done));
    }

    private static string FormatDuration(TimeSpan duration) {
        if (duration < TimeSpan.Zero) {
            duration = TimeSpan.Zero;
        }

        return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private sealed class AggregatorOptions {
        public string StoragePath { get; set; } = string.Empty;
        public string? StartRaw { get; set; }
        public string? EndRaw { get; set; }
        public string DbUser { get; set; } = "SYSDBA";
        public string DbPassword { get; set; } = "masterkey";
        public int BatchSize { get; set; } = 1000;
    }
}
