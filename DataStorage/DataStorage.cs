using Dapper;
using DataStorage.Models;
using FirebirdSql.Data.FirebirdClient;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Text;

namespace DataStorage;

/// <summary>
/// Represents data storage and encapsulates its related behavior and state.
/// </summary>
public class DataStorage {

    public enum StorageValidationIssueSeverity {
        Warning,
        Error
    }

    /// <summary>
    /// Represents storage validation issue and encapsulates its related behavior and state.
    /// </summary>
    public sealed record StorageValidationIssue(StorageValidationIssueSeverity Severity, string Message);

    /// <summary>
    /// Represents storage validation result and encapsulates its related behavior and state.
    /// </summary>
    public sealed class StorageValidationResult {
        /// <summary>
        /// Gets or sets the issues for storage validation result. Represents a state flag that enables or disables related behavior.
        /// </summary>
        public List<StorageValidationIssue> Issues { get; } = [];
        public bool HasWarnings => Issues.Any(x => x.Severity == StorageValidationIssueSeverity.Warning);
        public bool HasErrors => Issues.Any(x => x.Severity == StorageValidationIssueSeverity.Error);
    }

    private const string DbExt = ".fdb";
    private const int RecalcRawReadBatchSize = 20000;
    private readonly string _root;
    private readonly string _user;
    private readonly string _password;

    private readonly ConcurrentDictionary<string, byte> _ensuredRawDbs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _ensuredMonthlyAggDbs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _ensuredYearAggDbs = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _ensuredRawTables = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _ensuredAggTables = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> MergeAggSqlCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> UpsertAggSqlCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Represents bucket state and encapsulates its related behavior and state.
    /// </summary>
    private sealed class BucketState {
        /// <summary>
        /// Gets or sets the sum for bucket state. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double[] Sum { get; } = new double[Metrics.Length];
        /// <summary>
        /// Gets or sets the count for bucket state. Specifies sizing or boundary constraints used by runtime calculations.
        /// </summary>
        public int[] Count { get; } = new int[Metrics.Length];
        /// <summary>
        /// Gets or sets the min for bucket state. Specifies sizing or boundary constraints used by runtime calculations.
        /// </summary>
        public double?[] Min { get; } = new double?[Metrics.Length];
        /// <summary>
        /// Gets or sets the max for bucket state. Specifies sizing or boundary constraints used by runtime calculations.
        /// </summary>
        public double?[] Max { get; } = new double?[Metrics.Length];
        /// <summary>
        /// Gets or sets the first for bucket state. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double?[] First { get; } = new double?[Metrics.Length];
        /// <summary>
        /// Gets or sets the last for bucket state. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double?[] Last { get; } = new double?[Metrics.Length];
    }

    /// <summary>
    /// Represents recalc level context and encapsulates its related behavior and state.
    /// </summary>
    private sealed class RecalcLevelContext {
        /// <summary>
        /// Gets or sets the name for recalc level context. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public required string Name { get; init; }
        /// <summary>
        /// Gets or sets the span for recalc level context. Stores a duration value used by scheduling and aggregation logic.
        /// </summary>
        public required TimeSpan Span { get; init; }
        /// <summary>
        /// Gets or sets the monthly for recalc level context. Represents a state flag that enables or disables related behavior.
        /// </summary>
        public required bool Monthly { get; init; }
        /// <summary>
        /// Gets or sets the end bucket for recalc level context. Stores a timestamp used for ordering, filtering, or range calculations.
        /// </summary>
        public required DateTime EndBucket { get; init; }
        /// <summary>
        /// Gets or sets the upsert sql for recalc level context. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public required string UpsertSql { get; init; }
        /// <summary>
        /// Gets or sets the current bucket for recalc level context. Stores a timestamp used for ordering, filtering, or range calculations.
        /// </summary>
        public required DateTime CurrentBucket { get; set; }
        /// <summary>
        /// Gets or sets the state for recalc level context. Holds part of the component state used by higher-level application logic.
        /// </summary>
        public required BucketState State { get; set; }
        /// <summary>
        /// Gets or sets the pending rows for recalc level context. Stores a timestamp used for ordering, filtering, or range calculations.
        /// </summary>
        public List<KeyValuePair<DateTime, BucketState>> PendingRows { get; } = [];
    }

    /// <summary>
    /// Performs the aggregation recalc progress operation.
    /// </summary>
    public readonly record struct AggregationRecalcProgress(
        int ProcessedBuckets,
        int TotalBuckets,
        string Level,
        DateTime BucketTimestamp);

    /// <summary>
    /// Represents metric def and encapsulates its related behavior and state.
    /// </summary>
    private sealed record MetricDef(string Name, FbDbType RawType, string RawSqlType, string AggSqlType);
    private static readonly MetricDef[] Metrics = [
        new("CLOUD_COVER", FbDbType.SmallInt, "SMALLINT", "FLOAT"),
        new("IS_SAFE", FbDbType.SmallInt, "SMALLINT", "FLOAT"),
        new("DEW_POINT", FbDbType.Float, "FLOAT", "FLOAT"),
        new("HUMIDITY", FbDbType.Float, "FLOAT", "FLOAT"),
        new("PRESSURE", FbDbType.Float, "FLOAT", "FLOAT"),
        new("SKY_BRIGHTNESS", FbDbType.Double, "DOUBLE PRECISION", "DOUBLE PRECISION"),
        new("RAIN_RATE", FbDbType.Float, "FLOAT", "FLOAT"),
        new("SKY_QUALITY", FbDbType.Float, "FLOAT", "FLOAT"),
        new("SKY_TEMPERATURE", FbDbType.Float, "FLOAT", "FLOAT"),
        new("STAR_FWHM", FbDbType.Float, "FLOAT", "FLOAT"),
        new("TEMPERATURE", FbDbType.Float, "FLOAT", "FLOAT"),
        new("WIND_DIRECTION", FbDbType.Float, "FLOAT", "FLOAT"),
        new("WIND_GUST", FbDbType.Float, "FLOAT", "FLOAT"),
        new("WIND_SPEED", FbDbType.Float, "FLOAT", "FLOAT")
    ];

    private static readonly (string Name, TimeSpan Span, bool Monthly)[] Levels = [
        ("RAW", TimeSpan.FromSeconds(3), true),
        ("10S", TimeSpan.FromSeconds(10), true),
        ("30S", TimeSpan.FromSeconds(30), true),
        ("1M", TimeSpan.FromMinutes(1), true),
        ("5M", TimeSpan.FromMinutes(5), true),
        ("15M", TimeSpan.FromMinutes(15), true),
        ("1H", TimeSpan.FromHours(1), false),
        ("4H", TimeSpan.FromHours(4), false),
        ("12H", TimeSpan.FromHours(12), false),
        ("1D", TimeSpan.FromDays(1), false),
        ("3D", TimeSpan.FromDays(3), false),
        ("1W", TimeSpan.FromDays(7), false)
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStorage"/> class.
    /// </summary>
    /// <param name="storageRootPath">Path value for storage root path.</param>
    /// <param name="userName">Input value for user name.</param>
    /// <param name="password">Input value for password.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public DataStorage(string storageRootPath, string? userName = null, string? password = null) {
        if (string.IsNullOrWhiteSpace(storageRootPath)) {
            throw new ArgumentException("Storage root path cannot be null or empty", nameof(storageRootPath));
        }

        _root = storageRootPath;
        _user = string.IsNullOrWhiteSpace(userName) ? "SYSDBA" : userName;
        _password = string.IsNullOrWhiteSpace(password) ? "masterkey" : password;
        Directory.CreateDirectory(_root);
    }

    /// <summary>
    /// Adds the data for metric def.
    /// </summary>
    /// <param name="data">Input value for data.</param>
    public void AddData(ObservingData data) {
        ArgumentNullException.ThrowIfNull(data);
        AddDataBatch([data]);
    }

