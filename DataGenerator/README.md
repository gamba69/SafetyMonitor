# DataGenerator — Detailed Guide

## 1. Purpose

`DataGenerator` creates synthetic weather/safety telemetry and stores it in `DataStorage`.

Typical use cases:

- bootstrap empty environments for demos,
- stress-test charts and aggregation layers,
- create deterministic datasets using `--seed`.

---

## 2. Run

```bash
dotnet run --project DataGenerator -- <options>
```

Version:

```bash
dotnet run --project DataGenerator -- --version
```

---

## 3. Command-line Options (full reference)

- `--storage-path <path>` (**required**)
  - `DataStorage` root directory.

- `--start <timestamp>`
  - Start time of generated range.
  - Recommended format: ISO (`2024-01-15T00:00:00`).
  - Default: `UtcNow - 1 day`.

- `--end <timestamp>`
  - End time of generated range.
  - If omitted and `--count` is not set, defaults to `UtcNow`.

- `--interval <seconds>` (default: `60`)
  - Time step between generated samples.
  - Must be positive.

- `--count <int>`
  - Number of generated records.
  - If set (`>0`), it overrides `--end`.

- `--seed <int>`
  - Optional deterministic RNG seed.

- `--batch-size <int>` (default: `1000`)
  - Batch insert size for database writes.
  - Must be positive.

- `--clean`
  - Deletes existing raw data in target range before generation.

- `--db-user <user>` (default: `SYSDBA`)
- `--db-password <password>` (default: `masterkey`)

---

## 4. Execution Flow

1. Parse and validate options.
2. Resolve final time interval (using `start`, `end`, `count`, `interval`).
3. Optionally clean existing raw data (`--clean`).
4. Generate samples at each interval step.
5. Insert raw data in batches (`AddRawDataBatch`).
6. Recalculate aggregates for the same range.
7. Print progress and estimated remaining time for both phases.

---

## 5. Generated fields

Generated telemetry includes:

- CloudCover
- DewPoint
- Humidity
- Pressure
- RainRate
- SkyBrightness
- SkyQuality
- SkyTemperature
- StarFWHM
- Temperature
- WindDirection
- WindGust
- WindSpeed
- IsSafe

Generation patterns emulate realistic variability and weather episodes (including rain/humidity phases).

---

## 6. Examples

## 6.1 Generate one day at 1-minute step

```bash
dotnet run --project DataGenerator -- \
  --storage-path "D:\MeteoStorage" \
  --start 2026-03-01T00:00:00 \
  --end 2026-03-02T00:00:00 \
  --interval 60
```

## 6.2 Generate 100,000 deterministic samples

```bash
dotnet run --project DataGenerator -- \
  --storage-path "D:\MeteoStorage" \
  --start 2026-03-01T00:00:00 \
  --count 100000 --interval 10 --seed 42
```

## 6.3 Replace a historical range

```bash
dotnet run --project DataGenerator -- \
  --storage-path "D:\MeteoStorage" \
  --start 2026-03-01T00:00:00 \
  --end 2026-03-03T00:00:00 --clean
```

---

## 7. Exit Codes

- `0` — success.
- `1` — invalid options/time values or storage/runtime failure.

---

## 8. Related projects

- `../DataStorage/README.md`
- `../SafetyMonitor/README.md`
- `../DataAggregator/README.md`
