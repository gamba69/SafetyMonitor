# SafetyMonitorView - Safety Monitor Dashboard

**Complete implementation with all features!**

## ✅ ALL 4 COMPONENTS FULLY IMPLEMENTED

### 1. ValueTile with Real Data Display ✅
- **Color Schemes:** Gradient and discrete modes
- **Real-time Data:** Connected to DataService
- **Icons:** Emoji-based (ready for MaterialDesign)
- **Auto Color Adjustment:** Text color based on background brightness
- **Theme Support:** Light/Dark mode

### 2. ChartTile with ScottPlot ✅
- **Multiple Metrics:** Display any combination of metrics
- **Multiple Aggregations:** Min/Avg/Max of same metric
- **Custom Aggregation Interval:** Manual specification (seconds/minutes/hours)
- **Custom End Time:** View historical data (not just "now")
- **Per-Series Styling:** Color, line width, markers
- **Interactive Period Selector:** 15min to 30 days

### 3. Tile Editors ✅
- **ValueTileEditorForm:** Edit metric, color scheme, decimals, icon, size
- **ChartTileEditorForm:** Edit metrics/aggregations with DataGridView, custom intervals, custom end time, styling
- **Full Integration:** Opens from EditableTileControl

### 4. SettingsForm ✅
- **Storage Path Configuration:** Browse and test connection
- **Refresh Interval:** 1-60 seconds
- **Validation:** Test DataStorage connection before saving

## 🎯 Key Features

### Drag-and-Drop Editor
```
Dashboard Editor:
- Drag tiles to reposition
- Visual grid with dotted lines
- Collision detection
- Quick Access checkbox (max 5)
```

### Quick Access Controls
```
Main Screen Top Bar:
Theme: (•) ☀️ Light  ( ) 🌙 Dark
Quick Access: (•) Main  ( ) Analysis  ( ) Safety
```

### Advanced Chart Configuration
```csharp
// Example: Temperature Min/Avg/Max on one chart
MetricAggregations = new()
{
    new() { 
        Metric = Temperature, 
        Function = Minimum, 
        Label = "Temp Min", 
        Color = Blue, 
        LineWidth = 1.5f 
    },
    new() { 
        Metric = Temperature, 
        Function = Average, 
        Label = "Temp Avg", 
        Color = Green, 
        LineWidth = 2.5f 
    },
    new() { 
        Metric = Temperature, 
        Function = Maximum, 
        Label = "Temp Max", 
        Color = Red, 
        LineWidth = 1.5f,
        ShowMarkers = true 
    }
}
```

### Color Schemes
```
Temperature (Gradient):
-20°C → Dark Blue
  0°C → Blue
 10°C → Cyan
 20°C → Green
 30°C → Yellow
 40°C → Red

Humidity (Discrete):
0-30%   → Orange (Dry)
30-60%  → Green (Comfortable)
60-80%  → Light Blue (Humid)
80-100% → Blue (Very humid)
```

## 🚀 Quick Start

### 1. Prerequisites
- .NET 8.0 SDK
- Windows 10/11
- DataStorage project (adjacent folder)

### 2. Build
```bash
cd SafetyMonitorView
dotnet restore
dotnet build
dotnet run
```

### 3. Initial Setup
1. **File → Settings**
2. Browse to DataStorage folder
3. Click "Test Connection"
4. Set refresh interval (default: 5 seconds)
5. Click "Save"

### 4. Create Dashboard
1. **Dashboards → New Dashboard**
2. **Dashboards → Edit Current**
3. Add tiles using "Add Value Tile" or "Add Chart Tile"
4. Drag tiles to reposition
5. Click "Edit" on tile to configure
6. Click "Save"

## 📊 Examples

### Example 1: Multi-Metric Dashboard
```
Row 0: Temperature | Humidity | Pressure | Wind Speed
Row 1-2: Temperature & Humidity Chart (2×4 size)
```

### Example 2: Temperature Analysis
```
Chart Tile Configuration:
- Metric Aggregations:
  1. Temperature / Minimum / "Min" / Blue / 1.5px
  2. Temperature / Average / "Avg" / Green / 2.5px
  3. Temperature / Maximum / "Max" / Red / 1.5px
- Aggregation Interval: 10 minutes
- Period: Last 24 Hours
- Show Legend: Yes
- Show Grid: Yes
```

### Example 3: Historical Data
```
Chart Configuration:
- Period: 7 Days
- Custom End Time: 2026-01-28 23:59
- Custom Aggregation: 30 minutes
- View data from a week ago!
```

## 📁 Project Structure

```
SafetyMonitorView/
├── SafetyMonitorView.csproj    ✅ Complete
├── Program.cs                   ✅ Entry point
├── Models/
│   ├── MetricType.cs           ✅ 14 metrics (English)
│   ├── ColorScheme.cs          ✅ Gradient interpolation
│   ├── MetricAggregation.cs    ✅ Metric + Function + Style
│   ├── TileConfig.cs           ✅ Value & Chart configs
│   ├── Dashboard.cs            ✅ With IsQuickAccess
│   └── AppSettings.cs          ✅ Application settings
├── Services/
│   ├── DataService.cs          ✅ With aggregation support
│   ├── DashboardService.cs     ✅ Dashboard management
│   ├── ColorSchemeService.cs   ✅ Scheme management
│   └── AppSettingsService.cs   ✅ Settings persistence
├── Forms/
│   ├── MainForm.cs             ✅ With quick access & settings
│   ├── DashboardEditorForm.cs  ✅ Drag-and-drop WORKING
│   ├── EditableTileControl.cs  ✅ Drag-and-drop WORKING
│   ├── SettingsForm.cs         ✅ Storage + refresh config
│   ├── ValueTileEditorForm.cs  ✅ Value tile editor
│   └── ChartTileEditorForm.cs  ✅ Advanced chart editor
└── Controls/
    ├── DashboardPanel.cs       ✅ TableLayout with tiles
    ├── ValueTile.cs            ✅ With color schemes
    └── ChartTile.cs            ✅ With ScottPlot
```

