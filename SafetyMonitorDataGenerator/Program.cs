using DataStorage.Models;
using System.CommandLine;

namespace SafetyMonitorDataGenerator;

internal static class Program {

    private const int DefaultIntervalSeconds = 60;

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

        var intervalOption = new Option<int>("--interval") {
            Description = "Interval between records in seconds",
            DefaultValueFactory = _ => DefaultIntervalSeconds
        };

        var countOption = new Option<int?>("--count") {
            Description = "Number of records to generate (overrides end timestamp if provided)"
        };

        var seedOption = new Option<int?>("--seed") {
            Description = "Optional random seed for deterministic output"
        };

        var dbUserOption = new Option<string>("--db-user") {
            Description = "Database user name",
            DefaultValueFactory = _ => "SYSDBA"
        };

        var dbPasswordOption = new Option<string>("--db-password") {
            Description = "Database password",
            DefaultValueFactory = _ => "masterkey"
        };

        var rootCommand = new RootCommand("Generate synthetic ObservingConditions/SafetyMonitor data and store it in DataStorage");

        rootCommand.Options.Add(storagePathOption);
        rootCommand.Options.Add(startOption);
        rootCommand.Options.Add(endOption);
        rootCommand.Options.Add(intervalOption);
        rootCommand.Options.Add(countOption);
        rootCommand.Options.Add(seedOption);
        rootCommand.Options.Add(dbUserOption);
        rootCommand.Options.Add(dbPasswordOption);

        var options = new GeneratorOptions();

        rootCommand.SetAction(parseResult => {
            options.StoragePath = parseResult.GetValue(storagePathOption);
            options.StartRaw = parseResult.GetValue(startOption);
            options.EndRaw = parseResult.GetValue(endOption);
            options.IntervalSeconds = parseResult.GetValue(intervalOption);
            options.Count = parseResult.GetValue(countOption);
            options.Seed = parseResult.GetValue(seedOption);
            options.DbUser = parseResult.GetValue(dbUserOption);
            options.DbPassword = parseResult.GetValue(dbPasswordOption);
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

        return await GenerateAsync(options);
    }

    private static Task<int> GenerateAsync(GeneratorOptions options) {
        var interval = TimeSpan.FromSeconds(options.IntervalSeconds);
        if (interval <= TimeSpan.Zero) {
            Console.Error.WriteLine("Interval must be positive.");
            return Task.FromResult(1);
        }

        var startTime = ParseTimestamp(options.StartRaw) ?? DateTime.UtcNow.AddDays(-1);
        var endTime = ParseTimestamp(options.EndRaw);

        if (options.Count.HasValue && options.Count.Value > 0) {
            endTime = startTime.AddSeconds(interval.TotalSeconds * (options.Count.Value - 1));
        }

        endTime ??= DateTime.UtcNow;

        if (endTime < startTime) {
            Console.Error.WriteLine("End time must be greater than or equal to start time.");
            return Task.FromResult(1);
        }

        var random = options.Seed.HasValue ? new Random(options.Seed.Value) : new Random();

        var storage = new DataStorage.DataStorage(options.StoragePath, options.DbUser, options.DbPassword);

        var total = 0;
        for (var timestamp = startTime; timestamp <= endTime; timestamp = timestamp.Add(interval)) {
            var data = GenerateData(timestamp, random);
            storage.AddData(data);
            total++;
        }

        Console.WriteLine($"Generated {total} records from {startTime:O} to {endTime:O}.");
        return Task.FromResult(0);
    }

    private sealed class GeneratorOptions {
        public string StoragePath { get; set; } = string.Empty;
        public string? StartRaw { get; set; }
        public string? EndRaw { get; set; }
        public int IntervalSeconds { get; set; } = DefaultIntervalSeconds;
        public int? Count { get; set; }
        public int? Seed { get; set; }
        public string DbUser { get; set; } = "SYSDBA";
        public string DbPassword { get; set; } = "masterkey";
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

    private static ObservingData GenerateData(DateTime timestamp, Random random) {
        var temperature = NextRange(random, -10, 25);
        var humidity = NextRange(random, 20, 95);
        var cloudCover = NextRange(random, 0, 100);
        var windSpeed = NextRange(random, 0, 15);
        var rainRate = random.NextDouble() < 0.1 ? NextRange(random, 0.2, 5) : 0;

        var isSafe = rainRate < 0.1 && windSpeed < 12 && cloudCover < 70;

        return new ObservingData {
            Timestamp = timestamp,
            CloudCover = cloudCover,
            DewPoint = temperature - NextRange(random, 0, 10),
            Humidity = humidity,
            Pressure = NextRange(random, 990, 1035),
            RainRate = rainRate,
            SkyBrightness = NextRange(random, 0, 500),
            SkyQuality = NextRange(random, 16, 22),
            SkyTemperature = NextRange(random, -30, 10),
            StarFwhm = NextRange(random, 1, 6),
            Temperature = temperature,
            WindDirection = NextRange(random, 0, 360),
            WindGust = windSpeed + NextRange(random, 0, 8),
            WindSpeed = windSpeed,
            IsSafe = isSafe,
            Notes = "generated"
        };
    }

    private static double NextRange(Random random, double min, double max) {
        return min + random.NextDouble() * (max - min);
    }
}
