# DataStorage — Detailed Guide

## 1. Purpose

`DataStorage` is the Firebird-backed persistence/query library responsible for:

- writing raw telemetry,
- maintaining incremental aggregates,
- querying raw and aggregated time series,
- deleting raw data ranges,
- full-range aggregate recalculation,
- storage structure/schema validation.

---

## 2. Storage Layout Model

Storage uses `.fdb` shard files:

- monthly raw shard:
  - `<root>/<year>/<MM>_RAW.fdb`
- monthly aggregate shard (short intervals):
  - `<root>/<year>/<MM>_AGG.fdb` for `10S,30S,1M,5M,15M`
- yearly aggregate shard (long intervals):
  - `<root>/<year>_AGG.fdb` for `1H,4H,12H,1D,3D,1W`

Example:

- `D:\MeteoStorage\2026\03_RAW.fdb`
- `D:\MeteoStorage\2026\03_AGG.fdb`
- `D:\MeteoStorage\2026_AGG.fdb`

---

## 3. Core Data Model (`ObservingData`)

Main fields:

- `Timestamp`, optional `TimestampEnd`
- weather metrics (`CloudCover`, `Temperature`, `Humidity`, etc.)
- `IsSafeInt` with bool wrapper `IsSafe`
- aggregation metadata (`RecordCount`, `SafePercentage`)

---

## 4. Public API

## 4.1 Constructor

```csharp
new DataStorage(storageRootPath, userName, password)
```

Creates the storage root folder if needed and configures DB access.

## 4.2 Write API

- `AddData(ObservingData data)`
  - write one sample
- `AddRawDataBatch(IReadOnlyCollection<ObservingData> batch)`
  - batch write to raw layer only
- `AddDataBatch(IReadOnlyCollection<ObservingData> batch)`
  - batch write raw + incremental aggregate updates

## 4.3 Read API

- `GetData(start, end, slotDuration = null, aggregationFunction = Average)`
  - `slotDuration == null`: raw data
  - otherwise: mapped aggregate level
- `GetLatestData(endTime, maxLookback)`
  - latest raw sample in lookback window

## 4.4 Maintenance API

- `DeleteData(start, end)`
  - delete raw rows in range
- `RecalculateAggregations(start, end, progress?, upsertBatchSize = 1000)`
  - rebuild aggregates from raw data
- `ValidateStorageStructure(storagePath, validateDatabaseSchema)`
  - validate file layout and optionally schema/indexes

---

## 5. Aggregation Levels

Built-in levels:

- `RAW`
- `10S`, `30S`, `1M`, `5M`, `15M`
- `1H`, `4H`, `12H`, `1D`, `3D`, `1W`

`slotDuration` is mapped to the nearest supported level.

---

## 6. Aggregate Fields and Query Functions

Each metric stores per-bucket:

- `SUM`
- `COUNT`
- `AVG`
- `MIN`
- `MAX`
- `FIRST`
- `LAST`

Supported query functions:

- `Average`, `Minimum`, `Maximum`, `First`, `Last`, `Sum`, `Count`

---

## 7. Storage Validation

`ValidateStorageStructure` returns a `StorageValidationResult` with:

- `Issues` (`Warning` or `Error`)
- `HasWarnings`
- `HasErrors`

Validation checks include:

1. file/folder naming and placement (`*_RAW.fdb`, monthly/yearly `*_AGG.fdb`),
2. optional schema verification:
   - required tables,
   - required columns + expected types,
   - required `..._CREATED_AT` indexes.

This validation is used by UI startup flows to detect misconfigured storage early.

---

## 8. Performance and resilience notes

- parallel shard reads (`Parallel.ForEach`),
- cached SQL generation for merge/upsert commands,
- batch insert and batch upsert patterns,
- on-demand DB/table/index creation.

---

## 9. Operational notes

- Firebird wire encryption is enabled in connection strings.
- Schema drift in DB files is surfaced by validator errors.
- For large historical rebuilds, prefer the `DataAggregator` CLI.

---

## 10. Related projects

- `../DataCollector/README.md`
- `../DataGenerator/README.md`
- `../DataAggregator/README.md`
- `../SafetyMonitor/README.md`
