# DataCollector — Detailed Guide

## 1. Purpose

`DataCollector` is a CLI application that reads telemetry from ASCOM Alpaca devices:

- `ObservingConditions`
- `SafetyMonitor`

and writes samples to `DataStorage`.

It supports:

- single-run collection,
- resilient continuous mode with retries,
- discovery by device name,
- direct connection by `IP:port`,
- graceful shutdown on `Ctrl+C`.

---

## 2. Run

```bash
dotnet run --project DataCollector -- <options>
```

Version:

```bash
dotnet run --project DataCollector -- --version
```

---

## 3. Command-line Options (full reference)

## 3.1 ObservingConditions device

- `--oc-name <name>`
  - Device name for Alpaca discovery.
  - Use this **instead of** address+port.
- `--oc-address <ip-or-host>`
  - Direct host/IP connection.
- `--oc-port <port>`
  - Direct port connection.
- `--oc-device-number <int>` (default: `0`)
  - Alpaca remote device number.

## 3.2 SafetyMonitor device

- `--sm-name <name>`
  - Device name for Alpaca discovery.
  - Use this **instead of** address+port.
- `--sm-address <ip-or-host>`
  - Direct host/IP connection.
- `--sm-port <port>`
  - Direct port connection.
- `--sm-device-number <int>` (default: `0`)
  - Alpaca remote device number.

## 3.3 Retry and resilience

- `--discovery-retries <int>` (default: `3`)
  - Discovery attempts per device.
- `--data-retries <int>` (default: `3`)
  - Data read / connect retry attempts.
- `--retry-delay <ms>` (default: `1000`)
  - Delay between retry attempts.
- `--error-retry-delay <sec>` (default: `30`)
  - Delay after fatal loop error in continuous mode.

## 3.4 Collection mode

- `--continuous`
  - Run forever until interrupted.
- `--interval <sec>` (default: `3`)
  - Delay between collection cycles in continuous mode.

## 3.5 Output and storage

- `--quiet`
  - Suppress data printing to console.
- `--storage-path <path>`
  - `DataStorage` root path.
  - If omitted, data is not persisted to DB files.
- `--db-user <user>` (default: `SYSDBA`)
- `--db-password <password>` (default: `masterkey`)

---

## 4. Validation Rules

- Each device must be specified by **either** name **or** address+port.
- Name and address+port cannot be used simultaneously for the same device.
- `discovery-retries >= 1`
- `data-retries >= 1`
- `retry-delay >= 0`
- `interval >= 1`

Invalid options return exit code `1` with detailed error output.

---

## 5. Runtime Modes

## 5.1 Single-run mode (default)

Flow:

1. Connect to both devices.
2. Collect one sample set.
3. Print to console (unless `--quiet`).
4. Persist to storage (if `--storage-path` is set).
5. Disconnect devices.

## 5.2 Continuous mode

Flow:

1. Repeating collection loop every `--interval` seconds.
2. On connection/data errors:
   - reconnect attempts,
   - wait `--error-retry-delay`,
   - continue operation (process stays alive).
3. `Ctrl+C` triggers graceful cancellation and disconnect.

---

## 6. Collected Metrics

From `ObservingConditions`:

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

From `SafetyMonitor`:

- IsSafe

If a property is not implemented by the hardware driver, `null` is stored and collection continues.

---

## 7. Examples

## 7.1 Discovery + continuous mode

```bash
dotnet run --project DataCollector -- \
  --oc-name "ObservingConditions" \
  --sm-name "SafetyMonitor" \
  --continuous --interval 3 \
  --storage-path "D:\MeteoStorage"
```

## 7.2 Direct IP/port connection

```bash
dotnet run --project DataCollector -- \
  --oc-address 192.168.1.20 --oc-port 11111 \
  --sm-address 192.168.1.21 --sm-port 11111 \
  --storage-path "D:\MeteoStorage"
```

## 7.3 Quiet single run

```bash
dotnet run --project DataCollector -- \
  --oc-name "Obs" --sm-name "Safe" --quiet \
  --storage-path "D:\MeteoStorage"
```

---

## 8. Exit Codes

- `0` — success / user cancellation (`Ctrl+C`).
- `1` — options error, storage init error, connection error, or runtime failure.

---

## 9. Related projects

- `../DataStorage/README.md`
- `../SafetyMonitor/README.md`