    /// <summary>
    /// Adds the raw data batch for metric def.
    /// </summary>
    /// <param name="batch">Collection of batch items used by the operation.</param>
    public void AddRawDataBatch(IReadOnlyCollection<ObservingData> batch) {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Count == 0) {
            return;
        }

        var normalized = batch
            .Select(x => new { Data = x, Ts = FloorToSecond(x.Timestamp) })
            .GroupBy(x => new DateTime(x.Ts.Year, x.Ts.Month, 1));

        foreach (var monthGroup in normalized) {
            var ts = monthGroup.Key;
            var rawDb = GetRawDbPath(ts);
            EnsureRawDatabase(rawDb);

            using var rawConn = new FbConnection(GetConnectionString(rawDb));
            rawConn.Open();
            using var rawTx = rawConn.BeginTransaction();

            const string insertSql = @"INSERT INTO METEO_RAW (
CREATED_AT,CLOUD_COVER,IS_SAFE,DEW_POINT,HUMIDITY,PRESSURE,SKY_BRIGHTNESS,RAIN_RATE,SKY_QUALITY,SKY_TEMPERATURE,STAR_FWHM,TEMPERATURE,WIND_DIRECTION,WIND_GUST,WIND_SPEED
) VALUES (
@CreatedAt,@CloudCover,@IsSafeInt,@DewPoint,@Humidity,@Pressure,@SkyBrightness,@RainRate,@SkyQuality,@SkyTemperature,@StarFwhm,@Temperature,@WindDirection,@WindGust,@WindSpeed
)";

