# SafetyMonitor (WinForms) — Detailed Guide

## 1. What this application is

`SafetyMonitor` is a desktop dashboard application for weather and safety telemetry visualization, powered by historical and aggregated data read from `DataStorage`.

Core capabilities:

- configurable multi-dashboard workspace,
- Value Tiles for latest telemetry values,
- Chart Tiles with rich interaction,
- chart link modes (full/grouped/disabled),
- editors for axis rules, display settings, period presets, color schemes, and value schemes,
- chart table export,
- tray integration and startup/minimize behavior.

---

## 2. Screenshots

![Overview dashboard](../images/overview-dashboard.png)
![Dashboard](../images/dashboard.png)
![Dark theme](../images/nught-dashboard.png)
![Light theme](../images/light-theme.png)
![Settings](../images/settings.png)
![Chart tile](../images/chart-tile.png)
![Value tile](../images/value-tile.png)

---

## 3. Run

```bash
dotnet run --project SafetyMonitor
```

### Startup behavior

- **Single-instance**: launching a second instance is blocked.
- Splash screen is shown at startup.
- Storage path/structure can be validated before main UI opens.
- If storage is not configured or has issues, users are guided to Settings.

---

## 4. UI Building Blocks

## 4.1 Dashboard

A dashboard is a grid of tiles. Each tile has:

- position (`row`, `column`),
- size (`rowspan`, `colspan`),
- type (`ValueTile` or `ChartTile`),
- per-tile visual/data configuration.

Supported actions:

- create,
- duplicate,
- delete,
- choose active dashboard,
- favorite dashboards for quick access,
- edit current dashboard from menu/context actions.

## 4.2 Value Tile

Displays the most recent available value for a selected metric, including:

- numeric value,
- unit,
- optional transformed text from Value Scheme,
- color mapping from Color Scheme,
- optional metric icon.

Additional behavior:

- dynamic font fitting to avoid clipping,
- display mode variations depending on tile configuration,
- lookback-window fallback if no immediate sample is available.

## 4.3 Chart Tile

Interactive ScottPlot-based charting module with advanced behavior:

- multiple metric series and aggregation functions in one chart,
- multiple Y axes (separate metric-to-axis mapping),
- per-metric axis rules (range/log/invert),
- adaptive legend placement to reduce overlap,
- hover inspector with nearest-point details,
- auto mode and static mode for time range control,
- automatic switch to static mode on user interaction,
- auto-return from static mode after timeout,
- static pause toggle,
- period preset selector,
- cross-chart synchronization via link groups.

---

## 5. Non-obvious Features You Should Know

## 5.1 Context menus (important)

Context menus are available in several places:

- tile surface,
- chart plot area,
- quick dashboard favorites,
- dashboard panel.

These menus expose many high-value actions that are easy to miss if you only use top-level buttons:

- edit specific tile,
- edit current dashboard,
- switch link mode,
- add/remove favorites,
- open global editors (axis rules, metric settings, period presets, color/value schemes).

## 5.2 Chart mouse interaction

Charts support direct mouse interaction (ScottPlot behavior):

- hover -> inspector data preview,
- pan/zoom/scroll -> enters static mode,
- visible range updates -> aggregation interval adapts to target point density,
- hover anchor and inspector can synchronize across linked charts.

## 5.3 Linked charts (Link Mode)

Modes:

- **Full**: full synchronization across charts,
- **Grouped**: synchronization inside link groups only,
- **Disabled**: charts behave independently.

In grouped mode, each chart tile has a link group, and dashboard-level defaults define period presets per group.

## 5.4 Startup/switch overlay (Visor)

A temporary overlay form (“visor”) is used during startup and dashboard transitions to hide rendering artifacts:

- smoother visual transitions,
- fewer visible intermediate layout states,
- cleaner first impression on high-DPI/theme changes.

---

## 6. Settings (Detailed)

Main settings:

- `StoragePath` — root folder for `DataStorage`
- `RefreshInterval` — dashboard auto-refresh interval
- `ValueTileLookbackMinutes` — latest-sample lookup window for Value Tiles
- `ChartStaticModeTimeoutSeconds` — auto-return timeout from static mode
- `ChartStaticAggregationPresetMatchTolerancePercent` — preset matching tolerance
- `ChartStaticAggregationTargetPointCount` — target point density in static mode
- `ChartRawDataPointIntervalSeconds` — baseline raw point spacing preference
- `ShowRefreshIndicator` — visual refresh countdown/indicator
- `MinimizeToTray` — minimize-to-tray behavior
- `StartMinimized` — start minimized
- `ValidateDatabaseStructureOnStartup` — schema/layout checks on startup
- `MaterialColorScheme`, `IsDarkTheme` — visual theming

Maintenance actions are available (import/export/reset settings).

---

## 7. Performance Design

Implemented optimizations include:

- Value Tile snapshot mode (single latest-data query per refresh cycle),
- Chart snapshot/cache (shared results across chart tiles for same query keys),
- background prefetch before synchronous UI draw,
- controlled re-rendering during theme/application transitions.

---

## 8. Tray Behavior

When `MinimizeToTray = true`:

- minimizing hides window from taskbar,
- refresh timers can continue in tray mode,
- restore is available from tray icon/context menu.

---

## 9. Platform and limitations

- UI is Windows-oriented (WinForms + MaterialSkin + ScottPlot WinForms).
- Without a valid storage path, tiles/charts cannot display telemetry.

---

## 10. Related documents

- `../README.md` — overall solution overview
- `../DataStorage/README.md` — storage model and API
- `../DataCollector/README.md` — real device data collection
