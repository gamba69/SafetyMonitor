using DataStorage.Models;
using System.CommandLine;
using System.Diagnostics;

namespace SafetyMonitorDataGenerator;

internal static class Program {

    private const int DefaultIntervalSeconds = 60;
    private const int DefaultBatchSize = 1000;

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

        var batchSizeOption = new Option<int>("--batch-size") {
            Description = "Number of records inserted per batch",
            DefaultValueFactory = _ => DefaultBatchSize
        };

        var cleanOption = new Option<bool>("--clean") {
            Description = "Delete existing data in selected time range before generation"
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
        rootCommand.Options.Add(batchSizeOption);
        rootCommand.Options.Add(cleanOption);
        rootCommand.Options.Add(dbUserOption);
        rootCommand.Options.Add(dbPasswordOption);

        var options = new GeneratorOptions();

        rootCommand.SetAction(parseResult => {
            options.StoragePath = parseResult.GetValue(storagePathOption)!;
            options.StartRaw = parseResult.GetValue(startOption);
            options.EndRaw = parseResult.GetValue(endOption);
            options.IntervalSeconds = parseResult.GetValue(intervalOption);
            options.Count = parseResult.GetValue(countOption);
            options.Seed = parseResult.GetValue(seedOption);
            options.BatchSize = parseResult.GetValue(batchSizeOption);
            options.Clean = parseResult.GetValue(cleanOption);
            options.DbUser = parseResult.GetValue(dbUserOption)!;
            options.DbPassword = parseResult.GetValue(dbPasswordOption)!;
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
        var batchSize = options.BatchSize;

        if (options.Count.HasValue && options.Count.Value > 0) {
            endTime = startTime.AddSeconds(interval.TotalSeconds * (options.Count.Value - 1));
        }

        endTime ??= DateTime.UtcNow;

        if (endTime < startTime) {
            Console.Error.WriteLine("End time must be greater than or equal to start time.");
            return Task.FromResult(1);
        }

        if (batchSize <= 0) {
            Console.Error.WriteLine("Batch size must be positive.");
            return Task.FromResult(1);
        }

        var random = options.Seed.HasValue ? new Random(options.Seed.Value) : new Random();

        var storage = new DataStorage.DataStorage(options.StoragePath, options.DbUser, options.DbPassword);

        if (options.Clean) {
            var deletedRecords = storage.DeleteData(startTime, endTime.Value);
            Console.WriteLine($"Cleaned {deletedRecords} records in range {startTime:O} - {endTime:O}.");
        }

        var totalPlanned = CalculateTotalRecords(startTime, endTime.Value, interval);
        var stopwatch = Stopwatch.StartNew();
        var batch = new List<ObservingData>(batchSize);

        var total = 0;
        for (var timestamp = startTime; timestamp <= endTime; timestamp = timestamp.Add(interval)) {
            var data = GenerateData(timestamp, random);
            batch.Add(data);

            if (batch.Count >= batchSize) {
                storage.AddRawDataBatch(batch);
                batch.Clear();
            }

            total++;

            var remainingTime = CalculateRemainingTime(stopwatch.Elapsed, total, totalPlanned);
            Console.Write($"\rGenerated {total}/{totalPlanned}. Elapsed: {FormatDuration(stopwatch.Elapsed)}. Remaining: {FormatDuration(remainingTime)}.");
        }

        if (batch.Count > 0) {
            storage.AddRawDataBatch(batch);
        }

        Console.WriteLine();
        Console.WriteLine("Starting aggregation recalculation...");
        var recalcStopwatch = Stopwatch.StartNew();
        storage.RecalculateAggregations(startTime, endTime.Value, progress => {
            var percent = progress.TotalBuckets <= 0 ? 100d : (progress.ProcessedBuckets * 100d / progress.TotalBuckets);
            var eta = CalculateRemainingTime(recalcStopwatch.Elapsed, progress.ProcessedBuckets, progress.TotalBuckets);
            Console.Write($"\rRecalc {progress.ProcessedBuckets}/{progress.TotalBuckets} ({percent:0.0}%). Level: {progress.Level}. ETA: {FormatDuration(eta)}   ");
        }, options.BatchSize);
        Console.WriteLine();


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
        public int BatchSize { get; set; } = DefaultBatchSize;
        public bool Clean { get; set; }
        public string DbUser { get; set; } = "SYSDBA";
        public string DbPassword { get; set; } = "masterkey";
    }

    private static int CalculateTotalRecords(DateTime startTime, DateTime endTime, TimeSpan interval) {
        if (endTime < startTime) {
            return 0;
        }

        return (int)((endTime - startTime).Ticks / interval.Ticks) + 1;
    }

    private static TimeSpan CalculateRemainingTime(TimeSpan elapsed, int generated, int totalPlanned) {
        if (generated <= 0 || totalPlanned <= generated) {
            return TimeSpan.Zero;
        }

        var avgPerRecord = elapsed.TotalSeconds / generated;
        return TimeSpan.FromSeconds(avgPerRecord * (totalPlanned - generated));
    }

    private static string FormatDuration(TimeSpan duration) {
        if (duration < TimeSpan.Zero) {
            duration = TimeSpan.Zero;
        }

        return $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
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
        var dayOfYear = timestamp.DayOfYear;
        var solarPhase = Math.Sin(2 * Math.PI * (dayOfYear - 81) / 365.2422); // + летом, - зимой
        var daylightHours = 12 + 4 * solarPhase;
        var hour = timestamp.TimeOfDay.TotalHours;
        var solarNoonOffset = 14;
        var diurnalPhase = Math.Cos(2 * Math.PI * (hour - solarNoonOffset) / 24.0);
        var harmonic = Math.Sin(2 * Math.PI * hour / 8.0 + dayOfYear * 0.06) + 0.5 * Math.Sin(2 * Math.PI * hour / 3.2 + dayOfYear * 0.04);

        var clearSkyPeakLux = 22000 + Math.Max(0, solarPhase) * 58000;
        var dayProgress = Math.Abs(hour - 12) / Math.Max(0.1, daylightHours / 2.0);
        var solarElevationFactor = Math.Clamp(1 - Math.Pow(dayProgress, 1.8), 0, 1);

        var cloudCoverBase = 45 + 15 * Math.Sin(2 * Math.PI * dayOfYear / 14.0 + 1.2) + 20 * Math.Sin(2 * Math.PI * dayOfYear / 4.0 + hour / 12.0);
        var cloudCover = Math.Clamp(cloudCoverBase + NextRange(random, -18, 18), 0, 100);

        var cloudOpacity = cloudCover / 100.0;
        var cloudLightLoss = 1 - 0.85 * cloudOpacity;
        var backgroundNightLux = 0.0001 + 0.03 * NextRange(random, 0.0, 1.0);
        var skyBrightness = backgroundNightLux + clearSkyPeakLux * solarElevationFactor * cloudLightLoss;
        skyBrightness *= 1 + 0.03 * harmonic + NextRange(random, -0.05, 0.05);
        skyBrightness = Math.Clamp(skyBrightness, 0.0001, 80000);

        var seasonalAvgTemp = 6 + 15 * solarPhase;
        var dailyTempSwing = 3 + 6 * Math.Max(0, solarPhase);
        var temperature = seasonalAvgTemp + dailyTempSwing * diurnalPhase - 4.5 * cloudOpacity + 1.2 * harmonic + NextRange(random, -1.4, 1.4);
        temperature = Math.Clamp(temperature, -35, 38);

        var pressureTrend = 1015 + 9 * Math.Sin(2 * Math.PI * dayOfYear / 9.0 + hour / 8.0) + 7 * Math.Sin(2 * Math.PI * dayOfYear / 3.7 + 2.1);
        var pressure = Math.Clamp(pressureTrend - 12 * cloudOpacity + NextRange(random, -2.5, 2.5), 970, 1045);

        var humidityBase = 55 + 25 * cloudOpacity - 0.9 * (temperature - 5) + 8 * (1 - solarElevationFactor);
        var humidity = Math.Clamp(humidityBase + NextRange(random, -7, 7), 18, 100);

        var dewSpread = Math.Clamp(12 - humidity / 9.0 + NextRange(random, -1.2, 1.2), 0.5, 16);
        var dewPoint = temperature - dewSpread;

        var windSpeedBase = 1.5 + 5 * (1 - solarElevationFactor) + 4 * cloudOpacity + 1.8 * Math.Abs(Math.Sin(2 * Math.PI * dayOfYear / 2.8));
        var windSpeed = Math.Clamp(windSpeedBase + NextRange(random, -1.2, 1.4), 0, 24);
        var windGust = Math.Clamp(windSpeed + 0.4 + 0.6 * windSpeed + NextRange(random, 0, 3), windSpeed, 35);

        var rainProbability = Math.Clamp(0.03 + 0.55 * Math.Pow(cloudOpacity, 1.6) + 0.18 * (humidity / 100.0), 0, 0.9);
        var rainRate = random.NextDouble() < rainProbability
            ? Math.Max(0, NextRange(random, 0.05, 4.5) * (0.6 + cloudOpacity) * (humidity / 100.0))
            : 0;

        var skyTemperature = Math.Clamp(temperature - 14 - 24 * (1 - cloudOpacity) + NextRange(random, -2.5, 2.5), -60, 18);
        var skyQuality = Math.Clamp(21.9 - 2.2 * solarElevationFactor - 1.4 * cloudOpacity + NextRange(random, -0.25, 0.2), 16, 22);
        var starFwhm = Math.Clamp(1.2 + 0.08 * windSpeed + 0.02 * humidity + 1.3 * cloudOpacity + NextRange(random, -0.2, 0.35), 1, 8);

        var windDirection = (dayOfYear * 0.7 + hour * 12 + 40 * Math.Sin(2 * Math.PI * dayOfYear / 6.0) + NextRange(random, -35, 35)) % 360;
        if (windDirection < 0) {
            windDirection += 360;
        }

        var isSafe = rainRate < 0.1 && windSpeed < 12 && windGust < 18 && cloudCover < 80 && humidity < 92;

        return new ObservingData {
            Timestamp = timestamp,
            CloudCover = cloudCover,
            DewPoint = dewPoint,
            Humidity = humidity,
            Pressure = pressure,
            RainRate = rainRate,
            SkyBrightness = skyBrightness,
            SkyQuality = skyQuality,
            SkyTemperature = skyTemperature,
            StarFwhm = starFwhm,
            Temperature = temperature,
            WindDirection = windDirection,
            WindGust = windGust,
            WindSpeed = windSpeed,
            IsSafe = isSafe,
            Notes = "generated"
        };
    }

    private static double NextRange(Random random, double min, double max) {
        return min + random.NextDouble() * (max - min);
    }

    private static string GetVersionString() {
        return $"v{SafetyMonitor.Versioning.BuildVersion.Major}.{SafetyMonitor.Versioning.BuildVersion.Minor}.{SafetyMonitor.Versioning.BuildVersion.Patch} build {SafetyMonitor.Versioning.BuildVersion.Build} {SafetyMonitor.Versioning.BuildVersion.BuildDateUtc:dd.MM.yyyy}";
    }
}