            var rows = monthGroup.Select(item => CreateRawMergeParams(item.Data, item.Ts)).ToList();
            rawConn.Execute(insertSql, rows, rawTx);
            rawTx.Commit();
        }
    }

    /// <summary>
    /// Adds the data batch for metric def.
    /// </summary>
    /// <param name="batch">Collection of batch items used by the operation.</param>
    public void AddDataBatch(IReadOnlyCollection<ObservingData> batch) {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Count == 0) {
            return;
        }

        var normalized = batch
            .Select(x => new { Data = x, Ts = FloorToSecond(x.Timestamp) })
            .GroupBy(x => new DateTime(x.Ts.Year, x.Ts.Month, 1));

        foreach (var monthGroup in normalized) {
            var ts = monthGroup.Key;
            var rawDb = GetRawDbPath(ts);
            var monthAggDb = GetMonthlyAggDbPath(ts);
            var yearAggDb = GetYearAggDbPath(ts.Year);

            EnsureRawDatabase(rawDb);
            EnsureMonthlyAggDatabase(monthAggDb);
            EnsureYearAggDatabase(yearAggDb);

            using var rawConn = new FbConnection(GetConnectionString(rawDb));
            using var monthAggConn = new FbConnection(GetConnectionString(monthAggDb));
            using var yearAggConn = new FbConnection(GetConnectionString(yearAggDb));
            rawConn.Open(); monthAggConn.Open(); yearAggConn.Open();
            using var rawTx = rawConn.BeginTransaction();
            using var monthTx = monthAggConn.BeginTransaction();
            using var yearTx = yearAggConn.BeginTransaction();

            var materialized = monthGroup.ToList();

            // Batch raw MERGE calls in one Dapper execution pass
            var rawRows = materialized.Select(item => CreateRawMergeParams(item.Data, item.Ts)).ToList();
            if (rawRows.Count > 0) {
                rawConn.Execute(MergeRawSql, rawRows, rawTx);
            }

            // Batch aggregation MERGE calls per level (significantly reduces overhead)
            foreach (var level in Levels.Skip(1)) {
                var aggRows = materialized
                    .Select(item => CreateAggMergeParams(item.Data, AlignToBucket(item.Ts, level.Name)))
                    .ToList();

                if (aggRows.Count == 0) {
                    continue;
                }

                var targetConn = level.Monthly ? monthAggConn : yearAggConn;
                var targetTx = level.Monthly ? monthTx : yearTx;
                var sql = MergeAggSqlCache.GetOrAdd(level.Name, BuildMergeAggSql);
                targetConn.Execute(sql, aggRows, targetTx);
            }

            rawTx.Commit();
            monthTx.Commit();
            yearTx.Commit();
        }
    }

    /// <summary>
    /// Gets the data for metric def.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <param name="slotDuration">Input value for slot duration.</param>
    /// <param name="aggregationFunction">Input value for aggregation function.</param>
    /// <returns>The result of the operation.</returns>
    public List<ObservingData> GetData(DateTime startTime, DateTime endTime, TimeSpan? slotDuration = null, AggregationFunction aggregationFunction = AggregationFunction.Average) {
        if (endTime < startTime) {
            throw new ArgumentException("End time must be greater than or equal to start time");
        }

        if (slotDuration is null) {
            return GetRawData(startTime, endTime);
        }

        var level = MapSlotDuration(slotDuration.Value);
        if (level == "RAW") {
            return GetRawData(startTime, endTime);
        }

        return GetAggregatedData(startTime, endTime, level, slotDuration.Value, aggregationFunction);
    }

    /// <summary>
    /// Deletes the data for metric def.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <returns>The result of the operation.</returns>
    public int DeleteData(DateTime startTime, DateTime endTime) {
        if (endTime < startTime) {
            throw new ArgumentException("End time must be greater than or equal to start time");
        }

        var deleted = 0;
        for (var d = new DateTime(startTime.Year, startTime.Month, 1); d <= endTime; d = d.AddMonths(1)) {
            var db = GetRawDbPath(d);
            if (!File.Exists(db)) {
                continue;
            }

            using var conn = new FbConnection(GetConnectionString(db));
            conn.Open();
            deleted += conn.Execute("DELETE FROM METEO_RAW WHERE CREATED_AT >= @startTime AND CREATED_AT <= @endTime", new { startTime, endTime });
        }

        return deleted;
    }

    /// <summary>
    /// Gets the latest data for metric def.
    /// </summary>
    /// <param name="endTime">Input value for end time.</param>
    /// <param name="maxLookback">Input value for max lookback.</param>
    /// <returns>The result of the operation.</returns>
    public ObservingData? GetLatestData(DateTime endTime, TimeSpan maxLookback) {
        if (maxLookback <= TimeSpan.Zero) {
            throw new ArgumentException("Max lookback must be positive", nameof(maxLookback));
        }

        var start = endTime - maxLookback;
        for (var month = new DateTime(endTime.Year, endTime.Month, 1); month >= new DateTime(start.Year, start.Month, 1); month = month.AddMonths(-1)) {
            var db = GetRawDbPath(month);
            if (!File.Exists(db)) {
                continue;
            }

            using var conn = new FbConnection(GetConnectionString(db));
            conn.Open();
            const string sql = @"SELECT CREATED_AT AS ""Timestamp"", CLOUD_COVER AS CloudCover, DEW_POINT AS DewPoint, HUMIDITY AS Humidity, PRESSURE AS Pressure,
RAIN_RATE AS RainRate, SKY_BRIGHTNESS AS SkyBrightness, SKY_QUALITY AS SkyQuality, SKY_TEMPERATURE AS SkyTemperature, STAR_FWHM AS StarFwhm,
TEMPERATURE AS Temperature, WIND_DIRECTION AS WindDirection, WIND_GUST AS WindGust, WIND_SPEED AS WindSpeed, CAST(IS_SAFE AS INTEGER) AS IsSafeInt
FROM METEO_RAW WHERE CREATED_AT >= @start AND CREATED_AT <= @endTime ORDER BY CREATED_AT DESC ROWS 1";
            var row = conn.QueryFirstOrDefault<ObservingData>(sql, new { start, endTime });
            if (row != null) {
                return row;
            }
        }

        return null;
    }

    /// <summary>
    /// Executes recalculate aggregations as part of metric def processing.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <param name="progress">Input value for progress.</param>
    /// <param name="upsertBatchSize">Input value for upsert batch size.</param>
    public void RecalculateAggregations(DateTime startTime, DateTime endTime, Action<AggregationRecalcProgress>? progress = null, int upsertBatchSize = 1000) {
        if (endTime < startTime) {
            throw new ArgumentException("End time must be greater than or equal to start time");
        }

        upsertBatchSize = Math.Max(1, upsertBatchSize);

        var levels = Levels.Skip(1).ToList();
        var totalBuckets = levels.Sum(level => CountBucketsInRange(startTime, endTime, level.Name));
        var processedBuckets = 0;

        var contexts = levels.Select(level => {
            var startBucket = AlignToBucket(startTime, level.Name);
            var initialState = BuildSingleBucketState(GetRawData(startBucket, startTime.AddSeconds(-1)));

            return new RecalcLevelContext {
                Name = level.Name,
                Span = level.Span,
                Monthly = level.Monthly,
                EndBucket = AlignToBucket(endTime, level.Name),
                UpsertSql = UpsertAggSqlCache.GetOrAdd(level.Name, BuildUpsertAggSql),
                CurrentBucket = startBucket,
                State = initialState
            };
        }).ToList();

        foreach (var rawBatch in EnumerateRawDataBatches(startTime, endTime, RecalcRawReadBatchSize)) {
            foreach (var row in rawBatch) {
                var ts = FloorToSecond(row.Timestamp);
                foreach (var context in contexts) {
                    var rowBucket = AlignToBucket(ts, context.Name);

                    while (context.CurrentBucket < rowBucket) {
                        processedBuckets += QueueBucket(context, context.CurrentBucket, context.State, upsertBatchSize);
                        progress?.Invoke(new AggregationRecalcProgress(processedBuckets, totalBuckets, context.Name, context.CurrentBucket));
                        context.CurrentBucket = context.CurrentBucket.Add(context.Span);
                        context.State = new BucketState();
                    }

                    AggregateRow(context.State, row);
                }
            }
        }

        foreach (var context in contexts) {
            while (context.CurrentBucket < context.EndBucket) {
                processedBuckets += QueueBucket(context, context.CurrentBucket, context.State, upsertBatchSize);
                progress?.Invoke(new AggregationRecalcProgress(processedBuckets, totalBuckets, context.Name, context.CurrentBucket));
                context.CurrentBucket = context.CurrentBucket.Add(context.Span);
                context.State = new BucketState();
            }

            var tailStart = endTime.AddSeconds(1);
            var tailEndExclusive = context.CurrentBucket.Add(context.Span);
            MergeStates(context.State, BuildSingleBucketState(GetRawData(tailStart, tailEndExclusive.AddSeconds(-1))));

            processedBuckets += QueueBucket(context, context.CurrentBucket, context.State, upsertBatchSize);
            progress?.Invoke(new AggregationRecalcProgress(processedBuckets, totalBuckets, context.Name, context.CurrentBucket));
            FlushPendingRows(context);
        }

    }

    /// <summary>
    /// Executes queue bucket as part of metric def processing.
    /// </summary>
    /// <param name="context">Input value for context.</param>
    /// <param name="bucketTs">Input value for bucket ts.</param>
    /// <param name="state">Input value for state.</param>
    /// <param name="upsertBatchSize">Input value for upsert batch size.</param>
    /// <returns>The result of the operation.</returns>
    private int QueueBucket(RecalcLevelContext context, DateTime bucketTs, BucketState state, int upsertBatchSize) {
        context.PendingRows.Add(new KeyValuePair<DateTime, BucketState>(bucketTs, state));
        if (context.PendingRows.Count >= upsertBatchSize) {
            FlushPendingRows(context);
        }

        return 1;
    }

    /// <summary>
    /// Executes flush pending rows as part of metric def processing.
    /// </summary>
    /// <param name="context">Input value for context.</param>
    private void FlushPendingRows(RecalcLevelContext context) {
        if (context.PendingRows.Count == 0) {
            return;
        }

        var grouped = context.PendingRows
            .OrderBy(x => x.Key)
            .GroupBy(x => context.Monthly ? GetMonthlyAggDbPath(x.Key) : GetYearAggDbPath(x.Key.Year));

        foreach (var dbGroup in grouped) {
            var dbPath = dbGroup.Key;
            if (context.Monthly) {
                EnsureMonthlyAggDatabase(dbPath);
            } else {
                EnsureYearAggDatabase(dbPath);
            }

            using var conn = new FbConnection(GetConnectionString(dbPath));
            conn.Open();
            using var tx = conn.BeginTransaction();

            var rows = dbGroup.Select(x => CreateAggUpsertParams(x.Key, x.Value)).ToList();
            conn.Execute(context.UpsertSql, rows, tx);
            tx.Commit();
        }

        context.PendingRows.Clear();
    }

    /// <summary>
    /// Executes merge states as part of metric def processing.
    /// </summary>
    /// <param name="target">Input value for target.</param>
    /// <param name="source">Input value for source.</param>
    private static void MergeStates(BucketState target, BucketState source) {
        for (var i = 0; i < Metrics.Length; i++) {
            target.Sum[i] += source.Sum[i];
            target.Count[i] += source.Count[i];

            if (source.Min[i].HasValue) {
                target.Min[i] = !target.Min[i].HasValue || source.Min[i]!.Value < target.Min[i]!.Value ? source.Min[i] : target.Min[i];
            }

            if (source.Max[i].HasValue) {
                target.Max[i] = !target.Max[i].HasValue || source.Max[i]!.Value > target.Max[i]!.Value ? source.Max[i] : target.Max[i];
            }

            target.First[i] ??= source.First[i];
            if (source.Last[i].HasValue) {
                target.Last[i] = source.Last[i];
            }
        }
    }

    /// <summary>
    /// Executes aggregate row as part of metric def processing.
    /// </summary>
    /// <param name="state">Input value for state.</param>
    /// <param name="row">Input value for row.</param>
    private static void AggregateRow(BucketState state, ObservingData row) {
        for (var i = 0; i < Metrics.Length; i++) {
            var value = GetMetricValue(row, i);
            if (!value.HasValue) {
                continue;
            }

            var v = value.Value;
            state.Sum[i] += v;
            state.Count[i] += 1;
            state.Min[i] = !state.Min[i].HasValue || v < state.Min[i] ? v : state.Min[i];
            state.Max[i] = !state.Max[i].HasValue || v > state.Max[i] ? v : state.Max[i];
            state.First[i] ??= v;
            state.Last[i] = v;
        }
    }

    /// <summary>
    /// Builds the single bucket state for metric def.
    /// </summary>
    /// <param name="bucketRows">Collection of bucket rows items used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    private static BucketState BuildSingleBucketState(IEnumerable<ObservingData> bucketRows) {
        var state = new BucketState();
        foreach (var row in bucketRows) {
            AggregateRow(state, row);
        }

        return state;
    }

    /// <summary>
    /// Loads the raw bucket for metric def.
    /// </summary>
    /// <param name="levelName">Input value for level name.</param>
    /// <param name="bucketStart">Input value for bucket start.</param>
    /// <returns>The result of the operation.</returns>
    private List<ObservingData> LoadRawBucket(string levelName, DateTime bucketStart) {
        var span = Levels.First(x => x.Name == levelName).Span;
        var bucketEnd = bucketStart.Add(span).AddSeconds(-1);
        return GetRawData(bucketStart, bucketEnd);
    }

    /// <summary>
    /// Executes count buckets in range as part of metric def processing.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <param name="levelName">Input value for level name.</param>
    /// <returns>The result of the operation.</returns>
    private static int CountBucketsInRange(DateTime startTime, DateTime endTime, string levelName) {
        var startBucket = AlignToBucket(startTime, levelName);
        var endBucket = AlignToBucket(endTime, levelName);
        var span = Levels.First(x => x.Name == levelName).Span;
        var ticks = span.Ticks;
        if (ticks <= 0) {
            return 0;
        }

        return (int)(((endBucket - startBucket).Ticks / ticks) + 1);
    }

    /// <summary>
    /// Gets the metric value for metric def.
    /// </summary>
    /// <param name="row">Input value for row.</param>
    /// <param name="metricIndex">Input value for metric index.</param>
    /// <returns>The result of the operation.</returns>
    private static double? GetMetricValue(ObservingData row, int metricIndex) {
        return metricIndex switch {
            0 => row.CloudCover,
            1 => row.IsSafeInt,
            2 => row.DewPoint,
            3 => row.Humidity,
            4 => row.Pressure,
            5 => row.SkyBrightness,
            6 => row.RainRate,
            7 => row.SkyQuality,
            8 => row.SkyTemperature,
            9 => row.StarFwhm,
            10 => row.Temperature,
            11 => row.WindDirection,
            12 => row.WindGust,
            13 => row.WindSpeed,
            _ => null
        };
    }

    /// <summary>
    /// Builds the upsert agg sql for metric def.
    /// </summary>
    /// <param name="level">Input value for level.</param>
    /// <returns>The resulting string value.</returns>
    private static string BuildUpsertAggSql(string level) {
        var columns = new List<string> { "CREATED_AT" };
        foreach (var m in Metrics) {
            columns.AddRange([$"{m.Name}_SUM", $"{m.Name}_COUNT", $"{m.Name}_AVG", $"{m.Name}_MIN", $"{m.Name}_MAX", $"{m.Name}_FIRST", $"{m.Name}_LAST"]);
        }

        var values = new List<string> { "@CreatedAt" };
        foreach (var m in Metrics) {
            values.AddRange([$"@{m.Name}_SUM", $"@{m.Name}_COUNT", $"@{m.Name}_AVG", $"@{m.Name}_MIN", $"@{m.Name}_MAX", $"@{m.Name}_FIRST", $"@{m.Name}_LAST"]);
        }

        return $"UPDATE OR INSERT INTO METEO_AGG_{level} ({string.Join(",", columns)}) VALUES ({string.Join(",", values)}) MATCHING (CREATED_AT)";
    }

    /// <summary>
    /// Creates the agg upsert params for metric def.
    /// </summary>
    /// <param name="bucketTs">Input value for bucket ts.</param>
    /// <param name="state">Input value for state.</param>
    /// <returns>The result of the operation.</returns>
    private static DynamicParameters CreateAggUpsertParams(DateTime bucketTs, BucketState state) {
        var p = new DynamicParameters();
        p.Add("CreatedAt", bucketTs, DbType.DateTime);

        for (var i = 0; i < Metrics.Length; i++) {
            var m = Metrics[i].Name;
            var count = state.Count[i];
            var sum = state.Sum[i];
            var avg = count > 0 ? sum / count : (double?)null;

            p.Add($"{m}_SUM", count > 0 ? sum : (double?)null, DbType.Double);
            p.Add($"{m}_COUNT", count, DbType.Int32);
            p.Add($"{m}_AVG", avg, DbType.Double);
            p.Add($"{m}_MIN", state.Min[i], DbType.Double);
            p.Add($"{m}_MAX", state.Max[i], DbType.Double);
            p.Add($"{m}_FIRST", state.First[i], DbType.Double);
            p.Add($"{m}_LAST", state.Last[i], DbType.Double);
        }

        return p;
    }

    private static DateTime FloorToSecond(DateTime ts) => new(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, ts.Second, ts.Kind);

    /// <summary>
    /// Executes align to bucket as part of metric def processing.
    /// </summary>
    /// <param name="ts">Input value for ts.</param>
    /// <param name="level">Input value for level.</param>
    /// <returns>The result of the operation.</returns>
    private static DateTime AlignToBucket(DateTime ts, string level) {
        if (level == "3D") {
            var anchor = new DateTime(2000, 1, 1, 0, 0, 0, ts.Kind);
            var daySpan = (int)Math.Floor((ts.Date - anchor.Date).TotalDays / 3d) * 3;
            return anchor.AddDays(daySpan);
        }

        if (level == "1W") {
            var date = ts.Date;
            var delta = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-delta);
        }

        var span = Levels.First(x => x.Name == level).Span;
        var seconds = (long)span.TotalSeconds;
        var unix = new DateTimeOffset(ts).ToUnixTimeSeconds();
        var aligned = unix - (unix % seconds);
        return DateTimeOffset.FromUnixTimeSeconds(aligned).UtcDateTime;
    }

    private const string MergeRawSql = @"MERGE INTO METEO_RAW t