## 🎨 Features Checklist

### ✅ Implemented
- [x] Drag-and-drop tile editor (WORKING!)
- [x] Quick theme switcher (Light/Dark)
- [x] Quick dashboard access (max 5)
- [x] English interface (ALL strings)
- [x] Real-time data display (ValueTile)
- [x] ScottPlot charts (ChartTile)
- [x] Multiple aggregations per metric
- [x] Custom aggregation intervals
- [x] Custom end time (historical data)
- [x] Per-series styling (color, width, markers)
- [x] Color schemes (gradient & discrete)
- [x] Color scheme service with presets
- [x] Value tile editor
- [x] Chart tile editor with DataGridView
- [x] Settings form with storage path
- [x] Dashboard management (create/edit/delete)
- [x] IsQuickAccess flag
- [x] Auto-refresh (configurable)
- [x] **Settings persistence (NEW!)**

## 💾 Settings Persistence

**Application now remembers your preferences!**

### What is Saved

✅ **Window Settings:**
- Window size (width × height)
- Window position (X, Y coordinates)
- Maximized state

✅ **Theme Settings:**
- Light or Dark theme preference

✅ **Dashboard Settings:**
- Last opened dashboard

✅ **Data Settings:**
- Storage path
- Refresh interval

### Storage Location

Settings are saved to:
```
Windows: %APPDATA%\SafetyMonitorView\settings.json
```

Example:
```
C:\Users\YourName\AppData\Roaming\SafetyMonitorView\settings.json
```

### How It Works

1. **On Startup:**
   - Loads settings.json
   - Applies window size/position
   - Applies maximized state
   - Applies theme (Light/Dark)
   - Opens last dashboard

2. **During Usage:**
   - Window resize → saves size
   - Window move → saves position
   - Maximize/restore → saves state
   - Theme change → saves theme
   - Dashboard switch → saves last dashboard

3. **On Exit:**
   - Saves final window state
   - All settings preserved for next session

### Settings File Structure

```json
{
  "WindowWidth": 1400,
  "WindowHeight": 900,
  "WindowX": 100,
  "WindowY": 100,
  "IsMaximized": false,
  "IsDarkTheme": false,
  "LastDashboardId": "guid-here",
  "StoragePath": "C:\\AlpacaData",
  "RefreshInterval": 5
}
```

### Reset Settings

To reset all settings to defaults:
1. Close SafetyMonitorView
2. Delete: `%APPDATA%\SafetyMonitorView\settings.json`
3. Restart application

Settings will be recreated with defaults.

## 🔧 Configuration

### Chart Tile
```
Title: "Temperature Analysis"
Metrics and Aggregations:
  - Temperature | Minimum   | "Min" | Blue   | 1.5 | No
  - Temperature | Average   | "Avg" | Green  | 2.5 | No
  - Temperature | Maximum   | "Max" | Red    | 1.5 | Yes
Aggregation Interval: 10 Minutes
Custom End Time: [ ] Use Now  or  [2026-01-28 23:59]
Show Legend: [✓]
Show Grid: [✓]
Size: 2 rows × 4 columns
```

### Value Tile
```
Title: "Temperature"
Metric: Temperature
Color Scheme: Temperature (gradient)
Decimal Places: 1
Show Icon: [✓]
Size: 1 row × 1 column
```

### Settings
```
Data Storage Path: C:\AlpacaData
[Browse...] [Test Connection]
Refresh Interval: 5 seconds
[Save] [Cancel]
```

## 💡 Tips

1. **Color Schemes:** Temperature scheme has gradient enabled for smooth transitions
2. **Chart Performance:** Use aggregation for long periods (7+ days)
3. **Quick Access:** Mark up to 5 dashboards for one-click switching
4. **Historical Data:** Uncheck "Use Now" to view past data
5. **Multiple Metrics:** Can show any combination (e.g. Temp + Humidity)
6. **Window Settings:** Size/position/theme automatically saved

## 🐛 Troubleshooting

### No Data Displayed
- Check File → Settings → Test Connection
- Verify DataStorage folder path
- Ensure ASCOM device is running

### Drag-and-Drop Not Working
- Make sure you're in Dashboard Editor (not main view)
- Check that tile is not over buttons (Edit/Delete)

### Chart Empty
- Verify metric aggregations are configured
- Check date range and aggregation interval
- Ensure data exists for selected period

### Settings Not Saved
- Check write permissions to: %APPDATA%\SafetyMonitorView
- Settings saved automatically on changes

## 📝 Requirements

- .NET 8.0 SDK
- Windows 10/11
- MaterialSkin.2 (2.1.4)
- ScottPlot.WinForms (5.0.54)
- DataStorage project

## 🎉 Complete!

All 4 components fully implemented:
1. ✅ ValueTile with color schemes
2. ✅ ChartTile with ScottPlot
3. ✅ Tile editors (Value & Chart)
4. ✅ SettingsForm
5. ✅ Settings persistence (NEW!)

Ready to use!
