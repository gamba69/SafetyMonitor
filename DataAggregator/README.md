# DataAggregator — Detailed Guide

## 1. Purpose

`DataAggregator` rebuilds aggregated buckets (`10S ... 1W`) from raw `METEO_RAW` data for a selected time range.

Use cases:

- repair aggregates after bulk imports/updates,
- rebuild long historical windows,
- improve chart query performance by ensuring aggregate completeness.

---

## 2. Run

```bash
dotnet run --project DataAggregator -- <options>
```

Version:

```bash
dotnet run --project DataAggregator -- --version
```

---

## 3. Command-line Options (full reference)

- `--storage-path <path>` (**required**)
  - `DataStorage` root folder.

- `--start <timestamp>`
  - Recalculation start time.
  - Default: `UtcNow - 1 day`.

- `--end <timestamp>`
  - Recalculation end time.
  - Default: `UtcNow`.

- `--batch-size <int>` (default: `1000`)
  - Aggregation upsert batch size.

- `--db-user <user>` (default: `SYSDBA`)
- `--db-password <password>` (default: `masterkey`)

---

## 4. Validation and behavior

- If `end < start`, execution fails with error.
- Timestamp parsing uses `DateTime.TryParse`; ISO format is recommended.
- Progress output includes:
  - processed bucket count,
  - percentage,
  - elapsed time,
  - estimated remaining time.

---

## 5. Recalculated Levels

- Monthly aggregate shards: `10S`, `30S`, `1M`, `5M`, `15M`
- Year aggregate shards: `1H`, `4H`, `12H`, `1D`, `3D`, `1W`

For every metric and bucket, fields are recomputed:

- `SUM`, `COUNT`, `AVG`, `MIN`, `MAX`, `FIRST`, `LAST`

---

## 6. Examples

## 6.1 Recalculate recent day

```bash
dotnet run --project DataAggregator -- --storage-path "D:\MeteoStorage"
```

## 6.2 Recalculate specific historical period

```bash
dotnet run --project DataAggregator -- \
  --storage-path "D:\MeteoStorage" \
  --start 2026-01-01T00:00:00 --end 2026-03-01T00:00:00
```

## 6.3 Large rebuild with bigger batches

```bash
dotnet run --project DataAggregator -- \
  --storage-path "D:\MeteoStorage" \
  --start 2024-01-01T00:00:00 --end 2026-01-01T00:00:00 \
  --batch-size 10000
```

---

## 7. Exit Codes

- `0` — success.
- `1` — invalid arguments/time range.

---

## 8. Related projects

- `../DataStorage/README.md`
- `../DataGenerator/README.md`
- `../SafetyMonitor/README.md`