USING (SELECT CAST(@CreatedAt AS TIMESTAMP) CREATED_AT FROM RDB$DATABASE) s
ON (t.CREATED_AT = s.CREATED_AT)
WHEN MATCHED THEN UPDATE SET
    CLOUD_COVER = @CloudCover,
    IS_SAFE = @IsSafeInt,
    DEW_POINT = @DewPoint,
    HUMIDITY = @Humidity,
    PRESSURE = @Pressure,
    SKY_BRIGHTNESS = @SkyBrightness,
    RAIN_RATE = @RainRate,
    SKY_QUALITY = @SkyQuality,
    SKY_TEMPERATURE = @SkyTemperature,
    STAR_FWHM = @StarFwhm,
    TEMPERATURE = @Temperature,
    WIND_DIRECTION = @WindDirection,
    WIND_GUST = @WindGust,
    WIND_SPEED = @WindSpeed
WHEN NOT MATCHED THEN INSERT (
    CREATED_AT,CLOUD_COVER,IS_SAFE,DEW_POINT,HUMIDITY,PRESSURE,SKY_BRIGHTNESS,RAIN_RATE,SKY_QUALITY,SKY_TEMPERATURE,STAR_FWHM,TEMPERATURE,WIND_DIRECTION,WIND_GUST,WIND_SPEED
) VALUES (
    @CreatedAt,@CloudCover,@IsSafeInt,@DewPoint,@Humidity,@Pressure,@SkyBrightness,@RainRate,@SkyQuality,@SkyTemperature,@StarFwhm,@Temperature,@WindDirection,@WindGust,@WindSpeed
)";

    /// <summary>
    /// Creates the raw merge params for metric def.
    /// </summary>
    /// <param name="d">Input value for d.</param>
    /// <param name="ts">Input value for ts.</param>
    /// <returns>The result of the operation.</returns>
    private static object CreateRawMergeParams(ObservingData d, DateTime ts) {
        return new {
            CreatedAt = ts,
            CloudCover = d.CloudCover is null ? (short?)null : Convert.ToInt16(Math.Round(d.CloudCover.Value)),
            IsSafeInt = d.IsSafeInt is null ? (short?)null : Convert.ToInt16(d.IsSafeInt.Value),
            d.DewPoint,
            d.Humidity,
            d.Pressure,
            d.SkyBrightness,
            d.RainRate,
            d.SkyQuality,
            d.SkyTemperature,
            d.StarFwhm,
            d.Temperature,
            d.WindDirection,
            d.WindGust,
            d.WindSpeed
        };
    }

    /// <summary>
    /// Creates the agg merge params for metric def.
    /// </summary>
    /// <param name="d">Input value for d.</param>
    /// <param name="bucketTs">Input value for bucket ts.</param>
    /// <returns>The result of the operation.</returns>
    private static DynamicParameters CreateAggMergeParams(ObservingData d, DateTime bucketTs) {
        var p = new DynamicParameters();
        p.Add("CreatedAt", bucketTs, DbType.DateTime);
        AddMetricParams(p, d);
        return p;
    }

    /// <summary>
    /// Adds the metric params for metric def.
    /// </summary>
    /// <param name="p">Input value for p.</param>
    /// <param name="d">Input value for d.</param>
    private static void AddMetricParams(DynamicParameters p, ObservingData d) {
        p.Add("CLOUD_COVER", d.CloudCover, DbType.Double);
        p.Add("IS_SAFE", d.IsSafeInt is null ? null : Convert.ToDouble(d.IsSafeInt.Value, CultureInfo.InvariantCulture), DbType.Double);
        p.Add("DEW_POINT", d.DewPoint, DbType.Double);
        p.Add("HUMIDITY", d.Humidity, DbType.Double);
        p.Add("PRESSURE", d.Pressure, DbType.Double);
        p.Add("SKY_BRIGHTNESS", d.SkyBrightness, DbType.Double);
        p.Add("RAIN_RATE", d.RainRate, DbType.Double);
        p.Add("SKY_QUALITY", d.SkyQuality, DbType.Double);
        p.Add("SKY_TEMPERATURE", d.SkyTemperature, DbType.Double);
        p.Add("STAR_FWHM", d.StarFwhm, DbType.Double);
        p.Add("TEMPERATURE", d.Temperature, DbType.Double);
        p.Add("WIND_DIRECTION", d.WindDirection, DbType.Double);
        p.Add("WIND_GUST", d.WindGust, DbType.Double);
        p.Add("WIND_SPEED", d.WindSpeed, DbType.Double);
    }

    /// <summary>
    /// Builds the merge agg sql for metric def.
    /// </summary>
    /// <param name="level">Input value for level.</param>
    /// <returns>The resulting string value.</returns>
    private static string BuildMergeAggSql(string level) {
        var sb = new StringBuilder();
        sb.AppendLine($"MERGE INTO METEO_AGG_{level} t");
        sb.AppendLine("USING (SELECT CAST(@CreatedAt AS TIMESTAMP) CREATED_AT FROM RDB$DATABASE) s");
        sb.AppendLine("ON (t.CREATED_AT = s.CREATED_AT)");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET");

        var updates = new List<string>();
        foreach (var m in Metrics) {
            updates.Add($"{m.Name}_SUM = COALESCE(t.{m.Name}_SUM, 0) + COALESCE(@{m.Name}, 0)");
            updates.Add($"{m.Name}_COUNT = COALESCE(t.{m.Name}_COUNT, 0) + CASE WHEN @{m.Name} IS NULL THEN 0 ELSE 1 END");
            updates.Add($"{m.Name}_AVG = CASE WHEN (COALESCE(t.{m.Name}_COUNT, 0) + CASE WHEN @{m.Name} IS NULL THEN 0 ELSE 1 END)=0 THEN NULL ELSE (COALESCE(t.{m.Name}_SUM,0)+COALESCE(@{m.Name},0))/(COALESCE(t.{m.Name}_COUNT, 0) + CASE WHEN @{m.Name} IS NULL THEN 0 ELSE 1 END) END");
            updates.Add($"{m.Name}_MIN = CASE WHEN @{m.Name} IS NULL THEN t.{m.Name}_MIN WHEN t.{m.Name}_MIN IS NULL OR @{m.Name} < t.{m.Name}_MIN THEN @{m.Name} ELSE t.{m.Name}_MIN END");
            updates.Add($"{m.Name}_MAX = CASE WHEN @{m.Name} IS NULL THEN t.{m.Name}_MAX WHEN t.{m.Name}_MAX IS NULL OR @{m.Name} > t.{m.Name}_MAX THEN @{m.Name} ELSE t.{m.Name}_MAX END");
            updates.Add($"{m.Name}_FIRST = COALESCE(t.{m.Name}_FIRST, @{m.Name})");
            updates.Add($"{m.Name}_LAST = CASE WHEN @{m.Name} IS NULL THEN t.{m.Name}_LAST ELSE @{m.Name} END");
        }

        sb.AppendLine(string.Join(",\n", updates));
        sb.AppendLine("WHEN NOT MATCHED THEN INSERT (");

        var cols = new List<string> { "CREATED_AT" };
        foreach (var m in Metrics) {
            cols.AddRange([$"{m.Name}_SUM", $"{m.Name}_COUNT", $"{m.Name}_AVG", $"{m.Name}_MIN", $"{m.Name}_MAX", $"{m.Name}_FIRST", $"{m.Name}_LAST"]);
        }

        sb.AppendLine(string.Join(",", cols));
        sb.AppendLine(") VALUES (");

        var vals = new List<string> { "@CreatedAt" };
        foreach (var m in Metrics) {
            vals.AddRange([
                $"COALESCE(@{m.Name}, 0)",
                $"CASE WHEN @{m.Name} IS NULL THEN 0 ELSE 1 END",
                $"@{m.Name}",
                $"@{m.Name}",
                $"@{m.Name}",
                $"@{m.Name}",
                $"@{m.Name}"
            ]);
        }

        sb.AppendLine(string.Join(",", vals));
        sb.AppendLine(")");
        return sb.ToString();
    }

    /// <summary>
    /// Gets the raw data for metric def.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <returns>The result of the operation.</returns>
    private List<ObservingData> GetRawData(DateTime startTime, DateTime endTime) {
        var bags = new ConcurrentBag<ObservingData>();
        var shards = EnumerateMonths(startTime, endTime).Select(GetRawDbPath).Where(File.Exists).ToList();
        const string sql = @"SELECT CREATED_AT AS ""Timestamp"", CLOUD_COVER AS CloudCover, DEW_POINT AS DewPoint, HUMIDITY AS Humidity, PRESSURE AS Pressure,
RAIN_RATE AS RainRate, SKY_BRIGHTNESS AS SkyBrightness, SKY_QUALITY AS SkyQuality, SKY_TEMPERATURE AS SkyTemperature, STAR_FWHM AS StarFwhm,
TEMPERATURE AS Temperature, WIND_DIRECTION AS WindDirection, WIND_GUST AS WindGust, WIND_SPEED AS WindSpeed, CAST(IS_SAFE AS INTEGER) AS IsSafeInt
FROM METEO_RAW WHERE CREATED_AT >= @startTime AND CREATED_AT <= @endTime ORDER BY CREATED_AT ASC";

        Parallel.ForEach(shards, db => {
            using var conn = new FbConnection(GetConnectionString(db));
            conn.Open();
            foreach (var r in conn.Query<ObservingData>(sql, new { startTime, endTime })) {
                bags.Add(r);
            }
        });

        return [.. bags.OrderBy(x => x.Timestamp)];
    }

    /// <summary>
    /// Executes enumerate raw data batches as part of metric def processing.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <param name="batchSize">Input value for batch size.</param>
    /// <returns>The result of the operation.</returns>
    private IEnumerable<IReadOnlyList<ObservingData>> EnumerateRawDataBatches(DateTime startTime, DateTime endTime, int batchSize) {
        if (endTime < startTime) {
            yield break;
        }

        var boundedBatchSize = Math.Max(1, batchSize);
        var shards = EnumerateMonths(startTime, endTime).Select(GetRawDbPath).Where(File.Exists).ToList();
        const string sql = @"SELECT CREATED_AT AS ""Timestamp"", CLOUD_COVER AS CloudCover, DEW_POINT AS DewPoint, HUMIDITY AS Humidity, PRESSURE AS Pressure,
RAIN_RATE AS RainRate, SKY_BRIGHTNESS AS SkyBrightness, SKY_QUALITY AS SkyQuality, SKY_TEMPERATURE AS SkyTemperature, STAR_FWHM AS StarFwhm,
TEMPERATURE AS Temperature, WIND_DIRECTION AS WindDirection, WIND_GUST AS WindGust, WIND_SPEED AS WindSpeed, CAST(IS_SAFE AS INTEGER) AS IsSafeInt
FROM METEO_RAW WHERE CREATED_AT >= @startTime AND CREATED_AT <= @endTime
ORDER BY CREATED_AT ASC ROWS @rowStart TO @rowEnd";

        foreach (var db in shards) {
            using var conn = new FbConnection(GetConnectionString(db));
            conn.Open();

            var offset = 0;
            while (true) {
                var chunk = conn.Query<ObservingData>(sql, new {
                    startTime,
                    endTime,
                    rowStart = offset + 1,
                    rowEnd = offset + boundedBatchSize
                }).ToList();

                if (chunk.Count == 0) {
                    break;
                }

                yield return chunk;
                offset += chunk.Count;

                if (chunk.Count < boundedBatchSize) {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the aggregated data for metric def.
    /// </summary>
    /// <param name="startTime">Input value for start time.</param>
    /// <param name="endTime">Input value for end time.</param>
    /// <param name="level">Input value for level.</param>
    /// <param name="slotDuration">Input value for slot duration.</param>
    /// <param name="func">Input value for func.</param>
    /// <returns>The result of the operation.</returns>
    private List<ObservingData> GetAggregatedData(DateTime startTime, DateTime endTime, string level, TimeSpan slotDuration, AggregationFunction func) {
        string suffix = func switch {
            AggregationFunction.Average => "AVG",
            AggregationFunction.Minimum => "MIN",
            AggregationFunction.Maximum => "MAX",
            AggregationFunction.First => "FIRST",
            AggregationFunction.Last => "LAST",
            AggregationFunction.Sum => "SUM",
            AggregationFunction.Count => "COUNT",
            _ => "AVG"
        };

        string cloud = suffix == "COUNT" ? "CAST(CLOUD_COVER_COUNT AS DOUBLE PRECISION)" : $"CLOUD_COVER_{suffix}";
        string dew = suffix == "COUNT" ? "CAST(DEW_POINT_COUNT AS DOUBLE PRECISION)" : $"DEW_POINT_{suffix}";
        string humidity = suffix == "COUNT" ? "CAST(HUMIDITY_COUNT AS DOUBLE PRECISION)" : $"HUMIDITY_{suffix}";
        string pressure = suffix == "COUNT" ? "CAST(PRESSURE_COUNT AS DOUBLE PRECISION)" : $"PRESSURE_{suffix}";
        string rain = suffix == "COUNT" ? "CAST(RAIN_RATE_COUNT AS DOUBLE PRECISION)" : $"RAIN_RATE_{suffix}";
        string skyB = suffix == "COUNT" ? "CAST(SKY_BRIGHTNESS_COUNT AS DOUBLE PRECISION)" : $"SKY_BRIGHTNESS_{suffix}";
        string skyQ = suffix == "COUNT" ? "CAST(SKY_QUALITY_COUNT AS DOUBLE PRECISION)" : $"SKY_QUALITY_{suffix}";
        string skyT = suffix == "COUNT" ? "CAST(SKY_TEMPERATURE_COUNT AS DOUBLE PRECISION)" : $"SKY_TEMPERATURE_{suffix}";
        string fwhm = suffix == "COUNT" ? "CAST(STAR_FWHM_COUNT AS DOUBLE PRECISION)" : $"STAR_FWHM_{suffix}";
        string temp = suffix == "COUNT" ? "CAST(TEMPERATURE_COUNT AS DOUBLE PRECISION)" : $"TEMPERATURE_{suffix}";
        string windD = suffix == "COUNT" ? "CAST(WIND_DIRECTION_COUNT AS DOUBLE PRECISION)" : $"WIND_DIRECTION_{suffix}";
        string windG = suffix == "COUNT" ? "CAST(WIND_GUST_COUNT AS DOUBLE PRECISION)" : $"WIND_GUST_{suffix}";
        string windS = suffix == "COUNT" ? "CAST(WIND_SPEED_COUNT AS DOUBLE PRECISION)" : $"WIND_SPEED_{suffix}";
        string safe = suffix == "COUNT" ? "CAST(IS_SAFE_COUNT AS DOUBLE PRECISION)" : $"IS_SAFE_{suffix}";

        var table = $"METEO_AGG_{level}";
        var sql = $@"SELECT CREATED_AT AS ""Timestamp"", {cloud} AS CloudCover, {dew} AS DewPoint, {humidity} AS Humidity, {pressure} AS Pressure,
{rain} AS RainRate, {skyB} AS SkyBrightness, {skyQ} AS SkyQuality, {skyT} AS SkyTemperature, {fwhm} AS StarFwhm,
{temp} AS Temperature, {windD} AS WindDirection, {windG} AS WindGust, {windS} AS WindSpeed,
CAST({safe} AS INTEGER) AS IsSafeInt FROM {table}
WHERE CREATED_AT >= @startTime AND CREATED_AT <= @endTime ORDER BY CREATED_AT";

        var bag = new ConcurrentBag<ObservingData>();

        var shards = level is "10S" or "30S" or "1M" or "5M" or "15M"
            ? [.. EnumerateMonths(startTime, endTime).Select(GetMonthlyAggDbPath).Where(File.Exists)]
            : EnumerateYears(startTime, endTime).Select(GetYearAggDbPath).Where(File.Exists).ToList();

        Parallel.ForEach(shards, db => {
            using var conn = new FbConnection(GetConnectionString(db));
            conn.Open();
            foreach (var r in conn.Query<ObservingData>(sql, new { startTime, endTime })) {
                r.TimestampEnd = r.Timestamp + slotDuration;
                r.RecordCount = 1;
                r.SafePercentage = r.IsSafeInt;
                bag.Add(r);
            }
        });

        return [.. bag.OrderBy(x => x.Timestamp)];
    }

    /// <summary>
    /// Maps the slot duration for metric def.
    /// </summary>
    /// <param name="slotDuration">Input value for slot duration.</param>
    /// <returns>The resulting string value.</returns>
    private static string MapSlotDuration(TimeSpan slotDuration) {
        if (slotDuration <= TimeSpan.FromSeconds(3)) return "RAW";
        if (slotDuration <= TimeSpan.FromSeconds(10)) return "10S";
        if (slotDuration <= TimeSpan.FromSeconds(30)) return "30S";
        if (slotDuration <= TimeSpan.FromMinutes(1)) return "1M";
        if (slotDuration <= TimeSpan.FromMinutes(5)) return "5M";
        if (slotDuration <= TimeSpan.FromMinutes(15)) return "15M";
        if (slotDuration <= TimeSpan.FromHours(1)) return "1H";
        if (slotDuration <= TimeSpan.FromHours(4)) return "4H";
        if (slotDuration <= TimeSpan.FromHours(12)) return "12H";
        if (slotDuration <= TimeSpan.FromDays(1)) return "1D";
        if (slotDuration <= TimeSpan.FromDays(3)) return "3D";
        return "1W";
    }

    /// <summary>
    /// Executes enumerate months as part of metric def processing.
    /// </summary>
    /// <param name="start">Input value for start.</param>
    /// <param name="end">Input value for end.</param>
    /// <returns>The result of the operation.</returns>
    private static IEnumerable<DateTime> EnumerateMonths(DateTime start, DateTime end) {
        for (var d = new DateTime(start.Year, start.Month, 1); d <= end; d = d.AddMonths(1)) {
            yield return d;
        }
    }

    /// <summary>
    /// Executes enumerate years as part of metric def processing.
    /// </summary>
    /// <param name="start">Input value for start.</param>
    /// <param name="end">Input value for end.</param>
    /// <returns>The result of the operation.</returns>
    private static IEnumerable<int> EnumerateYears(DateTime start, DateTime end) {
        for (var y = start.Year; y <= end.Year; y++) {
            yield return y;
        }
    }

    private string GetRawDbPath(DateTime date) => Path.Combine(_root, date.Year.ToString(CultureInfo.InvariantCulture), $"{date:MM}_RAW{DbExt}");
    private string GetMonthlyAggDbPath(DateTime date) => Path.Combine(_root, date.Year.ToString(CultureInfo.InvariantCulture), $"{date:MM}_AGG{DbExt}");
    private string GetYearAggDbPath(int year) => Path.Combine(_root, $"{year}_AGG{DbExt}");

    /// <summary>
    /// Gets the connection string for metric def.
    /// </summary>
    /// <param name="dbPath">Path value for db path.</param>
    /// <returns>The resulting string value.</returns>
    private string GetConnectionString(string dbPath) {
        var csb = new FbConnectionStringBuilder {
            DataSource = "localhost",
            Database = dbPath,
            UserID = _user,
            Password = _password,
            ServerType = FbServerType.Default,
            Charset = "UTF8",
            WireCrypt = FbWireCrypt.Enabled,
            Pooling = true
        };
        return csb.ToString();
    }

    /// <summary>
    /// Ensures the raw database for metric def.
    /// </summary>
    /// <param name="path">Path value for path.</param>
    private void EnsureRawDatabase(string path) {
        if (!_ensuredRawDbs.TryAdd(path, 0)) {
            return;
        }

        EnsureDb(path);

        var schemaKey = $"{path}|METEO_RAW";
        if (!_ensuredRawTables.TryAdd(schemaKey, 0)) {
            return;
        }

        using var conn = new FbConnection(GetConnectionString(path));
        conn.Open();

        if (!TableExists(conn, "METEO_RAW")) {
            var columns = new List<string> { "CREATED_AT TIMESTAMP NOT NULL PRIMARY KEY" };
            foreach (var metric in Metrics) {
                columns.Add($"{metric.Name} {metric.RawSqlType}");
            }

            conn.Execute($"CREATE TABLE METEO_RAW ({string.Join(",", columns)})");
        }

        if (!IndexExists(conn, "IDX_METEO_RAW_CREATED_AT")) {
            conn.Execute("CREATE INDEX IDX_METEO_RAW_CREATED_AT ON METEO_RAW(CREATED_AT)");
        }
    }

    /// <summary>
    /// Ensures the monthly agg database for metric def.
    /// </summary>
    /// <param name="path">Path value for path.</param>
    private void EnsureMonthlyAggDatabase(string path) {
        if (!_ensuredMonthlyAggDbs.TryAdd(path, 0)) {
            return;
        }

        EnsureDb(path);
        using var conn = new FbConnection(GetConnectionString(path));
        conn.Open();
        foreach (var level in Levels.Where(x => x.Monthly && x.Name != "RAW")) {
            EnsureAggTable(conn, path, level.Name);
        }
    }

    /// <summary>
    /// Ensures the year agg database for metric def.
    /// </summary>
    /// <param name="path">Path value for path.</param>
    private void EnsureYearAggDatabase(string path) {
        if (!_ensuredYearAggDbs.TryAdd(path, 0)) {
            return;
        }

        EnsureDb(path);
        using var conn = new FbConnection(GetConnectionString(path));
        conn.Open();
        foreach (var level in Levels.Where(x => !x.Monthly)) {
            EnsureAggTable(conn, path, level.Name);
        }
    }

    /// <summary>
    /// Ensures the db for metric def.
    /// </summary>
    /// <param name="path">Path value for path.</param>
    private void EnsureDb(string path) {
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        if (File.Exists(path)) {
            return;
        }

        var cs = GetConnectionString(path);
        FbConnection.CreateDatabase(cs, 32768, true, false);
    }

    /// <summary>
    /// Ensures the agg table for metric def.
    /// </summary>
    /// <param name="conn">Input value for conn.</param>
    /// <param name="dbPath">Path value for db path.</param>
    /// <param name="level">Input value for level.</param>
    private void EnsureAggTable(FbConnection conn, string dbPath, string level) {
        var tableName = $"METEO_AGG_{level}";
        var indexName = $"IDX_METEO_AGG_{level}_CREATED_AT";
        var schemaKey = $"{dbPath}|{tableName}";

        if (!_ensuredAggTables.TryAdd(schemaKey, 0)) {
            return;
        }

        if (!TableExists(conn, tableName)) {
            var requiredColumns = BuildAggTableRequiredColumns();
            var columns = requiredColumns
                .Select(column => string.Equals(column.Key, "CREATED_AT", StringComparison.OrdinalIgnoreCase)
                    ? "CREATED_AT TIMESTAMP NOT NULL PRIMARY KEY"
                    : $"{column.Key} {column.Value}")
                .ToList();

            conn.Execute($"CREATE TABLE {tableName} ({string.Join(",", columns)})");
        }

        if (!IndexExists(conn, indexName)) {
            conn.Execute($"CREATE INDEX {indexName} ON {tableName}(CREATED_AT)");
        }
    }

    /// <summary>
    /// Determines whether table exists for metric def.
    /// </summary>
    /// <param name="conn">Input value for conn.</param>
    /// <param name="tableName">Input value for table name.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TableExists(FbConnection conn, string tableName) {
        const string sql = @"SELECT 1 FROM RDB$RELATIONS WHERE RDB$RELATION_NAME = @TableName AND COALESCE(RDB$SYSTEM_FLAG, 0) = 0";
        return conn.ExecuteScalar<int?>(sql, new { TableName = tableName.ToUpperInvariant() }).HasValue;
    }

    /// <summary>
    /// Determines whether index exists for metric def.
    /// </summary>
    /// <param name="conn">Input value for conn.</param>
    /// <param name="indexName">Input value for index name.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool IndexExists(FbConnection conn, string indexName) {
        const string sql = @"SELECT 1 FROM RDB$INDICES WHERE RDB$INDEX_NAME = @IndexName";
        return conn.ExecuteScalar<int?>(sql, new { IndexName = indexName.ToUpperInvariant() }).HasValue;
    }

    /// <summary>
    /// Validates the storage structure for metric def.
    /// </summary>
    /// <param name="storageRootPath">Path value for storage root path.</param>
    /// <param name="validateDatabaseSchema">Input value for validate database schema.</param>
    /// <returns>The result of the operation.</returns>
    public static StorageValidationResult ValidateStorageStructure(string? storageRootPath, bool validateDatabaseSchema) {
        var result = new StorageValidationResult();
        if (string.IsNullOrWhiteSpace(storageRootPath)) {
            result.Issues.Add(new(StorageValidationIssueSeverity.Warning, "Data storage path is not configured."));
            return result;
        }

        if (!Directory.Exists(storageRootPath)) {
            result.Issues.Add(new(StorageValidationIssueSeverity.Warning, "Data storage folder does not exist."));
            return result;
        }

        var root = Path.GetFullPath(storageRootPath);
        var dbFiles = Directory.EnumerateFiles(root, "*.fdb", SearchOption.AllDirectories).ToList();

        foreach (var file in dbFiles) {
            var name = Path.GetFileName(file);
            var parent = Path.GetDirectoryName(file) ?? root;
            var isRoot = string.Equals(parent, root, StringComparison.OrdinalIgnoreCase);

            var isYearAgg = System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{4}_AGG\.fdb$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var isMonthlyRaw = System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{2}_RAW\.fdb$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var isMonthlyAgg = System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{2}_AGG\.fdb$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (isRoot) {
                if (!isYearAgg) {
                    result.Issues.Add(new(StorageValidationIssueSeverity.Warning, $"Unexpected database file in storage root: {name}."));
                }
            } else {
                var yearFolderName = new DirectoryInfo(parent).Name;
                var isYearFolder = System.Text.RegularExpressions.Regex.IsMatch(yearFolderName, @"^\d{4}$");
                if (!isYearFolder || (!isMonthlyRaw && !isMonthlyAgg)) {
                    result.Issues.Add(new(StorageValidationIssueSeverity.Warning, $"Unexpected database file layout: {Path.GetRelativePath(root, file)}."));
                }
            }
        }

        if (!validateDatabaseSchema || dbFiles.Count == 0) {
            return result;
        }

        foreach (var file in dbFiles) {
            try {
                using var conn = new FbConnection(new FbConnectionStringBuilder {
                    DataSource = "localhost",
                    Database = file,
                    UserID = "SYSDBA",
                    Password = "masterkey",
                    ServerType = FbServerType.Default,
                    Charset = "UTF8",
                    WireCrypt = FbWireCrypt.Enabled,
                    Pooling = false
                }.ToString());
                conn.Open();

                var expectedSchemas = GetRequiredTableSchemasForFile(file, root);
                if (expectedSchemas.Count == 0) {
                    continue;
                }

                foreach (var schema in expectedSchemas) {
                    if (!TableExists(conn, schema.TableName)) {
                        result.Issues.Add(new(StorageValidationIssueSeverity.Error, $"Missing table {schema.TableName} in {Path.GetFileName(file)}."));
                        continue;
                    }

                    var actualColumns = GetTableColumns(conn, schema.TableName);
                    foreach (var requiredColumn in schema.RequiredColumns.Keys) {
                        if (!actualColumns.ContainsKey(requiredColumn)) {
                            result.Issues.Add(new(StorageValidationIssueSeverity.Error, $"Missing column {requiredColumn} in table {schema.TableName} ({Path.GetFileName(file)})."));
                            continue;
                        }

                        var expectedType = schema.RequiredColumns[requiredColumn];
                        var actualType = actualColumns[requiredColumn];
                        if (!string.Equals(actualType, expectedType, StringComparison.OrdinalIgnoreCase)) {
                            result.Issues.Add(new(StorageValidationIssueSeverity.Error,
                                $"Invalid type for column {requiredColumn} in table {schema.TableName} ({Path.GetFileName(file)}). Expected {expectedType}, got {actualType}."));
                        }
                    }

                    if (!IndexExists(conn, schema.IndexName)) {
                        result.Issues.Add(new(StorageValidationIssueSeverity.Error, $"Missing index {schema.IndexName} on table {schema.TableName} ({Path.GetFileName(file)})."));
                    }
                }
            } catch (Exception ex) {
                result.Issues.Add(new(StorageValidationIssueSeverity.Error, $"Cannot open database {Path.GetFileName(file)}: {ex.Message}"));
            }
        }

        return result;
    }

    /// <summary>
    /// Represents table schema expectation and encapsulates its related behavior and state.
    /// </summary>
    private sealed record TableSchemaExpectation(string TableName, string IndexName, Dictionary<string, string> RequiredColumns);

    /// <summary>
    /// Gets the required table schemas for file for table schema expectation.
    /// </summary>
    /// <param name="filePath">Path value for file path.</param>
    /// <param name="storageRootPath">Path value for storage root path.</param>
    /// <returns>The result of the operation.</returns>
    private static List<TableSchemaExpectation> GetRequiredTableSchemasForFile(string filePath, string storageRootPath) {
        var fileName = Path.GetFileName(filePath);
        if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^\d{2}_RAW\.fdb$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
            return [
                new TableSchemaExpectation(
                    "METEO_RAW",
                    "IDX_METEO_RAW_CREATED_AT",
                    BuildRawTableRequiredColumns())
            ];
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^\d{2}_AGG\.fdb$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
            return BuildAggTableSchemas(["10S", "30S", "1M", "5M", "15M"]);
        }

        var isRoot = string.Equals(Path.GetDirectoryName(filePath), Path.GetFullPath(storageRootPath), StringComparison.OrdinalIgnoreCase);
        if (isRoot && System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^\d{4}_AGG\.fdb$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
            return BuildAggTableSchemas(["1H", "4H", "12H", "1D", "3D", "1W"]);
        }

        return [];
    }

    /// <summary>
    /// Builds the agg table schemas for table schema expectation.
    /// </summary>
    /// <param name="levels">Collection of levels items used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    private static List<TableSchemaExpectation> BuildAggTableSchemas(IEnumerable<string> levels) {
        var requiredColumns = BuildAggTableRequiredColumns();

        return [.. levels.Select(level => new TableSchemaExpectation(
            $"METEO_AGG_{level}",
            $"IDX_METEO_AGG_{level}_CREATED_AT",
            new Dictionary<string, string>(requiredColumns, StringComparer.OrdinalIgnoreCase)))];
    }

    /// <summary>
    /// Builds the agg table required columns for table schema expectation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dictionary<string, string> BuildAggTableRequiredColumns() {
        var requiredColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["CREATED_AT"] = "TIMESTAMP"
        };

        foreach (var metric in Metrics) {
            requiredColumns[$"{metric.Name}_SUM"] = "DOUBLE PRECISION";
            requiredColumns[$"{metric.Name}_COUNT"] = "INTEGER";
            requiredColumns[$"{metric.Name}_AVG"] = metric.AggSqlType;
            requiredColumns[$"{metric.Name}_MIN"] = metric.AggSqlType;
            requiredColumns[$"{metric.Name}_MAX"] = metric.AggSqlType;
            requiredColumns[$"{metric.Name}_FIRST"] = metric.AggSqlType;
            requiredColumns[$"{metric.Name}_LAST"] = metric.AggSqlType;
        }

        return requiredColumns;
    }

    /// <summary>
    /// Builds the raw table required columns for table schema expectation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static Dictionary<string, string> BuildRawTableRequiredColumns() {
        var requiredColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["CREATED_AT"] = "TIMESTAMP"
        };

        foreach (var metric in Metrics) {
            requiredColumns[metric.Name] = metric.RawSqlType;
        }

        return requiredColumns;
    }

    /// <summary>
    /// Gets the table columns for table schema expectation.
    /// </summary>
    /// <param name="conn">Input value for conn.</param>
    /// <param name="tableName">Input value for table name.</param>
    /// <returns>The result of the operation.</returns>
    private static Dictionary<string, string> GetTableColumns(FbConnection conn, string tableName) {
        const string sql = @"
SELECT
    TRIM(rf.RDB$FIELD_NAME) AS COLUMN_NAME,
    CASE f.RDB$FIELD_TYPE
        WHEN 7 THEN CASE f.RDB$FIELD_SUB_TYPE WHEN 1 THEN 'NUMERIC' WHEN 2 THEN 'DECIMAL' ELSE 'SMALLINT' END
        WHEN 8 THEN CASE f.RDB$FIELD_SUB_TYPE WHEN 1 THEN 'NUMERIC' WHEN 2 THEN 'DECIMAL' ELSE 'INTEGER' END
        WHEN 10 THEN 'FLOAT'
        WHEN 12 THEN 'DATE'
        WHEN 13 THEN 'TIME'
        WHEN 14 THEN 'CHAR'
        WHEN 16 THEN CASE f.RDB$FIELD_SUB_TYPE WHEN 1 THEN 'NUMERIC' WHEN 2 THEN 'DECIMAL' ELSE 'BIGINT' END
        WHEN 23 THEN 'BOOLEAN'
        WHEN 27 THEN 'DOUBLE PRECISION'
        WHEN 35 THEN 'TIMESTAMP'
        WHEN 37 THEN 'VARCHAR'
        WHEN 261 THEN 'BLOB'
        ELSE 'UNKNOWN'
    END AS COLUMN_TYPE
FROM RDB$RELATION_FIELDS rf
JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
WHERE rf.RDB$RELATION_NAME = @TableName";

        var columns = conn.Query<(string? Name, string? Type)>(sql, new { TableName = tableName.ToUpperInvariant() });
        return columns
            .Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Type))
            .ToDictionary(
                x => x.Name!.Trim().ToUpperInvariant(),
                x => x.Type!.Trim().ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase);
    }
}
