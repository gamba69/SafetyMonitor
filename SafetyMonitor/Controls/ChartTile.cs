using MaterialSkin;
using SafetyMonitor.Forms;
using SafetyMonitor.Models;
using SafetyMonitor.Services;
using ScottPlot;
using ScottPlot.WinForms;
using System.Data;
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;

namespace SafetyMonitor.Controls;

/// <summary>
/// Represents chart tile and encapsulates its related behavior and state.
/// </summary>
public class ChartTile : Panel {

    #region Private Fields

    private readonly ChartTileConfig _config;
    private readonly DataService _dataService;
    private readonly ValueSchemeService _valueSchemeService = new();
    private readonly ColorSchemeService _colorSchemeService = new();
    private readonly ChartTableExportService _chartTableExportService = new();
    private readonly List<ScottPlot.IYAxis> _extraAxes = [];
    private readonly Dictionary<MetricType, ScottPlot.IYAxis> _metricAxes = [];
    private readonly Dictionary<MetricType, string> _metricAxisLabels = [];
    private readonly Dictionary<MetricType, Color> _metricFallbackColors = [];
    private readonly HashSet<MetricType> _metricsWithData = [];
#pragma warning disable IDE0028
    private readonly Dictionary<string, SafetyMonitor.Models.ColorScheme> _colorSchemesByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ValueScheme> _valueSchemesByName = new(StringComparer.Ordinal);
#pragma warning restore IDE0028
    private bool _initialized;
    private readonly ThemedMenuRenderer _contextMenuRenderer = new();
    private const int MenuIconSize = 22;
    private const float PlotContextMenuFontSize = 10f;
    private ContextMenuStrip? _plotContextMenu;
    private List<ChartPeriodPreset> _periodPresets = [];
    private ThemedComboBox? _periodSelector;
    private FlowLayoutPanel? _autoPeriodPanel;
    private Panel? _autoRangeSpacer;
    private bool _suppressPeriodChange;
    private bool _suppressStaticRangeChange;
    private bool _suppressPauseChange;
    private bool _isStaticMode;
    private ThemedDateTimePicker? _staticStartPicker;
    private ThemedDateTimePicker? _staticEndPicker;
    private FlowLayoutPanel? _staticRangePanel;
    private Button? _autoShiftLeftButton;
    private Button? _autoShiftRightButton;
    private Button? _staticShiftLeftButton;
    private Button? _staticShiftRightButton;
    private System.Windows.Forms.Timer? _staticModeTimer;
    private TimeSpan _staticModeTimeout = TimeSpan.FromMinutes(2);
    private double _staticAggregationPresetMatchTolerancePercent = 10;
    private int _staticAggregationTargetPointCount = 300;
    private DateTime _lastChartInteractionUtc = DateTime.MinValue;
    private double? _lastXAxisMin;
    private double? _lastXAxisMax;
    private ChartPeriod _autoPeriod;
    private TimeSpan? _autoCustomDuration;
    private string _autoPeriodPresetUid = "";
    private FormsPlot? _plot;
    private Label? _titleLabel;
    private Panel? _topPanel;
    private Panel? _modeSwitchContainer;
    private Panel? _modeSegmentPanel;
    private Panel? _inspectorHostPanel;
    private Panel? _inspectorSegmentPanel;
    private CheckBox? _pauseModeButton;
    private RadioButton? _autoModeButton;
    private RadioButton? _staticModeButton;
    private Label? _countdownLabel;
    private RichTextBox? _aggregationInfoTextBox;
    private RichTextBox? _hoverInfoTextBox;
    private double? _lastHoverAnchorX;
    private CheckBox? _inspectorButton;
    private object? _hoverVerticalLine;
    private readonly List<SeriesHoverSnapshot> _hoverSeries = [];
    private bool _inspectorActive;
    private bool _staticModePaused;
    private int _lastHorizontalPointCount;
    private const int HeaderControlHeight = 28;
    private const int StaticRangePanelWidth = 448;
    private const int AutoRangePanelWidth = 448;
    private const int AutoRangePanelLeftPadding = 0;
    private const int AutoRangeSpacerWidth = 240;
    private const int ModeSwitchContainerWidth = 130;
    private const int HeaderButtonSpacing = 2;
    private const int HeaderButtonY = 2;
    private const int HeaderControlButtonSize = HeaderControlHeight;
    private const int HeaderControlRightPadding = 2;
    private const int PngExportScaleFactor = 2;
    private const double LegendCornerOccupancyRoundingPercent = 20d;
    private int _availableLinkGroups = ChartLinkGroupInfo.MaxUsedGroups;
    private readonly Dictionary<ChartLinkGroup, string> _linkGroupPeriodShortNames = [];
    private static readonly Color LightThemePrimaryColor = Color.FromArgb(66, 66, 66);
    private static readonly Color LightThemeBorderColor = Color.FromArgb(52, 52, 52);
    private static readonly Color DarkThemeBorderColor = Color.FromArgb(53, 70, 76);

    /// <summary>
    /// Represents series hover snapshot and encapsulates its related behavior and state.
    /// </summary>
    private sealed class SeriesHoverSnapshot {
        /// <summary>
        /// Gets or sets the label for series hover snapshot. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string Label { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the unit for series hover snapshot. Stores textual configuration or display metadata used by application flows.
        /// </summary>
        public string Unit { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the metric for series hover snapshot. Holds part of the component state used by higher-level application logic.
        /// </summary>
        public MetricType Metric { get; init; }
        /// <summary>
        /// Gets or sets the series color for series hover snapshot. Controls visual presentation used by themed rendering and UI styling.
        /// </summary>
        public Color SeriesColor { get; set; } = Color.White;
        /// <summary>
        /// Gets or sets the base color for series hover snapshot. Controls visual presentation used by themed rendering and UI styling.
        /// </summary>
        public Color BaseColor { get; init; } = Color.White;
        /// <summary>
        /// Gets or sets the xs for series hover snapshot. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double[] Xs { get; init; } = [];
        /// <summary>
        /// Gets or sets the ys for series hover snapshot. Stores a numeric value used by calculations, thresholds, or telemetry display.
        /// </summary>
        public double[] Ys { get; init; } = [];
        /// <summary>
        /// Gets or sets the color scheme name for series hover snapshot. Controls visual presentation used by themed rendering and UI styling.
        /// </summary>
        public string ColorSchemeName { get; init; } = string.Empty;
        /// <summary>
        /// Gets or sets the legend plottable for series hover snapshot. Holds part of the component state used by higher-level application logic.
        /// </summary>
        public object? LegendPlottable { get; init; }
        /// <summary>
        /// Gets or sets the value scheme name for series hover snapshot. Controls visual presentation used by themed rendering and UI styling.
        /// </summary>
        public string ValueSchemeName { get; init; } = string.Empty;
    }

    #endregion Private Fields

    #region Public Events

    public event Action<ChartTile, string>? PeriodChanged;
    public event Action<ChartTile, DateTime, DateTime>? StaticRangeChanged;
    public event Action<ChartTile>? AutoModeRestored;
    public event Action<ChartTile>? ViewSettingsChanged;
    public event Action<ChartTile, bool>? InspectorToggled;
    public event Action<ChartTile, bool>? PlotHoverPresenceChanged;
    public event Action<ChartTile, double>? HoverAnchorChanged;
    public event Action<ChartTile, bool>? StaticPauseChanged;
    public event Action<ChartTile>? LinkGroupChanged;
    public event Action<ChartTile>? EditRequested;
    public event Action<ChartTile>? EditDashboardRequested;

    #endregion Public Events

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartTile"/> class.
    /// </summary>
    /// <param name="config">Input value for config.</param>
    /// <param name="dataService">Input value for data service.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public ChartTile(ChartTileConfig config, DataService dataService) {
        _config = config;
        _dataService = dataService;
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.None;
        Padding = new Padding(1);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        UpdateStyles();

        // Set a valid font to prevent GDI+ errors during auto-scaling
        // when child controls are added. The font is inherited from parent
        // and if parent has an invalid font (e.g., "Segoe UI" not installed),
        // Controls.Add() will throw when accessing FontHandle.
        Font = SystemFonts.DefaultFont;
        _autoPeriod = _config.Period;
        _autoCustomDuration = _config.CustomPeriodDuration;
        _autoPeriodPresetUid = _config.PeriodPresetUid;
        _staticModePaused = _config.StaticModePaused;

        // Legacy configs may persist a Custom period from static mode.
        // Auto mode must never keep ad-hoc custom windows.
        if (_autoPeriod == ChartPeriod.Custom && !_autoCustomDuration.HasValue) {
            _autoPeriod = ChartPeriod.Last24Hours;
            _config.Period = _autoPeriod;
        }
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Sets the available link groups for series hover snapshot.
    /// </summary>
    /// <param name="usedGroups">Input value for used groups.</param>
    public void SetAvailableLinkGroups(int usedGroups) {
        _availableLinkGroups = ChartLinkGroupInfo.NormalizeUsedGroups(usedGroups);
        _config.LinkGroup = ChartLinkGroupInfo.NormalizeGroup(_config.LinkGroup, _availableLinkGroups);
    }

    /// <summary>
    /// Sets the link group period short names for series hover snapshot.
    /// </summary>
    /// <param name="periodShortNames">Input value for period short names.</param>
    public void SetLinkGroupPeriodShortNames(IReadOnlyDictionary<ChartLinkGroup, string> periodShortNames) {
        _linkGroupPeriodShortNames.Clear();
        foreach (var pair in periodShortNames) {
            _linkGroupPeriodShortNames[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Returns the set of <see cref="DataService.GetChartData"/> parameter tuples that.
    /// <see cref="RefreshData"/> will request.  Called on the UI thread so that
    /// <see cref="DashboardPanel.RefreshDataAsync"/> can pre-fetch the data on a
    /// background thread before the synchronous RefreshData renders the chart.
    /// </summary>
    public IEnumerable<(ChartPeriod Period, DateTime? Start, DateTime? End, TimeSpan? Duration, TimeSpan? Interval, DataStorage.Models.AggregationFunction Function)> GetDataFetchRequirements() {
        var aggregationInterval = ResolveAggregationInterval();
        var seenFunctions = new HashSet<DataStorage.Models.AggregationFunction>();
        foreach (var agg in _config.MetricAggregations) {
            if (seenFunctions.Add(agg.Function)) {
                yield return (
                    _config.Period,
                    _config.CustomStartTime,
                    _isStaticMode ? _config.CustomEndTime : null,
                    _config.CustomPeriodDuration,
                    aggregationInterval,
                    agg.Function);
            }
        }
    }

    /// <summary>
    /// Refreshes the data for series hover snapshot.
    /// </summary>
    public void RefreshData() {
        if (_plot == null) {
            return;
        }

        _plot.Plot.Clear();
        ClearExtraAxes();
        _hoverSeries.Clear();
        _hoverVerticalLine = null;

        // Reset the default left axis label
        _plot.Plot.Axes.Left.Label.Text = string.Empty;

        if (_config.MetricAggregations.Count == 0) {
            _lastHorizontalPointCount = 0;
            UpdateAggregationInfoLabel(ResolveAggregationInterval());
            _plot.Plot.Title("No metrics configured");
            ApplyThemeColors();
            RememberCurrentXAxisLimits();
            _plot.Refresh();
            return;
        }

        var aggregationInterval = ResolveAggregationInterval();
        UpdateAggregationInfoLabel(aggregationInterval);
        var isLightTheme = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;

        // Determine distinct metric types to decide if multiple Y axes are needed
        var distinctMetrics = _config.MetricAggregations
            .Select(a => a.Metric)
            .Distinct()
            .ToList();

        bool useMultipleAxes = distinctMetrics.Count > 1;
        var axisMap = new Dictionary<MetricType, ScottPlot.IYAxis>();
        var axisStyleMap = new Dictionary<MetricType, (string LabelText, ScottPlot.Color Color)>();
        var hasLogYAxis = distinctMetrics.Any(metric => MetricDisplaySettingsStore.GetSettingOrDefault(metric).LogY);

        if (useMultipleAxes) {
            for (int i = 0; i < distinctMetrics.Count; i++) {
                var metric = distinctMetrics[i];
                var labelText = BuildAxisLabel(metric, _plot?.Height ?? Height);

                // Use the color of the first series for this metric to tint the axis
                var representativeColor = ScottPlot.Color.FromColor(
                    _config.MetricAggregations.First(a => a.Metric == metric).GetColorForTheme(isLightTheme));
                axisStyleMap[metric] = (labelText, representativeColor);

                if (i == 0) {
                    // First metric — use the built-in left Y axis
                    StyleAxis(_plot!.Plot.Axes.Left, labelText, representativeColor);
                    axisMap[metric] = _plot.Plot.Axes.Left;
                } else if (i == 1) {
                    // Second metric — use the built-in right Y axis
                    StyleAxis(_plot!.Plot.Axes.Right, labelText, representativeColor);
                    axisMap[metric] = _plot.Plot.Axes.Right;
                } else {
                    // 3rd+ metrics — create additional right-side Y axes
                    var extraAxis = _plot!.Plot.Axes.AddRightAxis();
                    StyleAxis(extraAxis, labelText, representativeColor);
                    axisMap[metric] = extraAxis;
                    _extraAxes.Add(extraAxis);
                }
            }
        } else if (distinctMetrics.Count == 1) {
            var metric = distinctMetrics[0];
            var labelText = BuildAxisLabel(metric, _plot?.Height ?? Height);
            var representativeColor = ScottPlot.Color.FromColor(
                _config.MetricAggregations.First(a => a.Metric == metric).GetColorForTheme(isLightTheme));
            axisStyleMap[metric] = (labelText, representativeColor);
            StyleAxis(_plot!.Plot.Axes.Left, labelText, representativeColor);
            axisMap[metric] = _plot.Plot.Axes.Left;
        }

        var hasVisibleSeries = false;
        var metricsWithData = new HashSet<MetricType>();
        var horizontalPoints = new HashSet<double>();
        var visiblePlotPoints = new List<(double X, double Y)>();
        var colorSchemes = _colorSchemeService.LoadSchemes()
            .GroupBy(scheme => scheme.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        _colorSchemesByName.Clear();
        foreach (var (name, scheme) in colorSchemes) {
            _colorSchemesByName[name] = scheme;
        }
        var valueSchemes = _valueSchemeService.LoadSchemes()
            .GroupBy(scheme => scheme.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        _valueSchemesByName.Clear();
        foreach (var (name, scheme) in valueSchemes) {
            _valueSchemesByName[name] = scheme;
        }

        // Cache DB results per AggregationFunction to avoid duplicate queries.
        // The DB returns all columns in each row, so the same result set can be
        // reused for every MetricAggregation that shares the same function.
        var dataCache = new Dictionary<DataStorage.Models.AggregationFunction, List<DataStorage.Models.ObservingData>>();

        foreach (var agg in _config.MetricAggregations) {
            if (!dataCache.TryGetValue(agg.Function, out var data)) {
                data = _dataService.GetChartData(
                    _config.Period,
                    _config.CustomStartTime,
                    _isStaticMode ? _config.CustomEndTime : null,
                    _config.CustomPeriodDuration,
                    aggregationInterval, agg.Function);
                dataCache[agg.Function] = data;
            }
            if (data.Count == 0) {
                continue;
            }
            var displaySetting = MetricDisplaySettingsStore.GetSettingOrDefault(agg.Metric);
            var timestamps = data.Select(d => DateTime.SpecifyKind(d.Timestamp, DateTimeKind.Utc)
                .ToLocalTime()
                .ToOADate()).ToArray();
            var values = data.Select(d => agg.Metric.GetValue(d) ?? double.NaN).ToArray();
            var plotValues = values
                .Select(value => displaySetting.LogY
                    ? (value > 0d ? Math.Log10(value) : double.NaN)
                    : value)
                .ToArray();
            var validData = timestamps
                .Zip(values, (time, rawValue) => new { Time = time, RawValue = rawValue })
                .Zip(plotValues, (item, plotValue) => new { item.Time, item.RawValue, PlotValue = plotValue })
                .Where(x => !double.IsNaN(x.PlotValue))
                .ToArray();
            if (validData.Length == 0) {
                continue;
            }
            var validTimes = validData.Select(x => x.Time).ToArray();
            var validPlotValues = validData.Select(x => x.PlotValue).ToArray();
            var validRawValues = validData.Select(x => x.RawValue).ToArray();
            foreach (var time in validTimes) {
                horizontalPoints.Add(time);
            }
            for (int i = 0; i < validTimes.Length; i++) {
                visiblePlotPoints.Add((validTimes[i], validPlotValues[i]));
            }
            hasVisibleSeries = true;
            metricsWithData.Add(agg.Metric);
            var yAxis = useMultipleAxes && axisMap.TryGetValue(agg.Metric, out var mappedAxis)
                ? mappedAxis
                : null;
            SafetyMonitor.Models.ColorScheme? colorScheme = null;
            var hasScheme = !string.IsNullOrWhiteSpace(agg.ColorSchemeName)
                && colorSchemes.TryGetValue(agg.ColorSchemeName, out colorScheme);
            object? legendPlottable = null;
            if (hasScheme && colorScheme != null) {
                legendPlottable = AddSchemeOnlySeries(validTimes, validPlotValues, validRawValues, agg, colorScheme, yAxis);
            } else {
                var scatter = _plot.Plot.Add.Scatter(validTimes, validPlotValues);
                legendPlottable = scatter;
                scatter.LegendText = agg.Label;
                scatter.Color = ScottPlot.Color.FromColor(agg.GetColorForTheme(isLightTheme));
                scatter.LineWidth = agg.LineWidth;
                scatter.MarkerLineWidth = agg.LineWidth;
                scatter.MarkerSize = agg.ShowMarkers
                    ? Math.Max(agg.LineWidth * 2f, 5f)
                    : 0;
                scatter.MarkerShape = MarkerShape.VerticalBar;
                scatter.Smooth = agg.Smooth;
                ApplySmoothTension(scatter, agg);

                // Assign this series to its dedicated Y axis when multiple axes are active
                if (yAxis != null) {
                    scatter.Axes.YAxis = yAxis;
                }
            }

            var baseColor = agg.GetColorForTheme(isLightTheme);
            var initialSeriesColor = hasScheme && colorScheme != null
                ? colorScheme.GetColor(validRawValues[^1])
                : baseColor;
            _hoverSeries.Add(new SeriesHoverSnapshot {
                Label = string.IsNullOrWhiteSpace(agg.Label)
                    ? agg.Metric.GetDisplayName()
                    : agg.Label,
                Unit = agg.Metric.GetUnit() ?? string.Empty,
                Metric = agg.Metric,
                SeriesColor = initialSeriesColor,
                BaseColor = baseColor,
                Xs = validTimes,
                Ys = validRawValues,
                ColorSchemeName = agg.ColorSchemeName ?? string.Empty,
                LegendPlottable = legendPlottable,
                ValueSchemeName = agg.ValueSchemeName ?? string.Empty
            });
        }

        _metricAxes.Clear();
        foreach (var (metric, axis) in axisMap) {
            _metricAxes[metric] = axis;
        }
        _metricAxisLabels.Clear();
        foreach (var (metric, style) in axisStyleMap) {
            _metricAxisLabels[metric] = style.LabelText;
        }
        _metricFallbackColors.Clear();
        foreach (var agg in _config.MetricAggregations) {
            if (!_metricFallbackColors.ContainsKey(agg.Metric)) {
                _metricFallbackColors[agg.Metric] = agg.GetColorForTheme(isLightTheme);
            }
        }
        _metricsWithData.Clear();
        foreach (var metric in metricsWithData) {
            _metricsWithData.Add(metric);
        }

        _lastHorizontalPointCount = horizontalPoints.Count;
        UpdateAggregationInfoLabel(aggregationInterval);

        _plot.Plot.Axes.DateTimeTicksBottom();

        // Clear any previously added axis rules
        _plot.Plot.Axes.Rules.Clear();

        var (startTime, endTime) = GetConfiguredPeriodRange();
        if (_isStaticMode) {
            _plot.Plot.Axes.SetLimitsX(startTime.ToOADate(), endTime.ToOADate());
        } else if (hasVisibleSeries) {
            _plot.Plot.Axes.AutoScale();
        } else {
            _plot.Plot.Axes.SetLimitsX(startTime.ToOADate(), endTime.ToOADate());
        }

        // Apply per-metric Y-axis rules from the global store
        ApplyAxisRules(axisMap);

        ApplyThemeColors();
        ApplyYAxisVisibility(axisMap, axisStyleMap, metricsWithData);
        UpdateVisualColorsForContext(_inspectorActive ? _lastHoverAnchorX : null);
        if (_config.ShowLegend && _config.MetricAggregations.Count > 1) {
            _plot.Plot.ShowLegend();
            ApplyAdaptiveLegendAlignment(visiblePlotPoints);
        } else {
            _plot.Plot.HideLegend();
        }

        if (_config.ShowGrid) {
            _plot.Plot.ShowGrid();
            var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
            _plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromColor(
                isLight ? Color.LightGray : Color.FromArgb(53, 70, 76));
            _plot.Plot.Grid.MajorLineWidth = 1;

            if (hasLogYAxis) {
                _plot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromColor(
                    isLight ? Color.FromArgb(55, 0, 0, 0) : Color.FromArgb(45, 255, 255, 255));
                _plot.Plot.Grid.MinorLineWidth = 1;
            }
        } else {
            _plot.Plot.HideGrid();
        }
        RememberCurrentXAxisLimits();
        EnsureInspectorState();
        if (_inspectorActive && _lastHoverAnchorX.HasValue) {
            ShowInspectorAt(_lastHoverAnchorX.Value);
        }
        _plot.Refresh();
    }

    /// <summary>
    /// Applies adaptive legend alignment for chart tile to reduce data overlap.
    /// </summary>
    /// <param name="visiblePlotPoints">Input value for visible plot points.</param>
    private void ApplyAdaptiveLegendAlignment(IReadOnlyList<(double X, double Y)> visiblePlotPoints) {
        if (_plot == null || visiblePlotPoints.Count == 0) {
            return;
        }

        var limits = _plot.Plot.Axes.GetLimits();
        if (!double.IsFinite(limits.Left) || !double.IsFinite(limits.Right)
            || !double.IsFinite(limits.Bottom) || !double.IsFinite(limits.Top)
            || limits.Left >= limits.Right || limits.Bottom >= limits.Top) {
            return;
        }

        var centerX = (limits.Left + limits.Right) / 2d;
        var centerY = (limits.Bottom + limits.Top) / 2d;

        var lowerRight = 0;
        var upperRight = 0;
        var lowerLeft = 0;
        var upperLeft = 0;

        foreach (var (x, y) in visiblePlotPoints) {
            if (!double.IsFinite(x) || !double.IsFinite(y)) {
                continue;
            }

            if (x < limits.Left || x > limits.Right || y < limits.Bottom || y > limits.Top) {
                continue;
            }

            if (x >= centerX) {
                if (y <= centerY) {
                    lowerRight++;
                } else {
                    upperRight++;
                }
            } else if (y <= centerY) {
                lowerLeft++;
            } else {
                upperLeft++;
            }
        }

        var priority = new (string Corner, int Count)[] {
            ("LowerRight", lowerRight),
            ("UpperRight", upperRight),
            ("LowerLeft", lowerLeft),
            ("UpperLeft", upperLeft)
        };

        var totalVisiblePoints = lowerRight + upperRight + lowerLeft + upperLeft;
        var roundingStep = Math.Max(1d, totalVisiblePoints * (LegendCornerOccupancyRoundingPercent / 100d));

        var targetCorner = priority
            .OrderBy(candidate => Math.Round(candidate.Count / roundingStep, MidpointRounding.AwayFromZero))
            .ThenBy(candidate => Array.FindIndex(priority, option => option.Corner == candidate.Corner))
            .Select(candidate => candidate.Corner)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(targetCorner)) {
            return;
        }

        var legend = _plot.Plot.Legend;
        var alignmentProperty = legend.GetType().GetProperty("Alignment");
        if (alignmentProperty?.PropertyType.IsEnum != true) {
            return;
        }

        if (Enum.TryParse(alignmentProperty.PropertyType, targetCorner, ignoreCase: true, out var parsedAlignment)) {
            alignmentProperty.SetValue(legend, parsedAlignment);
        }
    }

    /// <summary>
    /// Adds the scheme-only series for chart tile where segment and marker colors are resolved from metric values.
    /// </summary>
    /// <param name="times">Input value for times.</param>
    /// <param name="plotValues">Input value for plot values.</param>
    /// <param name="rawValues">Input value for raw values.</param>
    /// <param name="aggregation">Input value for aggregation.</param>
    /// <param name="colorScheme">Input value for color scheme.</param>
    /// <param name="yAxis">Input value for y axis.</param>
    private object? AddSchemeOnlySeries(
        double[] times,
        double[] plotValues,
        double[] rawValues,
        MetricAggregation aggregation,
        SafetyMonitor.Models.ColorScheme colorScheme,
        ScottPlot.IYAxis? yAxis) {
        var hasLegendLabel = false;

        if (times.Length == 1) {
            var color = ScottPlot.Color.FromColor(colorScheme.GetColor(rawValues[0]));
            var singlePoint = _plot!.Plot.Add.Scatter([times[0]], new[] { plotValues[0] });
            singlePoint.LegendText = aggregation.Label;
            singlePoint.Color = color;
            singlePoint.LineWidth = 0;
            singlePoint.MarkerLineWidth = aggregation.LineWidth;
            singlePoint.MarkerSize = aggregation.ShowMarkers
                ? Math.Max(aggregation.LineWidth * 2f, 5f)
                : 0;
            singlePoint.MarkerShape = MarkerShape.VerticalBar;
            if (yAxis != null) {
                singlePoint.Axes.YAxis = yAxis;
            }
            return singlePoint;
        }

        var segmentColors = new Color[times.Length - 1];
        for (int i = 0; i < segmentColors.Length; i++) {
            var referenceValue = (rawValues[i] + rawValues[i + 1]) / 2.0;
            segmentColors[i] = colorScheme.GetColor(referenceValue);
        }

        var runStart = 0;
        object? legendPlottable = null;
        for (int i = 1; i <= segmentColors.Length; i++) {
            var runEnded = i == segmentColors.Length || segmentColors[i] != segmentColors[runStart];
            if (!runEnded) {
                continue;
            }

            var runLength = i - runStart;
            var pointCount = runLength + 1;
            var runTimes = new double[pointCount];
            var runPlotValues = new double[pointCount];
            Array.Copy(times, runStart, runTimes, 0, pointCount);
            Array.Copy(plotValues, runStart, runPlotValues, 0, pointCount);

            var segment = _plot!.Plot.Add.Scatter(runTimes, runPlotValues);
            segment.LegendText = !hasLegendLabel ? aggregation.Label : string.Empty;
            hasLegendLabel = true;
            legendPlottable ??= segment;
            segment.Color = ScottPlot.Color.FromColor(segmentColors[runStart]);
            segment.LineWidth = aggregation.LineWidth;
            segment.MarkerSize = 0;
            segment.Smooth = aggregation.Smooth;
            ApplySmoothTension(segment, aggregation);
            if (yAxis != null) {
                segment.Axes.YAxis = yAxis;
            }

            runStart = i;
        }

        if (!aggregation.ShowMarkers) {
            return legendPlottable;
        }

        var markerSize = Math.Max(aggregation.LineWidth * 2f, 5f);
        for (int i = 0; i < times.Length; i++) {
            var marker = _plot!.Plot.Add.Scatter([times[i]], new[] { plotValues[i] });
            marker.LegendText = string.Empty;
            marker.Color = ScottPlot.Color.FromColor(colorScheme.GetColor(rawValues[i]));
            marker.LineWidth = 0;
            marker.MarkerLineWidth = aggregation.LineWidth;
            marker.MarkerSize = markerSize;
            marker.MarkerShape = MarkerShape.VerticalBar;
            if (yAxis != null) {
                marker.Axes.YAxis = yAxis;
            }
        }

        return legendPlottable;
    }

    /// <summary>
    /// Updates the theme for series hover snapshot.
    /// </summary>
    public void UpdateTheme() {
        if (_plot == null || _titleLabel == null) {
            return;
        }

        ApplyThemeColors();
        ApplyTileColors();
        ApplyPlotContextMenuTheme();
        UpdateModeSwitchAppearance();
        UpdateAggregationInfoLabel(ResolveAggregationInterval());
        // Restore title font that may have been overwritten by MaterialSkinManager
        var expectedFont = CreateSafeFont("Segoe UI", 11, System.Drawing.FontStyle.Bold);
        if (_titleLabel.Font.Size != expectedFont.Size || _titleLabel.Font.Style != expectedFont.Style) {
            var oldFont = _titleLabel.Font;
            _titleLabel.Font = expectedFont;
            oldFont.Dispose();
        } else {
            expectedFont.Dispose();
        }
        // Rebuild plottables so series colors are recalculated for the active theme.
        RefreshData();
        Invalidate(true);
    }

    /// <summary>
    /// Sets the period preset for series hover snapshot.
    /// </summary>
    /// <param name="periodPresetUid">Identifier of period preset.</param>
    /// <param name="refreshData">Input value for refresh data.</param>
    public void SetPeriodPreset(string periodPresetUid, bool refreshData = true) {
        if (_isStaticMode) {
            SetStaticMode(false);
        }

        _config.PeriodPresetUid = periodPresetUid;
        if (_periodSelector != null) {
            try {
                _suppressPeriodChange = true;
                SetSelectedPeriodPreset();
            } finally {
                _suppressPeriodChange = false;
            }
        }

        if (refreshData) {
            RefreshData();
        }
    }


    /// <summary>
    /// Sets the static mode timeout for series hover snapshot.
    /// </summary>
    /// <param name="timeout">Input value for timeout.</param>
    public void SetStaticModeTimeout(TimeSpan timeout) {
        _staticModeTimeout = timeout < TimeSpan.FromSeconds(10)
            ? TimeSpan.FromSeconds(10)
            : timeout;
    }

    /// <summary>
    /// Sets the static aggregation settings for series hover snapshot.
    /// </summary>
    /// <param name="presetMatchTolerancePercent">Input value for preset match tolerance percent.</param>
    /// <param name="targetPointCount">Input value for target point count.</param>
    public void SetStaticAggregationSettings(double presetMatchTolerancePercent, int targetPointCount) {
        _staticAggregationPresetMatchTolerancePercent = Math.Clamp(presetMatchTolerancePercent, 0, 100);
        _staticAggregationTargetPointCount = Math.Max(2, targetPointCount);

        UpdateAggregationInfoLabel(ResolveAggregationInterval());

        if (_isStaticMode) {
            RefreshData();
        }
    }

    /// <summary>
    /// Sets the static range for series hover snapshot.
    /// </summary>
    /// <param name="startLocal">Input value for start local.</param>
    /// <param name="endLocal">Input value for end local.</param>
    /// <param name="raiseEvents">Input value for raise events.</param>
    public void SetStaticRange(DateTime startLocal, DateTime endLocal, bool raiseEvents = true) {
        if (endLocal <= startLocal) {
            return;
        }

        EnterStaticMode(startLocal, endLocal, raiseEvents);
    }

    /// <summary>
    /// Executes exit static mode as part of series hover snapshot processing.
    /// </summary>
    /// <param name="raiseEvents">Input value for raise events.</param>
    public void ExitStaticMode(bool raiseEvents = true) {
        if (!_isStaticMode) {
            return;
        }

        SetStaticMode(false);
        _staticModePaused = false;
        _config.StaticModePaused = false;
        _config.Period = _autoPeriod;
        _config.CustomPeriodDuration = _autoCustomDuration;
        _config.PeriodPresetUid = _autoPeriodPresetUid;
        _config.CustomStartTime = null;
        _config.CustomEndTime = null;
        RefreshData();

        if (raiseEvents) {
            AutoModeRestored?.Invoke(this);
        }
    }

    /// <summary>
    /// Sets the static paused for series hover snapshot.
    /// </summary>
    /// <param name="paused">Input value for paused.</param>
    /// <param name="raiseEvents">Input value for raise events.</param>
    public void SetStaticPaused(bool paused, bool raiseEvents = true) {
        if (_staticModePaused == paused && _config.StaticModePaused == paused) {
            return;
        }

        _staticModePaused = paused;
        _config.StaticModePaused = paused;

        if (!paused) {
            _lastChartInteractionUtc = DateTime.UtcNow;
        }

        if (_pauseModeButton != null && _pauseModeButton.Checked != paused) {
            try {
                _suppressPauseChange = true;
                _pauseModeButton.Checked = paused;
            } finally {
                _suppressPauseChange = false;
            }
        }

        UpdateCountdownLabel();
        UpdateModeSwitchAppearance();

        if (raiseEvents) {
            StaticPauseChanged?.Invoke(this, paused);
            ViewSettingsChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// Sets the inspector enabled for series hover snapshot.
    /// </summary>
    /// <param name="enabled">Input value for enabled.</param>
    /// <param name="raiseEvents">Input value for raise events.</param>
    public void SetInspectorEnabled(bool enabled, bool raiseEvents = true) {
        if (_config.ShowInspector == enabled && _inspectorActive == enabled) {
            return;
        }

        _config.ShowInspector = enabled;
        EnsureInspectorState();
        UpdateModeSwitchAppearance();

        if (raiseEvents) {
            InspectorToggled?.Invoke(this, enabled);
            ViewSettingsChanged?.Invoke(this);
        }
    }

    public bool IsInspectorEnabled => _config.ShowInspector;
    public ChartTileConfig Config => _config;

    /// <summary>
    /// Shows the inspector at for series hover snapshot.
    /// </summary>
    /// <param name="x">Input value for x.</param>
    public void ShowInspectorAt(double x) {
        if (!_inspectorActive || _plot == null || _hoverSeries.Count == 0) {
            return;
        }

        var anchor = FindNearestAnchorX(x);
        if (!anchor.HasValue) {
            return;
        }

        if (_lastHoverAnchorX.HasValue && Math.Abs(_lastHoverAnchorX.Value - anchor.Value) < 1e-9) {
            return;
        }

        UpdateHoverVerticalLine(anchor.Value);
        UpdateHoverInfo(anchor.Value);
        _plot.Refresh();
    }

    /// <summary>
    /// Executes clear inspector display as part of series hover snapshot processing.
    /// </summary>
    public void ClearInspectorDisplay() {
        HideInspector();
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Executes on font changed as part of series hover snapshot processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        if (_initialized) {
            ApplyTileColors();
        }
        if (_initialized && _titleLabel != null) {
            var oldFont = _titleLabel.Font;
            _titleLabel.Font = CreateSafeFont("Segoe UI", 11, System.Drawing.FontStyle.Bold);
            oldFont.Dispose();
        }
    }

    /// <summary>
    /// Executes on handle created as part of series hover snapshot processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);

        // Initialize UI only after handle is created (GDI+ is ready)
        if (!_initialized) {
            _initialized = true;
            InitializeUI();
        }
    }

    /// <summary>
    /// Executes on paint as part of series hover snapshot processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        DrawTileBorder(e.Graphics);
    }

    /// <summary>
    /// Executes dispose as part of series hover snapshot processing.
    /// </summary>
    /// <param name="disposing">Input value for disposing.</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            ChartPeriodPresetStore.PresetsChanged -= HandlePresetsChanged;
            MetricAxisRuleStore.RulesChanged -= HandleAxisRulesChanged;
            MetricDisplaySettingsStore.SettingsChanged -= HandleAxisRulesChanged;
            DetachTileContextMenuHandlers(this);
            _plot?.MouseUp -= Plot_MouseUp;
            _plot?.MouseMove -= Plot_MouseMove;
            _plot?.MouseEnter -= Plot_MouseEnter;
            _plot?.MouseLeave -= Plot_MouseLeave;
            _plot?.MouseWheel -= Plot_MouseWheel;
            _plot?.MouseDoubleClick -= Plot_MouseDoubleClick;
            if (_staticModeTimer != null) {
                _staticModeTimer.Stop();
                _staticModeTimer.Tick -= StaticModeTimer_Tick;
                _staticModeTimer.Dispose();
                _staticModeTimer = null;
            }
            _plotContextMenu?.Dispose();
            _plotContextMenu = null;
        }
        base.Dispose(disposing);
    }

    #endregion Protected Methods

    #region Private Methods

    /// <summary>
    /// Builds the axis label for series hover snapshot.
    /// </summary>
    /// <param name="metric">Input value for metric.</param>
    /// <returns>The resulting string value.</returns>
    private static string BuildAxisLabel(MetricType metric, int plotHeight) {
        var displayName = metric.GetDisplayName();
        var shortName = metric.GetShortName();
        var unit = metric.GetUnit();

        // 4-step degradation:
        // 1) long + unit
        // 2) short + unit
        // 3) short
        // 4) no text (color-only axis)
        if (plotHeight >= 220) {
            return string.IsNullOrWhiteSpace(unit)
                ? displayName
                : $"{displayName} ({unit})";
        }

        if (plotHeight >= 170) {
            return string.IsNullOrWhiteSpace(unit)
                ? shortName
                : $"{shortName} ({unit})";
        }

        if (plotHeight >= 130) {
            return shortName;
        }

        return string.Empty;
    }

    /// <summary>
    /// Applies the smooth tension for series hover snapshot.
    /// </summary>
    /// <param name="scatter">Input value for scatter.</param>
    /// <param name="aggregation">Input value for aggregation.</param>
    private static void ApplySmoothTension(object scatter, MetricAggregation aggregation) {
        if (!aggregation.Smooth) {
            return;
        }

        var smoothTensionProperty = scatter.GetType().GetProperty("SmoothTension");
        if (smoothTensionProperty == null || !smoothTensionProperty.CanWrite) {
            return;
        }

        var clampedTension = Math.Clamp(aggregation.Tension, 0f, 3f);
        smoothTensionProperty.SetValue(scatter, clampedTension);
    }

    /// <summary>
    /// Creates the safe font for series hover snapshot.
    /// </summary>
    /// <param name="familyName">Input value for family name.</param>
    /// <param name="emSize">Input value for em size.</param>
    /// <param name="style">Input value for style.</param>
    /// <returns>The result of the operation.</returns>
    private static Font CreateSafeFont(string familyName, float emSize, System.Drawing.FontStyle style = System.Drawing.FontStyle.Regular) {
        try {
            var font = new Font(familyName, emSize, style);
            // Verify the font is valid by checking if the family name matches or is a substitute
            if (font.Name != familyName && font.OriginalFontName != familyName) {
                // Font was substituted, but we still return it as it's valid
            }
            return font;
        } catch {
            // Fallback to system default font
            return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
        }
    }
    /// <summary>
    /// Executes style axis as part of series hover snapshot processing.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="labelText">Input value for label text.</param>
    /// <param name="color">Input value for color.</param>
    private static void StyleAxis(ScottPlot.IYAxis axis, string labelText, ScottPlot.Color color) {
        axis.Label.Text = labelText;
        axis.Label.ForeColor = color;
        axis.TickLabelStyle.ForeColor = color;
        axis.FrameLineStyle.Color = color;
        axis.MajorTickStyle.Color = color;
        axis.MinorTickStyle.Color = color;
    }

    private (DateTime start, DateTime end) GetConfiguredPeriodRange() {
        var endTime = _isStaticMode && _config.CustomEndTime.HasValue
            ? ToLocalChartTime(_config.CustomEndTime.Value)
            : DateTime.Now;

        var startTime = _config.Period switch {
            ChartPeriod.Last15Minutes => endTime.AddMinutes(-15),
            ChartPeriod.LastHour => endTime.AddHours(-1),
            ChartPeriod.Last6Hours => endTime.AddHours(-6),
            ChartPeriod.Last24Hours => endTime.AddHours(-24),
            ChartPeriod.Last7Days => endTime.AddDays(-7),
            ChartPeriod.Last30Days => endTime.AddDays(-30),
            ChartPeriod.Custom => _config.CustomStartTime.HasValue
                ? ToLocalChartTime(_config.CustomStartTime.Value)
                : (_config.CustomPeriodDuration.HasValue
                    ? endTime.Add(-_config.CustomPeriodDuration.Value)
                    : endTime.AddHours(-24)),
            _ => endTime.AddHours(-24)
        };

        return (startTime, endTime);
    }

    /// <summary>
    /// Executes to local chart time as part of series hover snapshot processing.
    /// </summary>
    /// <param name="value">Input value for value.</param>
    /// <returns>The result of the operation.</returns>
    private static DateTime ToLocalChartTime(DateTime value) => value.Kind switch {
        DateTimeKind.Utc => value.ToLocalTime(),
        DateTimeKind.Local => value,
        _ => DateTime.SpecifyKind(value, DateTimeKind.Local)
    };

    private void ApplyYAxisVisibility(
        IReadOnlyDictionary<MetricType, ScottPlot.IYAxis> axisMap,
        Dictionary<MetricType, (string LabelText, ScottPlot.Color Color)> axisStyleMap,
        HashSet<MetricType> metricsWithData) {
        if (_plot == null) {
            return;
        }

        SetAxisNeutralState(_plot.Plot.Axes.Left);
        SetAxisNeutralState(_plot.Plot.Axes.Right);

        foreach (var axis in _extraAxes) {
            SetAxisNeutralState(axis);
        }

        foreach (var (metric, axis) in axisMap) {
            if (!metricsWithData.Contains(metric)) {
                continue;
            }

            if (axisStyleMap.TryGetValue(metric, out var axisStyle)) {
                StyleAxis(axis, axisStyle.LabelText, axisStyle.Color);
                SetAxisVisibility(axis, true);
            }
        }
    }

    /// <summary>
    /// Sets the axis neutral state for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    private static void SetAxisNeutralState(ScottPlot.IYAxis axis) {
        SetAxisVisibility(axis, true);

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var neutralColor = ScottPlot.Color.FromColor(isLight ? LightThemePrimaryColor : Color.White);
        axis.Label.Text = string.Empty;
        axis.Label.ForeColor = neutralColor;
        axis.TickLabelStyle.ForeColor = neutralColor;
        axis.FrameLineStyle.Color = neutralColor;
        axis.MajorTickStyle.Color = neutralColor;
        axis.MinorTickStyle.Color = neutralColor;
    }

    /// <summary>
    /// Sets the axis visibility for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="isVisible">Input value for is visible.</param>
    private static void SetAxisVisibility(object axis, bool isVisible) {
        var property = axis.GetType().GetProperty("IsVisible");
        if (property?.CanWrite == true && property.PropertyType == typeof(bool)) {
            property.SetValue(axis, isVisible);
        }
    }

    /// <summary>
    /// Applies the theme colors for series hover snapshot.
    /// </summary>
    private void ApplyThemeColors() {
        if (_plot == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        if (isLight) {
            _plot.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.Axes.Color(ScottPlot.Color.FromColor(LightThemePrimaryColor));
            _plot.Plot.Legend.BackgroundColor = ScottPlot.Color.FromColor(Color.FromArgb(200, 255, 255, 255));
            _plot.Plot.Legend.FontColor = ScottPlot.Color.FromColor(LightThemePrimaryColor);
            _plot.Plot.Legend.OutlineColor = ScottPlot.Color.FromColor(Color.LightGray);
        } else {
            _plot.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(Color.FromArgb(35, 47, 52));
            _plot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(Color.FromArgb(35, 47, 52));
            _plot.Plot.Axes.Color(ScottPlot.Color.FromColor(Color.White));
            _plot.Plot.Legend.BackgroundColor = ScottPlot.Color.FromColor(Color.FromArgb(170, 46, 61, 66));
            _plot.Plot.Legend.FontColor = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.Legend.OutlineColor = ScottPlot.Color.FromColor(Color.FromArgb(80, 102, 110));
        }

        // Axes.Color() resets ALL axes to the same color,
        // so re-apply per-metric colors when multi-axis mode is active.
        ReapplyAxisColors();
    }

    /// <summary>
    /// Applies the tile colors for series hover snapshot.
    /// </summary>
    private void ApplyTileColors() {
        if (_titleLabel == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var tileBg = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        var fg = isLight ? LightThemePrimaryColor : Color.White;

        if (BackColor != tileBg) {
            BackColor = tileBg;
        }

        _titleLabel.ForeColor = fg;
        if (_aggregationInfoTextBox != null) {
            _aggregationInfoTextBox.BackColor = tileBg;
            _aggregationInfoTextBox.ForeColor = fg;
            _aggregationInfoTextBox.Font = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular);
        }
        if (_hoverInfoTextBox != null) {
            _hoverInfoTextBox.BackColor = tileBg;
            _hoverInfoTextBox.ForeColor = fg;
            _hoverInfoTextBox.Font = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular);
        }
        if (_topPanel != null && _topPanel.BackColor != tileBg) {
            _topPanel.BackColor = tileBg;
        }
        _periodSelector?.ApplyTheme();
        _periodSelector?.BorderColorOverride = isLight ? Color.FromArgb(150, 150, 150) : null;

        if (_autoPeriodPanel != null && _autoPeriodPanel.BackColor != tileBg) {
            _autoPeriodPanel.BackColor = tileBg;
        }

        if (_autoRangeSpacer != null && _autoRangeSpacer.BackColor != tileBg) {
            _autoRangeSpacer.BackColor = tileBg;
        }

        if (_staticRangePanel != null && _staticRangePanel.BackColor != tileBg) {
            _staticRangePanel.BackColor = tileBg;
        }

        UpdateRangeShiftButtonsAppearance();

        _staticStartPicker?.ApplyTheme();
        _staticEndPicker?.ApplyTheme();
        _staticStartPicker?.BorderColorOverride = isLight ? Color.FromArgb(150, 150, 150) : null;
        _staticEndPicker?.BorderColorOverride = isLight ? Color.FromArgb(150, 150, 150) : null;

        if (_inspectorActive && _lastHoverAnchorX.HasValue) {
            UpdateHoverInfo(_lastHoverAnchorX.Value);
        }
    }

    /// <summary>
    /// Executes draw tile border as part of series hover snapshot processing.
    /// </summary>
    /// <param name="graphics">Input value for graphics.</param>
    private void DrawTileBorder(Graphics graphics) {
        if (ClientSize.Width <= 1 || ClientSize.Height <= 1) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var borderColor = isLight ? LightThemeBorderColor : DarkThemeBorderColor;
        ControlPaint.DrawBorder(graphics, ClientRectangle, borderColor, ButtonBorderStyle.Solid);
    }

    /// <summary>
    /// Applies the plot context menu theme for series hover snapshot.
    /// </summary>
    private void ApplyPlotContextMenuTheme() {
        if (_plotContextMenu == null) {
            return;
        }

        ApplyPlotContextMenuTheme(_plotContextMenu);
    }

    /// <summary>
    /// Applies the plot context menu theme for series hover snapshot.
    /// </summary>
    /// <param name="contextMenu">Input value for context menu.</param>
    private void ApplyPlotContextMenuTheme(ContextMenuStrip contextMenu) {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var menuBackground = isLight ? Color.White : Color.FromArgb(38, 52, 57);
        var menuText = isLight ? Color.FromArgb(66, 66, 66) : Color.FromArgb(240, 240, 240);
        var menuIconColor = isLight ? Color.FromArgb(66, 66, 66) : Color.FromArgb(240, 240, 240);

        _contextMenuRenderer.UpdateTheme();

        contextMenu.RenderMode = ToolStripRenderMode.Professional;
        contextMenu.Renderer = _contextMenuRenderer;
        contextMenu.ShowImageMargin = true;
        contextMenu.BackColor = menuBackground;
        contextMenu.ForeColor = menuText;
        contextMenu.Font = CreateSafeFont("Segoe UI", PlotContextMenuFontSize, System.Drawing.FontStyle.Regular);
        contextMenu.ImageScalingSize = new Size(MenuIconSize, MenuIconSize);

        ApplyContextMenuItemColors(contextMenu.Items, menuBackground, menuText);
        ApplyContextMenuItemFont(contextMenu.Items, contextMenu.Font);
        UpdateContextMenuIcons(contextMenu.Items, menuIconColor);
    }

    /// <summary>
    /// Creates the plot context menu for series hover snapshot.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private ContextMenuStrip CreatePlotContextMenu() {
        var contextMenu = new ContextMenuStrip {
            ShowImageMargin = true,
            ImageScalingSize = new Size(MenuIconSize, MenuIconSize),
            Cursor = Cursors.Hand
        };

        contextMenu.Opening += (_, _) => {
            RebuildPlotContextMenu(contextMenu);
            ApplyPlotContextMenuTheme(contextMenu);
        };

        RebuildPlotContextMenu(contextMenu);
        InteractiveCursorStyler.Apply(contextMenu.Items);
        return contextMenu;
    }

    /// <summary>
    /// Rebuilds the plot context menu for series hover snapshot.
    /// </summary>
    /// <param name="contextMenu">Input value for context menu.</param>
    private void RebuildPlotContextMenu(ContextMenuStrip contextMenu) {
        contextMenu.Items.Clear();

        contextMenu.Items.Add(CreatePlotMenuItem("Edit Dashboard…", MaterialIcons.CommonEdit, (_, _) => EditDashboardRequested?.Invoke(this)));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(CreatePlotMenuItem("Edit Tile…", MaterialIcons.CommonEdit, HandleEditTileClick));

        if (_availableLinkGroups > 1) {
            var linkGroupItem = CreatePlotMenuItem("Link group", MaterialIcons.ToolbarChartsGroup, (_, _) => { });
            foreach (var group in ChartLinkGroupInfo.GetAvailable(_availableLinkGroups)) {
                var periodShortName = _linkGroupPeriodShortNames.GetValueOrDefault(group, string.Empty);
                var item = CreateToggleMenuItem(group.GetDisplayName(periodShortName), GetLinkGroupIcon(group), _config.LinkGroup == group, (_, _) => {
                    if (_config.LinkGroup == group) {
                        return;
                    }

                    _config.LinkGroup = group;
                    ViewSettingsChanged?.Invoke(this);
                    LinkGroupChanged?.Invoke(this);
                });
                linkGroupItem.DropDownItems.Add(item);
            }
            contextMenu.Items.Add(linkGroupItem);
            contextMenu.Items.Add(new ToolStripSeparator());
        }

        var isExporting = ExcelExportStateService.IsExporting;
        var copyItem = CreatePlotMenuItem("Copy to Clipboard", MaterialIcons.PlotMenuCopyToClipboard, HandleCopyImageClick);
        copyItem.Enabled = !isExporting;
        contextMenu.Items.Add(copyItem);
        var pngItem = CreatePlotMenuItem("Save as .png", MaterialIcons.PlotMenuImage, HandleSaveImageClick);
        pngItem.Enabled = !isExporting;
        contextMenu.Items.Add(pngItem);
        var xlsxItem = CreatePlotMenuItem("Save as .xlsx", MaterialIcons.PlotMenuTable, HandleSaveTableClick);
        xlsxItem.Enabled = !isExporting;
        contextMenu.Items.Add(xlsxItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        AddToggleMenuItems(contextMenu);
        InteractiveCursorStyler.Apply(contextMenu.Items);
    }

    /// <summary>
    /// Adds the toggle menu items for series hover snapshot.
    /// </summary>
    /// <param name="contextMenu">Input value for context menu.</param>
    private void AddToggleMenuItems(ContextMenuStrip contextMenu) {
        var legendItem = CreateToggleMenuItem("Legend", MaterialIcons.PlotMenuLegendToggle, _config.ShowLegend, (_, _) => {
            _config.ShowLegend = !_config.ShowLegend;
            ApplyViewSettings();
        });

        var gridItem = CreateToggleMenuItem("Grid", MaterialIcons.PlotMenuGrid4x4, _config.ShowGrid, (_, _) => {
            _config.ShowGrid = !_config.ShowGrid;
            ApplyViewSettings();
        });

        var inspectorItem = CreateToggleMenuItem("Inspector", MaterialIcons.ChartInspector, _config.ShowInspector, (_, _) => {
            SetInspectorEnabled(!_config.ShowInspector);
        });

        contextMenu.Items.Add(legendItem);
        contextMenu.Items.Add(gridItem);
        contextMenu.Items.Add(inspectorItem);

        if (_config.MetricAggregations.Count == 0) {
            return;
        }

        if (_config.MetricAggregations.Count == 1) {
            var aggregation = _config.MetricAggregations[0];
            contextMenu.Items.Add(CreateToggleMenuItem("Smooth", MaterialIcons.PlotMenuConversionPath, aggregation.Smooth, (_, _) => {
                aggregation.Smooth = !aggregation.Smooth;
                ApplyViewSettings();
            }));
        } else {
            var smoothItem = new ToolStripMenuItem("Smooth") { Tag = MaterialIcons.PlotMenuConversionPath };
            foreach (var aggregation in _config.MetricAggregations) {
                smoothItem.DropDownItems.Add(CreateToggleMenuItem(GetAggregationDisplayName(aggregation), MaterialIcons.PlotMenuConversionPath, aggregation.Smooth, (_, _) => {
                    aggregation.Smooth = !aggregation.Smooth;
                    ApplyViewSettings();
                }));
            }
            contextMenu.Items.Add(smoothItem);
        }

        contextMenu.Items.Add(CreateToggleMenuItem("Markers", MaterialIcons.PlotMenuStat0, _config.MetricAggregations.Any(x => x.ShowMarkers), (_, _) => {
            var enableMarkers = !_config.MetricAggregations.All(x => x.ShowMarkers);
            foreach (var aggregation in _config.MetricAggregations) {
                aggregation.ShowMarkers = enableMarkers;
            }
            ApplyViewSettings();
        }));
    }

    /// <summary>
    /// Gets the aggregation display name for series hover snapshot.
    /// </summary>
    /// <param name="aggregation">Input value for aggregation.</param>
    /// <returns>The resulting string value.</returns>
    private static string GetAggregationDisplayName(MetricAggregation aggregation) {
        if (!string.IsNullOrWhiteSpace(aggregation.Label)) {
            return aggregation.Label;
        }

        return $"{aggregation.Metric.GetDisplayName()} ({aggregation.Function})";
    }

    /// <summary>
    /// Gets the link group icon for series hover snapshot.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <returns>The resulting string value.</returns>
    private static string GetLinkGroupIcon(ChartLinkGroup group) => group switch {
        ChartLinkGroup.Alpha => MaterialIcons.LinkGroupAlpha,
        ChartLinkGroup.Bravo => MaterialIcons.LinkGroupBravo,
        ChartLinkGroup.Charlie => MaterialIcons.LinkGroupCharlie,
        ChartLinkGroup.Delta => MaterialIcons.LinkGroupDelta,
        ChartLinkGroup.Echo => MaterialIcons.LinkGroupEcho,
        ChartLinkGroup.Foxtrot => MaterialIcons.LinkGroupFoxtrot,
        _ => MaterialIcons.LinkGroupAlpha
    };

    /// <summary>
    /// Applies the view settings for series hover snapshot.
    /// </summary>
    private void ApplyViewSettings() {
        RefreshData();
        ViewSettingsChanged?.Invoke(this);
    }

    /// <summary>
    /// Creates the toggle menu item for series hover snapshot.
    /// </summary>
    /// <param name="text">Input value for text.</param>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="isChecked">Input value for is checked.</param>
    /// <param name="onClick">Input value for on click.</param>
    /// <returns>The result of the operation.</returns>
    private static ToolStripMenuItem CreateToggleMenuItem(string text, string iconName, bool isChecked, EventHandler onClick) {
        var item = CreatePlotMenuItem(text, iconName, onClick);
        item.Checked = isChecked;
        return item;
    }

    /// <summary>
    /// Creates the plot menu item for series hover snapshot.
    /// </summary>
    /// <param name="text">Input value for text.</param>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="onClick">Input value for on click.</param>
    /// <returns>The result of the operation.</returns>
    private static ToolStripMenuItem CreatePlotMenuItem(string text, string iconName, EventHandler onClick) {
        var iconColor = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(66, 66, 66)
            : Color.FromArgb(240, 240, 240);

        var item = new ToolStripMenuItem(text) {
            // Keep classic image+text mode for platforms where image margin works,
            // and also include an ASCII marker in text so item origin is always visible.
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize, IconRenderPreset.DarkOutlined),
            ImageScaling = ToolStripItemImageScaling.None,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = iconName
        };

        item.Click += onClick;
        return item;
    }

    /// <summary>
    /// Updates the context menu icons for series hover snapshot.
    /// </summary>
    /// <param name="items">Input value for items.</param>
    /// <param name="iconColor">Input value for icon color.</param>
    private static void UpdateContextMenuIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is not ToolStripMenuItem menuItem) {
                continue;
            }

            if (menuItem.Tag is string iconName && !string.IsNullOrWhiteSpace(iconName)) {
                menuItem.Image?.Dispose();
                menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize, IconRenderPreset.DarkOutlined);
            }

            if (menuItem.DropDownItems.Count > 0) {
                UpdateContextMenuIcons(menuItem.DropDownItems, iconColor);
            }
        }
    }

    /// <summary>
    /// Handles the edit tile click for series hover snapshot.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void HandleEditTileClick(object? sender, EventArgs e) {
        EditRequested?.Invoke(this);
    }

    /// <summary>
    /// Handles the save image click for series hover snapshot.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void HandleSaveImageClick(object? sender, EventArgs e) {
        if (_plot == null) {
            return;
        }

        using var dialog = new SaveFileDialog {
            Filter = "PNG Image|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = ExportFileNameSanitizer.SanitizeStem(_config.Title, "chart") + ".png"
        };

        if (dialog.ShowDialog() != DialogResult.OK) {
            return;
        }

        SavePlotPng(dialog.FileName, PngExportScaleFactor);
    }

    /// <summary>
    /// Handles the save table click for series hover snapshot.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void HandleSaveTableClick(object? sender, EventArgs e) {
        if (ExcelExportStateService.IsExporting) {
            return;
        }

        var aggregationInterval = ResolveAggregationInterval();
        var aggregatedData = GetAggregatedExportData(aggregationInterval);
        var rawData = GetRawExportData();

        using var dialog = new SaveFileDialog {
            Filter = "Excel Workbook|*.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            FileName = ExportFileNameSanitizer.SanitizeStem(_config.Title, "table") + ".xlsx"
        };

        if (dialog.ShowDialog() != DialogResult.OK) {
            return;
        }

        if (!ExcelExportStateService.TryBeginExport()) {
            return;
        }

        var filePath = dialog.FileName;
        var metricAggregations = _config.MetricAggregations;
        var exportService = _chartTableExportService;
        var syncContext = SynchronizationContext.Current;

        Task.Run(() => {
            try {
                ChartTableExportService.Export(filePath, metricAggregations, aggregatedData, rawData, ExcelExportStateService.ReportProgress);
            } catch (IOException ioEx) {
                syncContext?.Post(_ => ThemedMessageBox.Show(
                    this,
                    $"Cannot save the table file because it is being used by another process. {ioEx.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning), null);
            } catch (Exception ex) {
                syncContext?.Post(_ => ThemedMessageBox.Show(
                    this,
                    $"Failed to export table. {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error), null);
            } finally {
                Thread.Sleep(500);
                ExcelExportStateService.EndExport();
            }
        });
    }

    /// <summary>
    /// Gets the aggregated export data for series hover snapshot.
    /// </summary>
    /// <param name="aggregationInterval">Input value for aggregation interval.</param>
    /// <returns>The result of the operation.</returns>
    private List<DataStorage.Models.ObservingData> GetAggregatedExportData(TimeSpan? aggregationInterval) {
        var exportRows = new Dictionary<DateTime, DataStorage.Models.ObservingData>();

        var dataCache = new Dictionary<DataStorage.Models.AggregationFunction, List<DataStorage.Models.ObservingData>>();

        foreach (var aggregation in _config.MetricAggregations) {
            if (!dataCache.TryGetValue(aggregation.Function, out var rows)) {
                rows = _dataService.GetChartData(
                    _config.Period,
                    _config.CustomStartTime,
                    _isStaticMode ? _config.CustomEndTime : null,
                    _config.CustomPeriodDuration,
                    aggregationInterval,
                    aggregation.Function);
                dataCache[aggregation.Function] = rows;
            }

            foreach (var row in rows) {
                if (!exportRows.TryGetValue(row.Timestamp, out var mergedRow)) {
                    mergedRow = new DataStorage.Models.ObservingData { Timestamp = row.Timestamp };
                    exportRows[row.Timestamp] = mergedRow;
                }

                SetMetricValue(mergedRow, aggregation.Metric, aggregation.Metric.GetValue(row));
            }
        }

        return [.. exportRows.Values.OrderBy(x => x.Timestamp)];
    }

    /// <summary>
    /// Gets the raw export data for series hover snapshot.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private List<DataStorage.Models.ObservingData> GetRawExportData() {
        return _dataService.GetChartData(
            _config.Period,
            _config.CustomStartTime,
            _isStaticMode ? _config.CustomEndTime : null,
            _config.CustomPeriodDuration,
            null,
            null);
    }

    /// <summary>
    /// Sets the metric value for series hover snapshot.
    /// </summary>
    /// <param name="target">Input value for target.</param>
    /// <param name="metric">Input value for metric.</param>
    /// <param name="value">Input value for value.</param>
    private static void SetMetricValue(DataStorage.Models.ObservingData target, MetricType metric, double? value) {
        switch (metric) {
            case MetricType.Temperature:
                target.Temperature = value;
                break;
            case MetricType.Humidity:
                target.Humidity = value;
                break;
            case MetricType.Pressure:
                target.Pressure = value;
                break;
            case MetricType.DewPoint:
                target.DewPoint = value;
                break;
            case MetricType.CloudCover:
                target.CloudCover = value;
                break;
            case MetricType.SkyTemperature:
                target.SkyTemperature = value;
                break;
            case MetricType.SkyBrightness:
                target.SkyBrightness = value;
                break;
            case MetricType.SkyQualitySQM:
                target.SkyQuality = value;
                break;
            case MetricType.RainRate:
                target.RainRate = value;
                break;
            case MetricType.WindSpeed:
                target.WindSpeed = value;
                break;
            case MetricType.WindGust:
                target.WindGust = value;
                break;
            case MetricType.WindDirection:
                target.WindDirection = value;
                break;
            case MetricType.StarFwhm:
                target.StarFwhm = value;
                break;
            case MetricType.IsSafe:
                target.SafePercentage = value;
                break;
        }
    }

    /// <summary>
    /// Handles the copy image click for series hover snapshot.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void HandleCopyImageClick(object? sender, EventArgs e) {
        using var bmp = CapturePlotBitmap();
        if (bmp != null) {
            Clipboard.SetImage((Bitmap)bmp.Clone());
        }
    }

    /// <summary>
    /// Handles the autoscale click for series hover snapshot.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void HandleAutoscaleClick(object? sender, EventArgs e) {
        if (_plot == null) {
            return;
        }

        ExitStaticMode();
        _plot.Plot.Axes.AutoScale();
        RememberCurrentXAxisLimits();
        _plot.Refresh();
    }

    /// <summary>
    /// Handles the open in window click for series hover snapshot.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void HandleOpenInWindowClick(object? sender, EventArgs e) {
        if (_plot == null) {
            return;
        }

        using var bmp = CapturePlotBitmap();
        if (bmp == null) {
            return;
        }

        var previewForm = new Form {
            Text = string.IsNullOrWhiteSpace(_config.Title) ? "Chart" : _config.Title,
            StartPosition = FormStartPosition.CenterParent,
            Width = Math.Max(640, _plot.Width + 40),
            Height = Math.Max(420, _plot.Height + 80)
        };

        var pictureBox = new PictureBox {
            Dock = DockStyle.Fill,
            Image = (Bitmap)bmp.Clone(),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        previewForm.FormClosed += (_, _) => pictureBox.Image?.Dispose();
        previewForm.Controls.Add(pictureBox);
        previewForm.Show(this);
    }

    /// <summary>
    /// Saves the plot as png for series hover snapshot.
    /// </summary>
    /// <param name="filePath">Input value for file path.</param>
    /// <param name="scaleFactor">Input value for scale factor.</param>
    private void SavePlotPng(string filePath, int scaleFactor = 1) {
        if (_plot == null || _plot.Width <= 0 || _plot.Height <= 0) {
            return;
        }

        var safeScaleFactor = Math.Max(1, scaleFactor);
        var exportWidth = _plot.Width * safeScaleFactor;
        var exportHeight = _plot.Height * safeScaleFactor;
        _plot.Plot.SavePng(filePath, exportWidth, exportHeight);
    }

    /// <summary>
    /// Executes capture plot bitmap as part of series hover snapshot processing.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private Bitmap? CapturePlotBitmap() {
        if (_plot == null || _plot.Width <= 0 || _plot.Height <= 0) {
            return null;
        }

        var bmp = new Bitmap(_plot.Width, _plot.Height);
        _plot.DrawToBitmap(bmp, new Rectangle(Point.Empty, _plot.Size));
        return bmp;
    }

    /// <summary>
    /// Executes plot mouse up as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Plot_MouseUp(object? sender, MouseEventArgs e) {
        DetectXAxisInteraction();

        if (e.Button != MouseButtons.Right || _plot == null || _plotContextMenu == null) {
            return;
        }

        ApplyPlotContextMenuTheme();
        _plotContextMenu.Show(_plot, e.Location);
    }

    /// <summary>
    /// Executes tile mouse up as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Tile_MouseUp(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Right || _plotContextMenu == null || sender is not Control sourceControl) {
            return;
        }

        ApplyPlotContextMenuTheme();
        _plotContextMenu.Show(sourceControl, e.Location);
    }

    /// <summary>
    /// Executes attach tile context menu handlers as part of series hover snapshot processing.
    /// </summary>
    /// <param name="root">Input value for root.</param>
    private void AttachTileContextMenuHandlers(Control root) {
        if (root is FormsPlot) {
            return;
        }

        root.MouseUp -= Tile_MouseUp;
        root.MouseUp += Tile_MouseUp;

        foreach (Control child in root.Controls) {
            AttachTileContextMenuHandlers(child);
        }
    }

    /// <summary>
    /// Executes detach tile context menu handlers as part of series hover snapshot processing.
    /// </summary>
    /// <param name="root">Input value for root.</param>
    private void DetachTileContextMenuHandlers(Control root) {
        if (root is FormsPlot) {
            return;
        }

        root.MouseUp -= Tile_MouseUp;

        foreach (Control child in root.Controls) {
            DetachTileContextMenuHandlers(child);
        }
    }

    /// <summary>
    /// Executes disable scott plot interaction overlays as part of series hover snapshot processing.
    /// </summary>
    private void DisableScottPlotInteractionOverlays() {
        if (_plot == null) {
            return;
        }

        // Disable benchmark toggle action so FPS overlay cannot be enabled.
        _plot.UserInputProcessor.RemoveAll<ScottPlot.Interactivity.UserActionResponses.DoubleClickBenchmark>();
    }

    /// <summary>
    /// Applies the context menu item colors for series hover snapshot.
    /// </summary>
    /// <param name="items">Input value for items.</param>
    /// <param name="backColor">Input value for back color.</param>
    /// <param name="foreColor">Input value for fore color.</param>
    private static void ApplyContextMenuItemColors(ToolStripItemCollection items, Color backColor, Color foreColor) {
        foreach (ToolStripItem item in items) {
            item.BackColor = backColor;
            item.ForeColor = foreColor;

            if (item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0) {
                ApplyContextMenuItemColors(menuItem.DropDownItems, backColor, foreColor);
            }
        }
    }

    /// <summary>
    /// Applies the context menu item font for series hover snapshot.
    /// </summary>
    /// <param name="items">Input value for items.</param>
    /// <param name="menuFont">Input value for menu font.</param>
    private static void ApplyContextMenuItemFont(ToolStripItemCollection items, Font menuFont) {
        foreach (ToolStripItem item in items) {
            item.Font = menuFont;

            if (item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0) {
                ApplyContextMenuItemFont(menuItem.DropDownItems, menuFont);
            }
        }
    }

    /// <summary>
    /// Executes clear extra axes as part of series hover snapshot processing.
    /// </summary>
    private void ClearExtraAxes() {
        if (_plot == null) {
            return;
        }

        foreach (var axis in _extraAxes) {
            _plot.Plot.Axes.Remove(axis);
        }
        _extraAxes.Clear();
    }

    /// <summary>
    /// Initializes series hover snapshot state and required resources.
    /// </summary>
    private void InitializeUI() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var tileBg = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        var fg = isLight ? LightThemePrimaryColor : Color.White;

        _topPanel = new Panel {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(5),
            BackColor = tileBg
        };

        _titleLabel = new Label {
            Text = _config.Title,
            Dock = DockStyle.Fill,
            Font = CreateSafeFont("Segoe UI", 11, System.Drawing.FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0),
            ForeColor = fg
        };

        _periodSelector = new ThemedComboBox {
            Width = 120,
            Height = HeaderControlHeight,
            Margin = new Padding(HeaderButtonSpacing, HeaderButtonY, HeaderButtonSpacing, HeaderButtonY)
        };
        LoadPeriodPresets();
        SetSelectedPeriodPreset();
        _periodSelector.SelectedIndexChanged += (s, e) => {
            if (!_suppressPeriodChange) {
                UpdatePeriodFromSelection();
            }
        };

        _autoShiftLeftButton = CreateRangeShiftButton(true);
        _autoShiftLeftButton.Click += (s, e) => ShiftCurrentRange(-1);
        _autoShiftLeftButton.MouseUp += (s, e) => _plot?.Focus();
        _autoShiftRightButton = CreateRangeShiftButton(false);
        _autoShiftRightButton.Click += (s, e) => ShiftCurrentRange(1);
        _autoShiftRightButton.MouseUp += (s, e) => _plot?.Focus();

        _autoPeriodPanel = new FlowLayoutPanel {
            Dock = DockStyle.Right,
            Width = AutoRangePanelWidth,
            AutoSize = false,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(AutoRangePanelLeftPadding, 0, 0, 0),
            BackColor = tileBg
        };
        _autoRangeSpacer = new Panel {
            Width = AutoRangeSpacerWidth,
            Height = HeaderControlHeight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = tileBg
        };
        _autoPeriodPanel.Controls.Add(_autoRangeSpacer);
        _autoPeriodPanel.Controls.Add(_periodSelector);
        _autoPeriodPanel.Controls.Add(_autoShiftLeftButton);
        _autoPeriodPanel.Controls.Add(_autoShiftRightButton);
        LayoutAutoPeriodPanel();
        ChartPeriodPresetStore.PresetsChanged += HandlePresetsChanged;
        MetricAxisRuleStore.RulesChanged += HandleAxisRulesChanged;
        MetricDisplaySettingsStore.SettingsChanged += HandleAxisRulesChanged;

        _topPanel.Controls.Add(_titleLabel);

        _staticRangePanel = new FlowLayoutPanel {
            Dock = DockStyle.Right,
            Width = StaticRangePanelWidth,
            AutoSize = false,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Visible = false,
            BackColor = tileBg
        };

        _staticStartPicker = CreateStaticDatePicker();
        _staticEndPicker = CreateStaticDatePicker();
        _staticShiftLeftButton = CreateRangeShiftButton(true);
        _staticShiftLeftButton.Click += (s, e) => ShiftCurrentRange(-1);
        _staticShiftLeftButton.MouseUp += (s, e) => _plot?.Focus();
        _staticShiftRightButton = CreateRangeShiftButton(false);
        _staticShiftRightButton.Click += (s, e) => ShiftCurrentRange(1);
        _staticShiftRightButton.MouseUp += (s, e) => _plot?.Focus();
        _staticRangePanel.Controls.Add(_staticStartPicker);
        _staticRangePanel.Controls.Add(_staticEndPicker);
        _staticRangePanel.Controls.Add(_staticShiftLeftButton);
        _staticRangePanel.Controls.Add(_staticShiftRightButton);
        LayoutStaticRangePanel();

        // Auto / Static mode switcher (rightmost in header).
        _autoModeButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 1,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Size = new Size(HeaderControlButtonSize, HeaderControlButtonSize),
            Checked = true,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _autoModeButton.CheckedChanged += (s, e) => {
            if (_autoModeButton!.Checked && _isStaticMode) {
                ExitStaticMode();
            }
            UpdateModeSwitchAppearance();
            UpdateAggregationInfoLabel(ResolveAggregationInterval());
        };

        _staticModeButton = new RadioButton {
            Text = string.Empty,
            Appearance = Appearance.Button,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 1,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Size = new Size(HeaderControlButtonSize, HeaderControlButtonSize),
            Checked = false,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _staticModeButton.CheckedChanged += (s, e) => {
            if (_staticModeButton!.Checked) {
                if (_isStaticMode) {
                    if (!_staticModePaused) {
                        // Already in static — reset countdown timer
                        _lastChartInteractionUtc = DateTime.UtcNow;
                        UpdateCountdownLabel();
                    }
                } else {
                    // Enter static mode: freeze current auto period range
                    var (start, end) = GetConfiguredPeriodRange();
                    EnterStaticMode(start, end, raiseEvents: true);
                }
            }
            UpdateModeSwitchAppearance();
            UpdateAggregationInfoLabel(ResolveAggregationInterval());
        };

        _pauseModeButton = new CheckBox {
            Text = string.Empty,
            Appearance = Appearance.Button,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 1,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Size = new Size(HeaderControlButtonSize, HeaderControlButtonSize),
            Checked = _staticModePaused,
            Visible = false,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _pauseModeButton.CheckedChanged += (s, e) => {
            if (_suppressPauseChange) {
                return;
            }

            SetStaticPaused(_pauseModeButton.Checked);
        };

        _inspectorButton = new CheckBox {
            Text = string.Empty,
            Appearance = Appearance.Button,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 1,
                CheckedBackColor = Color.Transparent,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            AutoSize = false,
            Dock = DockStyle.Fill,
            Checked = _config.ShowInspector,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };
        _inspectorButton.CheckedChanged += (s, e) => {
            SetInspectorEnabled(_inspectorButton.Checked);
        };

        _inspectorSegmentPanel = new Panel {
            Size = new Size(HeaderControlButtonSize, HeaderControlButtonSize),
            Location = new Point(HeaderButtonSpacing, HeaderButtonY),
            Padding = Padding.Empty
        };
        _inspectorSegmentPanel.Controls.Add(_inspectorButton);

        _inspectorHostPanel = new Panel {
            Dock = DockStyle.Right,
            Width = HeaderControlButtonSize + HeaderButtonSpacing * 2,
            BackColor = tileBg
        };
        _inspectorHostPanel.Controls.Add(_inspectorSegmentPanel);

        _modeSegmentPanel = new Panel {
            Size = new Size(HeaderControlButtonSize * 2 + HeaderButtonSpacing, HeaderControlButtonSize),
            Padding = Padding.Empty
        };
        _autoModeButton.Location = new Point(0, 0);
        _staticModeButton.Location = new Point(HeaderControlButtonSize + HeaderButtonSpacing, 0);
        _modeSegmentPanel.Controls.Add(_autoModeButton);
        _modeSegmentPanel.Controls.Add(_staticModeButton);

        _countdownLabel = new Label {
            Text = "",
            AutoSize = false,
            Size = new Size(36, HeaderControlHeight),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = CreateSafeFont("Segoe UI", 9f, System.Drawing.FontStyle.Regular),
            ForeColor = fg,
            Visible = false
        };

        _modeSwitchContainer = new Panel {
            Dock = DockStyle.Right,
            Width = ModeSwitchContainerWidth,
            BackColor = tileBg
        };

        // Left reserved area: pause + countdown. Right fixed area: auto/static.
        LayoutModeControlBlock();
        _modeSwitchContainer.Controls.Add(_pauseModeButton);
        _modeSwitchContainer.Controls.Add(_countdownLabel);
        _modeSwitchContainer.Controls.Add(_modeSegmentPanel);

        _topPanel.Controls.Add(_autoPeriodPanel);
        _topPanel.Controls.Add(_staticRangePanel);
        _topPanel.Controls.Add(_inspectorHostPanel);
        _topPanel.Controls.Add(_modeSwitchContainer);
        UpdateModeSwitchAppearance();
        UpdateAggregationInfoLabel(ResolveAggregationInterval());

        // Guard against MaterialSkinManager overwriting our colors.
        // MSM walks the control tree and sets BackColor directly;
        // this handler fires immediately and corrects it.
        _topPanel.BackColorChanged += (s, e) => {
            var sm = MaterialSkinManager.Instance;
            var light = sm.Theme == MaterialSkinManager.Themes.LIGHT;
            var expected = light ? Color.White : Color.FromArgb(35, 47, 52);
            if (_topPanel.BackColor != expected) {
                _topPanel.BackColor = expected;
            }
        };
        _modeSwitchContainer!.BackColorChanged += (s, e) => {
            var sm = MaterialSkinManager.Instance;
            var light = sm.Theme == MaterialSkinManager.Themes.LIGHT;
            var expected = light ? Color.White : Color.FromArgb(35, 47, 52);
            if (_modeSwitchContainer.BackColor != expected) {
                _modeSwitchContainer.BackColor = expected;
            }
        };
        _topPanel.SizeChanged += (s, e) => {
            LayoutAutoPeriodPanel();
            LayoutStaticRangePanel();
        };
        _staticStartPicker?.ValueChanged += StaticPicker_ValueChanged;
        _staticEndPicker?.ValueChanged += StaticPicker_ValueChanged;

        RestorePersistedStaticState();

        _staticModeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _staticModeTimer.Tick += StaticModeTimer_Tick;
        _staticModeTimer.Start();


        var bottomInfoPanel = new Panel {
            Dock = DockStyle.Bottom,
            Height = 18,
            BackColor = tileBg,
            Padding = new Padding(4, 0, 4, 2)
        };

        _aggregationInfoTextBox = new RichTextBox {
            Dock = DockStyle.Right,
            Width = 260,
            Margin = Padding.Empty,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Multiline = false,
            ScrollBars = RichTextBoxScrollBars.None,
            WordWrap = false,
            DetectUrls = false,
            TabStop = false,
            ShortcutsEnabled = false,
            BackColor = tileBg,
            ForeColor = isLight ? LightThemePrimaryColor : Color.White,
            Font = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular),
            Text = string.Empty
        };

        _hoverInfoTextBox = new RichTextBox {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Multiline = false,
            ScrollBars = RichTextBoxScrollBars.None,
            WordWrap = false,
            DetectUrls = false,
            TabStop = false,
            ShortcutsEnabled = false,
            BackColor = tileBg,
            ForeColor = isLight ? LightThemePrimaryColor : Color.White,
            Font = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular),
            Text = string.Empty
        };

        bottomInfoPanel.Controls.Add(_hoverInfoTextBox);
        bottomInfoPanel.Controls.Add(_aggregationInfoTextBox);
        UpdateAggregationInfoLabel(ResolveAggregationInterval());

        _plot = new FormsPlot {
            Dock = DockStyle.Fill,         // Use explicit safe font (Segoe UI is guaranteed on Windows)
            Font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular)
        };
        _plotContextMenu = CreatePlotContextMenu();
        ContextMenuStrip = _plotContextMenu;
        _plot.ContextMenuStrip = null;
        _plot.MouseUp += Plot_MouseUp;
        _plot.MouseMove += Plot_MouseMove;
        _plot.MouseEnter += Plot_MouseEnter;
        _plot.MouseLeave += Plot_MouseLeave;
        _plot.MouseWheel += Plot_MouseWheel;
        _plot.MouseDoubleClick += Plot_MouseDoubleClick;
        _plot.Resize += (_, _) => {
            ReapplyAxisColors();
            _plot.Refresh();
        };
        DisableScottPlotInteractionOverlays();
        ApplyPlotContextMenuTheme();

        // Add header and info labels first
        Controls.Add(_topPanel);
        Controls.Add(bottomInfoPanel);
        AttachTileContextMenuHandlers(this);

        // Use BeginInvoke to defer adding FormsPlot until message loop is ready
        // This avoids GDI+ font errors during auto-scaling
        BeginInvoke(() => {
            Controls.Add(_plot);
            Controls.SetChildIndex(_plot, 0); // Move to back (Fill dock)
            UpdateTheme();
        });
    }
    /// <summary>
    /// Applies the axis rules for series hover snapshot.
    /// </summary>
    /// <param name="axisMap">Input value for axis map.</param>
    private void ApplyAxisRules(Dictionary<MetricType, ScottPlot.IYAxis> axisMap) {
        if (_plot == null) {
            return;
        }

        foreach (var (metric, yAxis) in axisMap) {
            var displaySetting = MetricDisplaySettingsStore.GetSettingOrDefault(metric);
            ConfigureAxisTicksForLogY(yAxis, displaySetting.LogY);
            SetAxisInversion(yAxis, displaySetting.InvertY);

            var rule = MetricAxisRuleStore.GetRule(metric);
            if (rule == null) {
                continue;
            }

            var xAxis = _plot.Plot.Axes.Bottom;

            if (rule.MinBoundary.HasValue && rule.MaxBoundary.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumBoundary(xAxis, yAxis,
                    new ScottPlot.AxisLimits(
                        double.NegativeInfinity, double.PositiveInfinity,
                        rule.MinBoundary.Value, rule.MaxBoundary.Value)));
            }

            // MinimumBoundary - prevent panning/zooming below this Y value
            if (rule.MinBoundary.HasValue && !rule.MaxBoundary.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumBoundary(xAxis, yAxis,
                    new ScottPlot.AxisLimits(
                        double.NegativeInfinity, double.PositiveInfinity,
                        rule.MinBoundary.Value, double.PositiveInfinity)));
            }

            // MaximumBoundary - prevent panning/zooming above this Y value
            if (rule.MaxBoundary.HasValue && !rule.MinBoundary.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumBoundary(xAxis, yAxis,
                    new ScottPlot.AxisLimits(
                        double.NegativeInfinity, double.PositiveInfinity,
                        double.NegativeInfinity, rule.MaxBoundary.Value)));
            }

            // MaximumSpan - limit how far apart Y axis limits can be (limits zoom-out)
            if (rule.MaxSpan.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumSpan(xAxis, yAxis, double.PositiveInfinity, rule.MaxSpan.Value));
            }

            // MinimumSpan - minimum Y-axis range (limits zoom-in)
            if (rule.MinSpan.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MinimumSpan(xAxis, yAxis, 0, rule.MinSpan.Value));
            }
        }
    }



    /// <summary>
    /// Configures the axis ticks for log y for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="useLogarithmicScale">Input value for use logarithmic scale.</param>
    private static void ConfigureAxisTicksForLogY(ScottPlot.IYAxis axis, bool useLogarithmicScale) {
        if (useLogarithmicScale) {
            var tickGen = new ScottPlot.TickGenerators.NumericAutomatic {
                MinorTickGenerator = new ScottPlot.TickGenerators.LogMinorTickGenerator(),
                IntegerTicksOnly = false,
                LabelFormatter = y => {
                    var linearValue = Math.Pow(10, y);
                    var fractionDigits = GetLogYAxisFractionDigits(axis, linearValue);
                    return linearValue.ToString($"N{fractionDigits}");
                }
            };

            axis.TickGenerator = tickGen;
            return;
        }

        axis.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
    }

    /// <summary>
    /// Gets the log y axis fraction digits for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="linearValue">Input value for linear value.</param>
    /// <returns>The result of the operation.</returns>
    private static int GetLogYAxisFractionDigits(ScottPlot.IYAxis axis, double linearValue) {
        var safeLinearValue = double.IsFinite(linearValue) && linearValue > 0 ? linearValue : 1d;
        var minVisibleValue = TryGetVisibleLogYAxisMinValue(axis, out var axisMinValue)
            ? axisMinValue
            : safeLinearValue;
        var visibleSpan = TryGetVisibleLogYAxisSpan(axis, out var axisSpan)
            ? axisSpan
            : double.PositiveInfinity;

        var minValueDigits = GetDigitsBySmallestValue(minVisibleValue);
        var spanDigits = GetDigitsByRange(visibleSpan);

        return Math.Clamp(Math.Max(minValueDigits, spanDigits), 0, 8);
    }

    /// <summary>
    /// Gets the digits by smallest value for series hover snapshot.
    /// </summary>
    /// <param name="minValue">Input value for min value.</param>
    /// <returns>The result of the operation.</returns>
    private static int GetDigitsBySmallestValue(double minValue) {
        if (!double.IsFinite(minValue) || minValue <= 0) {
            return 0;
        }

        if (minValue >= 1) {
            return 0;
        }

        return (int)Math.Ceiling(-Math.Log10(minValue));
    }

    /// <summary>
    /// Gets the digits by range for series hover snapshot.
    /// </summary>
    /// <param name="span">Input value for span.</param>
    /// <returns>The result of the operation.</returns>
    private static int GetDigitsByRange(double span) {
        if (!double.IsFinite(span) || span <= 0) {
            return 0;
        }

        if (span > 1) {
            return 0;
        }

        return (int)Math.Ceiling(-Math.Log10(span));
    }

    /// <summary>
    /// Attempts to get visible log y axis min value for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="minValue">Input value for min value.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TryGetVisibleLogYAxisMinValue(ScottPlot.IYAxis axis, out double minValue) {
        minValue = 0;
        if (!TryGetVisibleLogYAxisBounds(axis, out var minExp, out _)) {
            return false;
        }

        minValue = Math.Pow(10, minExp);
        return double.IsFinite(minValue) && minValue > 0;
    }

    /// <summary>
    /// Attempts to get visible log y axis span for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="span">Input value for span.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TryGetVisibleLogYAxisSpan(ScottPlot.IYAxis axis, out double span) {
        span = 0;
        if (!TryGetVisibleLogYAxisBounds(axis, out var minExp, out var maxExp)) {
            return false;
        }

        var minValue = Math.Pow(10, minExp);
        var maxValue = Math.Pow(10, maxExp);
        if (!double.IsFinite(minValue) || !double.IsFinite(maxValue) || maxValue < minValue) {
            return false;
        }

        span = maxValue - minValue;
        return double.IsFinite(span) && span >= 0;
    }

    /// <summary>
    /// Attempts to get visible log y axis bounds for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="minExp">Input value for min exp.</param>
    /// <param name="maxExp">Input value for max exp.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TryGetVisibleLogYAxisBounds(ScottPlot.IYAxis axis, out double minExp, out double maxExp) {
        maxExp = 0;

        var axisType = axis.GetType();
        var getLimitsMethod = axisType.GetMethod("GetLimits", Type.EmptyTypes);
        if (getLimitsMethod != null) {
            var limits = getLimitsMethod.Invoke(axis, null);
            if (limits != null
                && TryGetNumericValue(limits, "Bottom", out minExp)
                && TryGetNumericValue(limits, "Top", out maxExp)) {
                return maxExp >= minExp;
            }
        }

        if (TryGetNumericValue(axis, "Min", out minExp) && TryGetNumericValue(axis, "Max", out maxExp)) {
            return maxExp >= minExp;
        }

        return false;
    }

    /// <summary>
    /// Attempts to get numeric value for series hover snapshot.
    /// </summary>
    /// <param name="source">Input value for source.</param>
    /// <param name="memberName">Input value for member name.</param>
    /// <param name="value">Input value for value.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TryGetNumericValue(object source, string memberName, out double value) {
        value = 0;
        var sourceType = source.GetType();

        var property = sourceType.GetProperty(memberName);
        if (property != null) {
            var rawValue = property.GetValue(source);
            if (TryConvertToDouble(rawValue, out value)) {
                return true;
            }
        }

        var field = sourceType.GetField(memberName);
        if (field != null) {
            var rawValue = field.GetValue(source);
            if (TryConvertToDouble(rawValue, out value)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to convert to double for series hover snapshot.
    /// </summary>
    /// <param name="rawValue">Input value for raw value.</param>
    /// <param name="value">Input value for value.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TryConvertToDouble(object? rawValue, out double value) {
        value = 0;
        if (rawValue == null) {
            return false;
        }

        return rawValue switch {
            double d when double.IsFinite(d) => (value = d) == d,
            float f when float.IsFinite(f) => (value = f) == f,
            int i => (value = i) == i,
            long l => (value = l) == l,
            decimal m => (value = (double)m) == (double)m,
            _ => false
        };
    }

    /// <summary>
    /// Sets the axis inversion for series hover snapshot.
    /// </summary>
    /// <param name="axis">Input value for axis.</param>
    /// <param name="isInverted">Input value for is inverted.</param>
    private static void SetAxisInversion(object axis, bool isInverted) {
        var axisType = axis.GetType();

        var propertyCandidates = new[] { "Inverted", "IsInverted" };
        foreach (var propertyName in propertyCandidates) {
            var property = axisType.GetProperty(propertyName);
            if (property?.CanWrite == true && property.PropertyType == typeof(bool)) {
                property.SetValue(axis, isInverted);
                return;
            }
        }

        var setInvertedMethod = axisType.GetMethod("SetInverted", [typeof(bool)]);
        if (setInvertedMethod != null) {
            setInvertedMethod.Invoke(axis, [isInverted]);
            return;
        }

        var invertMethod = axisType.GetMethod("Invert", Type.EmptyTypes);
        var isInvertedMethod = axisType.GetMethod("IsInverted", Type.EmptyTypes);
        if (invertMethod != null && isInvertedMethod != null) {
            var current = isInvertedMethod.Invoke(axis, null) as bool?;
            if (current.HasValue && current.Value != isInverted) {
                invertMethod.Invoke(axis, null);
            }
        }
    }

    /// <summary>
    /// Resolves the aggregation interval for series hover snapshot.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private TimeSpan? ResolveAggregationInterval() {
        if (_isStaticMode && _config.CustomPeriodDuration.HasValue && _config.CustomPeriodDuration.Value > TimeSpan.Zero) {
            var staticInterval = ResolveStaticAggregationInterval(_config.CustomPeriodDuration.Value);
            _config.CustomAggregationInterval = staticInterval;
            return staticInterval > TimeSpan.Zero ? staticInterval : null;
        }

        var preset = _periodPresets.FirstOrDefault(p => string.Equals(p.Uid, _config.PeriodPresetUid, StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(preset.Uid)) {
            _config.CustomAggregationInterval = preset.AggregationInterval;
            return preset.AggregationInterval > TimeSpan.Zero ? preset.AggregationInterval : null;
        }

        var legacyInterval = _config.CustomAggregationInterval;
        if (legacyInterval.HasValue) {
            return legacyInterval.Value > TimeSpan.Zero ? legacyInterval : null;
        }

        return DataService.GetRecommendedAggregationInterval(_config.Period);
    }

    /// <summary>
    /// Resolves the static aggregation interval for series hover snapshot.
    /// </summary>
    /// <param name="range">Input value for range.</param>
    /// <returns>The result of the operation.</returns>
    private TimeSpan ResolveStaticAggregationInterval(TimeSpan range) {
        return ChartAggregationHelper.CalculateAutomaticAggregationInterval(
            range,
            _staticAggregationPresetMatchTolerancePercent,
            _staticAggregationTargetPointCount,
            _periodPresets.Select(p => (p.Duration, p.AggregationInterval)),
            applyPeriodMatching: true);
    }

    /// <summary>
    /// Updates the aggregation info label for series hover snapshot.
    /// </summary>
    /// <param name="interval">Input value for interval.</param>
    private void UpdateAggregationInfoLabel(TimeSpan? interval) {
        var aggregationInfoTextBox = _aggregationInfoTextBox;
        if (aggregationInfoTextBox == null || aggregationInfoTextBox.IsDisposed || aggregationInfoTextBox.Disposing) {
            return;
        }

        if (aggregationInfoTextBox.InvokeRequired) {
            aggregationInfoTextBox.BeginInvoke(() => UpdateAggregationInfoLabel(interval));
            return;
        }

        if (!aggregationInfoTextBox.IsHandleCreated) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var color = isLight ? LightThemePrimaryColor : Color.White;
        var valueText = ChartAggregationHelper.FormatAggregationLabel(interval);

        aggregationInfoTextBox.Clear();
        aggregationInfoTextBox.SelectionStart = 0;
        aggregationInfoTextBox.SelectionLength = 0;
        aggregationInfoTextBox.SelectionAlignment = System.Windows.Forms.HorizontalAlignment.Right;
        aggregationInfoTextBox.SelectionColor = color;
        aggregationInfoTextBox.SelectionFont = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Bold);
        aggregationInfoTextBox.AppendText("A:");
        aggregationInfoTextBox.SelectionColor = color;
        aggregationInfoTextBox.SelectionFont = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular);
        aggregationInfoTextBox.AppendText($" {valueText}");
        aggregationInfoTextBox.SelectionColor = color;
        aggregationInfoTextBox.SelectionFont = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Bold);
        aggregationInfoTextBox.AppendText("  P:");
        aggregationInfoTextBox.SelectionColor = color;
        aggregationInfoTextBox.SelectionFont = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular);
        aggregationInfoTextBox.AppendText($" {_lastHorizontalPointCount}");
        aggregationInfoTextBox.SelectionStart = 0;
        aggregationInfoTextBox.SelectionLength = 0;
    }

    /// <summary>
    /// Handles the axis rules changed for series hover snapshot.
    /// </summary>
    private void HandleAxisRulesChanged() {
        if (_plot == null) {
            return;
        }

        if (_plot.InvokeRequired) {
            _plot.BeginInvoke(HandleAxisRulesChanged);
            return;
        }

        RefreshData();
    }

    /// <summary>
    /// Handles the presets changed for series hover snapshot.
    /// </summary>
    private void HandlePresetsChanged() {
        if (_periodSelector == null) {
            return;
        }

        if (_periodSelector.InvokeRequired) {
            _periodSelector.BeginInvoke(HandlePresetsChanged);
            return;
        }

        LoadPeriodPresets();
        SetSelectedPeriodPreset();
    }

    /// <summary>
    /// Loads the period presets for series hover snapshot.
    /// </summary>
    private void LoadPeriodPresets() {
        if (_periodSelector == null) {
            return;
        }

        _periodSelector.Items.Clear();
        _periodPresets = [.. ChartPeriodPresetStore.GetPresetItems()];
        foreach (var preset in _periodPresets) {
            _periodSelector.Items.Add(preset.Label);
        }
    }

    /// <summary>
    /// Sets the selected period preset for series hover snapshot.
    /// </summary>
    private void SetSelectedPeriodPreset() {
        if (_periodSelector == null) {
            return;
        }

        var preservePersistedStaticState = !_isStaticMode && HasPersistedStaticRange();

        var targetPresetUid = _isStaticMode ? _autoPeriodPresetUid : _config.PeriodPresetUid;

        var index = ChartPeriodPresetStore.FindMatchingPresetIndex(targetPresetUid, _periodPresets);
        if (index >= 0) {
            var preset = _periodPresets[index];
            _autoPeriodPresetUid = preset.Uid;
            _autoPeriod = preset.Period;
            _autoCustomDuration = preset.Period == ChartPeriod.Custom ? preset.Duration : null;

            if (!_isStaticMode && !preservePersistedStaticState) {
                _config.PeriodPresetUid = preset.Uid;
                _config.Period = preset.Period;
                _config.CustomPeriodDuration = _autoCustomDuration;
                _config.CustomAggregationInterval = preset.AggregationInterval;
                _config.CustomStartTime = null;
                _config.CustomEndTime = null;
            }

            _periodSelector.SelectedIndex = index;
            return;
        }

        var fallbackPreset = ChartPeriodPresetStore.GetFallbackPreset(_periodPresets);
        _autoPeriodPresetUid = fallbackPreset.Uid;
        _autoPeriod = fallbackPreset.Period;
        _autoCustomDuration = fallbackPreset.Period == ChartPeriod.Custom
            ? fallbackPreset.Duration
            : null;

        if (!_isStaticMode && !preservePersistedStaticState) {
            _config.PeriodPresetUid = _autoPeriodPresetUid;
            _config.Period = _autoPeriod;
            _config.CustomPeriodDuration = _autoCustomDuration;
            _config.CustomAggregationInterval = fallbackPreset.AggregationInterval;
            _config.CustomStartTime = null;
            _config.CustomEndTime = null;
        }

        if (_periodSelector.Items.Count > 0) {
            _periodSelector.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Updates the period from selection for series hover snapshot.
    /// </summary>
    private void UpdatePeriodFromSelection() {
        if (_periodSelector == null || _suppressPeriodChange) {
            return;
        }

        var index = _periodSelector.SelectedIndex;
        if (index < 0 || index >= _periodPresets.Count) {
            return;
        }

        var preset = _periodPresets[index];
        _autoPeriodPresetUid = preset.Uid;
        _autoPeriod = preset.Period;
        _autoCustomDuration = preset.Period == ChartPeriod.Custom ? preset.Duration : null;
        _config.PeriodPresetUid = preset.Uid;
        _config.Period = preset.Period;
        _config.CustomPeriodDuration = preset.Period == ChartPeriod.Custom ? preset.Duration : null;
        _config.CustomAggregationInterval = preset.AggregationInterval;
        _config.CustomStartTime = null;
        _config.CustomEndTime = null;
        RefreshData();
        PeriodChanged?.Invoke(this, _config.PeriodPresetUid);
    }

    /// <summary>
    /// Executes reapply axis colors as part of series hover snapshot processing.
    /// </summary>
    private void ReapplyAxisColors() {
        if (_plot == null) {
            return;
        }

        UpdateVisualColorsForContext(_inspectorActive ? _lastHoverAnchorX : null);
    }

    /// <summary>
    /// Creates the range shift button for series hover snapshot.
    /// </summary>
    /// <param name="shiftLeft">Input value for shift left.</param>
    /// <returns>The result of the operation.</returns>
    private static Button CreateRangeShiftButton(bool shiftLeft) {
        return new Button {
            Width = HeaderControlHeight,
            Height = HeaderControlHeight,
            Margin = new Padding(HeaderButtonSpacing, HeaderButtonY, HeaderButtonSpacing, HeaderButtonY),
            Padding = Padding.Empty,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = {
                BorderSize = 1,
                MouseDownBackColor = Color.Transparent,
                MouseOverBackColor = Color.Transparent
            },
            Text = string.Empty,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            TabStop = false,
            CausesValidation = false,
            Tag = shiftLeft
        };
    }

    /// <summary>
    /// Shifts the current range for series hover snapshot.
    /// </summary>
    /// <param name="direction">Input value for direction.</param>
    private void ShiftCurrentRange(int direction) {
        if (direction == 0) {
            return;
        }

        var (start, end) = GetConfiguredPeriodRange();
        var range = end - start;
        if (range <= TimeSpan.Zero) {
            return;
        }

        var shift = TimeSpan.FromTicks(range.Ticks * direction);
        EnterStaticMode(start + shift, end + shift, raiseEvents: true);
        _plot?.Focus();
    }

    /// <summary>
    /// Updates the range shift buttons appearance for series hover snapshot.
    /// </summary>
    private void UpdateRangeShiftButtonsAppearance() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var iconColor = isLight ? Color.FromArgb(35, 47, 52) : Color.FromArgb(223, 234, 239);
        var buttonBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var hoverBg = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(58, 74, 80);
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        foreach (var button in new[] { _autoShiftLeftButton, _autoShiftRightButton, _staticShiftLeftButton, _staticShiftRightButton }) {
            if (button == null) {
                continue;
            }

            var shiftLeft = button.Tag is bool value && value;
            button.BackColor = buttonBg;
            button.Image = MaterialIcons.GetIcon(shiftLeft ? "keyboard_double_arrow_left" : "keyboard_double_arrow_right", iconColor, 16, IconRenderPreset.DarkOutlined);
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseOverBackColor = hoverBg;
            button.FlatAppearance.MouseDownBackColor = hoverBg;
        }
    }

    /// <summary>
    /// Creates the static date picker for series hover snapshot.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private static ThemedDateTimePicker CreateStaticDatePicker() {
        return new ThemedDateTimePicker {
            Width = 176,
            Height = HeaderControlHeight,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Margin = new Padding(4, 2, 4, 2)
        };
    }

    /// <summary>
    /// Determines whether has persisted static range for series hover snapshot.
    /// </summary>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private bool HasPersistedStaticRange() {
        if (_config.Period != ChartPeriod.Custom
            || !_config.CustomStartTime.HasValue
            || !_config.CustomEndTime.HasValue) {
            return false;
        }

        var startLocal = ToLocalChartTime(_config.CustomStartTime.Value);
        var endLocal = ToLocalChartTime(_config.CustomEndTime.Value);
        return endLocal > startLocal;
    }

    /// <summary>
    /// Executes restore persisted static state as part of series hover snapshot processing.
    /// </summary>
    private void RestorePersistedStaticState() {
        if (!HasPersistedStaticRange()) {
            _staticModePaused = false;
            _config.StaticModePaused = false;
            return;
        }

        var startLocal = ToLocalChartTime(_config.CustomStartTime!.Value);
        var endLocal = ToLocalChartTime(_config.CustomEndTime!.Value);
        if (endLocal <= startLocal) {
            _staticModePaused = false;
            _config.StaticModePaused = false;
            return;
        }

        SetStaticMode(true);
        SyncStaticPickers(startLocal, endLocal);
        _lastChartInteractionUtc = _staticModePaused ? DateTime.MinValue : DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the mode switch appearance for series hover snapshot.
    /// </summary>
    private void UpdateModeSwitchAppearance() {
        if (_modeSegmentPanel == null || _inspectorHostPanel == null || _inspectorSegmentPanel == null || _autoModeButton == null
            || _staticModeButton == null || _pauseModeButton == null || _countdownLabel == null
            || _modeSwitchContainer == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);
        var iconColor = isLight ? Color.FromArgb(35, 47, 52) : Color.FromArgb(223, 234, 239);
        var tileBg = isLight ? Color.White : Color.FromArgb(35, 47, 52);

        _modeSwitchContainer.BackColor = tileBg;
        _inspectorHostPanel.BackColor = tileBg;
        _inspectorSegmentPanel.BackColor = borderColor;

        _modeSwitchContainer.Width = ModeSwitchContainerWidth;
        _modeSegmentPanel.Size = new Size(HeaderControlButtonSize * 2 + HeaderButtonSpacing, HeaderControlButtonSize);
        _autoModeButton.Location = new Point(0, 0);
        _staticModeButton.Location = new Point(HeaderControlButtonSize + HeaderButtonSpacing, 0);
        LayoutModeControlBlock();

        if (_autoPeriodPanel != null) {
            _autoPeriodPanel.Width = AutoRangePanelWidth;
            _autoPeriodPanel.Padding = new Padding(AutoRangePanelLeftPadding, 0, 0, 0);
        }

        LayoutAutoPeriodPanel();

        LayoutStaticRangePanel();

        _modeSegmentPanel.BackColor = borderColor;
        _pauseModeButton.BackColor = segmentBg;
        _pauseModeButton.FlatAppearance.BorderColor = borderColor;
        var hoverBg = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(58, 74, 80);
        _pauseModeButton.FlatAppearance.CheckedBackColor = segmentBg;
        _pauseModeButton.FlatAppearance.MouseOverBackColor = hoverBg;
        _pauseModeButton.FlatAppearance.MouseDownBackColor = hoverBg;
        _autoModeButton.BackColor = _autoModeButton.Checked ? activeBg : segmentBg;
        _autoModeButton.FlatAppearance.BorderColor = borderColor;
        _staticModeButton.BackColor = _staticModeButton.Checked ? activeBg : segmentBg;
        _staticModeButton.FlatAppearance.BorderColor = borderColor;
        _pauseModeButton.Visible = _isStaticMode;

        _pauseModeButton.Image = MaterialIcons.GetIcon(_pauseModeButton.Checked ? "play_arrow" : "pause", iconColor, 22, IconRenderPreset.DarkOutlined);
        _pauseModeButton.ImageAlign = ContentAlignment.MiddleCenter;
        _autoModeButton.Image = MaterialIcons.GetIcon(MaterialIcons.ChartModeAuto, iconColor, 22, IconRenderPreset.DarkOutlined);
        _autoModeButton.ImageAlign = ContentAlignment.MiddleCenter;
        _staticModeButton.Image = MaterialIcons.GetIcon(MaterialIcons.ChartModeStatic, iconColor, 22, IconRenderPreset.DarkOutlined);
        _staticModeButton.ImageAlign = ContentAlignment.MiddleCenter;
        if (_inspectorButton != null) {
            _inspectorButton.BackColor = _inspectorButton.Checked ? activeBg : segmentBg;
            _inspectorButton.FlatAppearance.BorderColor = borderColor;
            _inspectorButton.FlatAppearance.MouseOverBackColor = hoverBg;
            _inspectorButton.FlatAppearance.MouseDownBackColor = hoverBg;
            _inspectorButton.Image = MaterialIcons.GetIcon(MaterialIcons.ChartInspector, iconColor, 22, IconRenderPreset.DarkOutlined);
            _inspectorButton.ImageAlign = ContentAlignment.MiddleCenter;
        }

        UpdateRangeShiftButtonsAppearance();

        _countdownLabel.ForeColor = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        _countdownLabel.BackColor = tileBg;
        _countdownLabel.Visible = _isStaticMode && !_staticModePaused;
    }

    /// <summary>
    /// Lays out the auto period panel for series hover snapshot.
    /// </summary>
    private void LayoutAutoPeriodPanel() {
        if (_autoPeriodPanel == null || _autoRangeSpacer == null || _periodSelector == null || _autoShiftLeftButton == null || _autoShiftRightButton == null) {
            return;
        }

        var usedWidth = _periodSelector.Width + _periodSelector.Margin.Horizontal
            + _autoShiftLeftButton.Width + _autoShiftLeftButton.Margin.Horizontal
            + _autoShiftRightButton.Width + _autoShiftRightButton.Margin.Horizontal;
        var availableForSpacer = _autoPeriodPanel.ClientSize.Width - _autoPeriodPanel.Padding.Horizontal - usedWidth;
        _autoRangeSpacer.Width = Math.Max(0, availableForSpacer);
    }

    /// <summary>
    /// Lays out the static range panel for series hover snapshot.
    /// </summary>
    private void LayoutStaticRangePanel() {
        if (_staticRangePanel == null || _staticStartPicker == null || _staticEndPicker == null || _staticShiftLeftButton == null || _staticShiftRightButton == null) {
            return;
        }

        var usedWidth = _staticRangePanel.Padding.Horizontal
            + _staticStartPicker.Width + _staticStartPicker.Margin.Horizontal
            + _staticEndPicker.Width + _staticEndPicker.Margin.Horizontal
            + _staticShiftLeftButton.Width + _staticShiftLeftButton.Margin.Horizontal
            + _staticShiftRightButton.Width + _staticShiftRightButton.Margin.Horizontal;

        _staticRangePanel.Width = usedWidth;
    }

    /// <summary>
    /// Lays out the mode control block for series hover snapshot.
    /// </summary>
    private void LayoutModeControlBlock() {
        if (_modeSwitchContainer == null || _modeSegmentPanel == null || _pauseModeButton == null || _countdownLabel == null) {
            return;
        }

        var modeX = _modeSwitchContainer.Width - _modeSegmentPanel.Width - HeaderControlRightPadding;
        _modeSegmentPanel.Location = new Point(Math.Max(modeX, HeaderButtonY), HeaderButtonY);

        var countdownX = _modeSegmentPanel.Left - _countdownLabel.Width - HeaderButtonSpacing;
        _countdownLabel.Location = new Point(Math.Max(countdownX, HeaderButtonY), HeaderButtonY);

        var pauseX = _countdownLabel.Left - _pauseModeButton.Width - HeaderButtonSpacing;
        _pauseModeButton.Location = new Point(Math.Max(pauseX, HeaderButtonY), HeaderButtonY);
    }

    /// <summary>
    /// Updates the countdown label for series hover snapshot.
    /// </summary>
    private void UpdateCountdownLabel() {
        if (_countdownLabel == null) {
            return;
        }

        if (!_isStaticMode || _staticModePaused || _lastChartInteractionUtc == DateTime.MinValue) {
            _countdownLabel.Visible = false;
            return;
        }

        var elapsed = DateTime.UtcNow - _lastChartInteractionUtc;
        var remaining = _staticModeTimeout - elapsed;

        if (remaining.TotalSeconds <= 0) {
            _countdownLabel.Text = "0:00";
            _countdownLabel.Visible = true;
            return;
        }

        _countdownLabel.Text = $"{(int)remaining.TotalMinutes}:{remaining.Seconds:D2}";
        _countdownLabel.Visible = true;
    }

    /// <summary>
    /// Sets the static mode for series hover snapshot.
    /// </summary>
    /// <param name="enabled">Input value for enabled.</param>
    private void SetStaticMode(bool enabled) {
        _isStaticMode = enabled;

        _topPanel?.SuspendLayout();
        try {
            if (_autoPeriodPanel != null) {
                _autoPeriodPanel.Visible = !enabled;
                _autoPeriodPanel.PerformLayout();
            }

            if (_staticRangePanel != null) {
                _staticRangePanel.Visible = enabled;
                _staticRangePanel.PerformLayout();
            }

            if (enabled) {
                _staticRangePanel?.BringToFront();
            } else {
                _autoPeriodPanel?.BringToFront();
            }
        } finally {
            _topPanel?.ResumeLayout(performLayout: true);
        }

        // Sync mode switcher radio buttons
        if (_autoModeButton != null && _staticModeButton != null) {
            if (enabled && !_staticModeButton.Checked) {
                _staticModeButton.Checked = true;
            } else if (!enabled && !_autoModeButton.Checked) {
                _autoModeButton.Checked = true;
            }
        }

        if (_pauseModeButton != null && _pauseModeButton.Checked != _staticModePaused) {
            _pauseModeButton.Checked = _staticModePaused;
        }

        _countdownLabel?.Visible = enabled && !_staticModePaused;
        UpdateModeSwitchAppearance();
        UpdateAggregationInfoLabel(ResolveAggregationInterval());
        if (enabled && !_staticModePaused) {
            UpdateCountdownLabel();
        }

        _topPanel?.PerformLayout();
        _topPanel?.Invalidate();
    }

    /// <summary>
    /// Executes enter static mode as part of series hover snapshot processing.
    /// </summary>
    /// <param name="startLocal">Input value for start local.</param>
    /// <param name="endLocal">Input value for end local.</param>
    /// <param name="raiseEvents">Input value for raise events.</param>
    private void EnterStaticMode(DateTime startLocal, DateTime endLocal, bool raiseEvents) {
        if (!_isStaticMode) {
            _autoPeriod = _config.Period;
            _autoCustomDuration = _config.CustomPeriodDuration;
            _autoPeriodPresetUid = _config.PeriodPresetUid;
        }

        SetStaticMode(true);
        _config.Period = ChartPeriod.Custom;
        _config.CustomStartTime = startLocal.ToUniversalTime();
        _config.CustomEndTime = endLocal.ToUniversalTime();
        _config.CustomPeriodDuration = endLocal - startLocal;
        _lastChartInteractionUtc = DateTime.UtcNow;
        SyncStaticPickers(startLocal, endLocal);
        RefreshData();

        if (raiseEvents) {
            StaticRangeChanged?.Invoke(this, startLocal, endLocal);
        }
    }

    /// <summary>
    /// Executes sync static pickers as part of series hover snapshot processing.
    /// </summary>
    /// <param name="startLocal">Input value for start local.</param>
    /// <param name="endLocal">Input value for end local.</param>
    private void SyncStaticPickers(DateTime startLocal, DateTime endLocal) {
        if (_staticStartPicker == null || _staticEndPicker == null) {
            return;
        }

        try {
            _suppressStaticRangeChange = true;
            _staticStartPicker.Value = startLocal;
            _staticEndPicker.Value = endLocal;
        } finally {
            _suppressStaticRangeChange = false;
        }
    }

    /// <summary>
    /// Executes static picker value changed as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void StaticPicker_ValueChanged(object? sender, EventArgs e) {
        if (_suppressStaticRangeChange || _staticStartPicker == null || _staticEndPicker == null) {
            return;
        }

        var start = _staticStartPicker.Value;
        var end = _staticEndPicker.Value;
        if (end <= start) {
            return;
        }

        EnterStaticMode(start, end, raiseEvents: true);
    }

    /// <summary>
    /// Executes static mode timer tick as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void StaticModeTimer_Tick(object? sender, EventArgs e) {
        if (!_isStaticMode || _staticModePaused || _lastChartInteractionUtc == DateTime.MinValue) {
            return;
        }

        UpdateCountdownLabel();

        if (DateTime.UtcNow - _lastChartInteractionUtc >= _staticModeTimeout) {
            ExitStaticMode();
        }
    }

    /// <summary>
    /// Ensures the inspector state for series hover snapshot.
    /// </summary>
    private void EnsureInspectorState() {
        _inspectorActive = _config.ShowInspector;

        if (_inspectorButton != null && _inspectorButton.Checked != _inspectorActive) {
            _inspectorButton.Checked = _inspectorActive;
        }

        if (!_inspectorActive) {
            HideInspector();
        }
    }

    /// <summary>
    /// Executes plot mouse move as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Plot_MouseMove(object? sender, MouseEventArgs e) {
        if (!_inspectorActive || _plot == null || _hoverSeries.Count == 0) {
            return;
        }

        var coordinates = _plot.Plot.GetCoordinates(new Pixel(e.X, e.Y));
        var anchor = FindNearestAnchorX(coordinates.X);
        if (!anchor.HasValue) {
            return;
        }

        if (_lastHoverAnchorX.HasValue && Math.Abs(_lastHoverAnchorX.Value - anchor.Value) < 1e-9) {
            return;
        }

        UpdateHoverVerticalLine(anchor.Value);
        UpdateHoverInfo(anchor.Value);
        _plot.Refresh();
        HoverAnchorChanged?.Invoke(this, anchor.Value);
    }

    /// <summary>
    /// Executes plot mouse enter as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Plot_MouseEnter(object? sender, EventArgs e) {
        PlotHoverPresenceChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Executes plot mouse leave as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Plot_MouseLeave(object? sender, EventArgs e) {
        PlotHoverPresenceChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Finds the nearest anchor x for series hover snapshot.
    /// </summary>
    /// <param name="x">Input value for x.</param>
    /// <returns>The result of the operation.</returns>
    private double? FindNearestAnchorX(double x) {
        double? bestX = null;
        var bestDelta = double.PositiveInfinity;

        foreach (var series in _hoverSeries) {
            if (series.Xs.Length == 0) {
                continue;
            }

            var index = FindNearestIndex(series.Xs, x);
            if (index < 0) {
                continue;
            }

            var delta = Math.Abs(series.Xs[index] - x);
            if (delta < bestDelta) {
                bestDelta = delta;
                bestX = series.Xs[index];
            }
        }

        return bestX;
    }

    /// <summary>
    /// Finds the nearest index for series hover snapshot.
    /// </summary>
    /// <param name="sortedValues">Input value for sorted values.</param>
    /// <param name="value">Input value for value.</param>
    /// <returns>The result of the operation.</returns>
    private static int FindNearestIndex(double[] sortedValues, double value) {
        if (sortedValues.Length == 0) {
            return -1;
        }

        var index = Array.BinarySearch(sortedValues, value);
        if (index >= 0) {
            return index;
        }

        var next = ~index;
        if (next <= 0) {
            return 0;
        }

        if (next >= sortedValues.Length) {
            return sortedValues.Length - 1;
        }

        var prev = next - 1;
        return Math.Abs(sortedValues[prev] - value) <= Math.Abs(sortedValues[next] - value)
            ? prev
            : next;
    }

    /// <summary>
    /// Updates the hover vertical line for series hover snapshot.
    /// </summary>
    /// <param name="x">Input value for x.</param>
    private void UpdateHoverVerticalLine(double x) {
        if (_plot == null) {
            return;
        }

        if (_hoverVerticalLine == null) {
            var line = _plot.Plot.Add.VerticalLine(x);
            // Inspector marker should be clearly visible against dense chart data.
            // Use a dotted pattern to create a subtle "textured" appearance.
            line.LinePattern = LinePattern.Dotted;
            line.LineWidth = 0.8f;
            line.IsVisible = true;
            _hoverVerticalLine = line;
        }

        if (_hoverVerticalLine is ScottPlot.Plottables.VerticalLine verticalLine) {
            verticalLine.X = x;
            verticalLine.IsVisible = true;
            var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
            verticalLine.Color = ScottPlot.Color.FromColor(isLight ? Color.FromArgb(165, 55, 70, 78) : Color.FromArgb(185, 214, 228, 236));
        }
    }

    /// <summary>
    /// Updates the hover info for series hover snapshot.
    /// </summary>
    /// <param name="anchorX">Input value for anchor x.</param>
    private void UpdateHoverInfo(double anchorX) {
        if (_hoverInfoTextBox == null) {
            return;
        }

        _lastHoverAnchorX = anchorX;
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var baseTextColor = isLight ? LightThemePrimaryColor : Color.White;
        var timestamp = DateTime.FromOADate(anchorX).ToString("yyyy-MM-dd HH:mm:ss");

        _hoverInfoTextBox.SuspendLayout();
        _hoverInfoTextBox.Clear();

        var visibleSeries = new List<(SeriesHoverSnapshot Series, double Value)>();
        foreach (var series in _hoverSeries) {
            var index = FindNearestIndex(series.Xs, anchorX);
            if (index < 0 || index >= series.Ys.Length) {
                continue;
            }

            var value = series.Ys[index];
            if (double.IsNaN(value)) {
                continue;
            }

            visibleSeries.Add((series, value));
        }

        UpdateVisualColorsForContext(anchorX, visibleSeries);

        AppendHoverInfoChunk(timestamp, isBold: false, baseTextColor, addSeparator: visibleSeries.Count > 0);

        for (var i = 0; i < visibleSeries.Count; i++) {
            var (series, value) = visibleSeries[i];

            var seriesColor = ResolveSeriesVisualColor(series, value);
            AppendHoverInfoChunk(series.Label, isBold: true, seriesColor, addSeparator: false);
            string formattedValue;
            var appendUnit = true;
            if (!string.IsNullOrEmpty(series.ValueSchemeName)) {
                var valueScheme = _valueSchemesByName.TryGetValue(series.ValueSchemeName, out var cachedScheme)
                    ? cachedScheme
                    : null;
                var transformedText = valueScheme?.GetText(value);
                formattedValue = transformedText ?? MetricDisplaySettingsStore.FormatMetricValue(series.Metric, value);
                appendUnit = false;
            } else {
                formattedValue = MetricDisplaySettingsStore.FormatMetricValue(series.Metric, value);
            }

            var unit = appendUnit && !string.IsNullOrWhiteSpace(series.Unit)
                ? $" {series.Unit}"
                : string.Empty;

            AppendHoverInfoChunk($": {formattedValue}{unit}", isBold: false, seriesColor,
                addSeparator: i < visibleSeries.Count - 1);
        }

        _hoverInfoTextBox.SelectionStart = 0;
        _hoverInfoTextBox.SelectionLength = 0;
        _hoverInfoTextBox.ResumeLayout();
    }

    /// <summary>
    /// Executes append hover info chunk as part of series hover snapshot processing.
    /// </summary>
    /// <param name="text">Input value for text.</param>
    /// <param name="isBold">Input value for is bold.</param>
    /// <param name="color">Input value for color.</param>
    /// <param name="addSeparator">Input value for add separator.</param>
    private void AppendHoverInfoChunk(string text, bool isBold, Color color, bool addSeparator) {
        if (_hoverInfoTextBox == null || string.IsNullOrEmpty(text)) {
            return;
        }

        _hoverInfoTextBox.SelectionStart = _hoverInfoTextBox.TextLength;
        _hoverInfoTextBox.SelectionLength = 0;
        _hoverInfoTextBox.SelectionColor = color;
        _hoverInfoTextBox.SelectionFont = CreateSafeFont(
            "Segoe UI", 7.5f, isBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
        _hoverInfoTextBox.AppendText(text);

        if (!addSeparator) {
            return;
        }

        _hoverInfoTextBox.SelectionStart = _hoverInfoTextBox.TextLength;
        _hoverInfoTextBox.SelectionLength = 0;
        _hoverInfoTextBox.SelectionColor = color;
        _hoverInfoTextBox.SelectionFont = CreateSafeFont("Segoe UI", 7.5f, System.Drawing.FontStyle.Regular);
        _hoverInfoTextBox.AppendText("  |  ");
    }

    /// <summary>
    /// Hides the inspector for series hover snapshot.
    /// </summary>
    private void HideInspector() {
        if (_hoverVerticalLine is ScottPlot.Plottables.VerticalLine verticalLine) {
            verticalLine.IsVisible = false;
            _plot?.Refresh();
        }

        _hoverInfoTextBox?.Clear();
        _lastHoverAnchorX = null;
        UpdateVisualColorsForContext(null);
    }

    /// <summary>
    /// Resolves the series visual color for series hover snapshot.
    /// </summary>
    /// <param name="series">Input value for series.</param>
    /// <param name="value">Input value for value.</param>
    /// <returns>The result of the operation.</returns>
    private Color ResolveSeriesVisualColor(SeriesHoverSnapshot series, double value) {
        if (!string.IsNullOrWhiteSpace(series.ColorSchemeName)
            && _colorSchemesByName.TryGetValue(series.ColorSchemeName, out var colorScheme)) {
            return colorScheme.GetColor(value);
        }

        return series.BaseColor;
    }

    /// <summary>
    /// Updates visual colors for context for series hover snapshot.
    /// </summary>
    /// <param name="anchorX">Input value for anchor x.</param>
    /// <param name="visibleSeries">Input value for visible series.</param>
    private void UpdateVisualColorsForContext(double? anchorX, List<(SeriesHoverSnapshot Series, double Value)>? visibleSeries = null) {
        if (_plot == null || _hoverSeries.Count == 0) {
            return;
        }

        var seriesValues = visibleSeries;
        if (seriesValues == null) {
            seriesValues = [];
            foreach (var series in _hoverSeries) {
                var index = anchorX.HasValue ? FindNearestIndex(series.Xs, anchorX.Value) : series.Ys.Length - 1;
                if (index < 0 || index >= series.Ys.Length) {
                    continue;
                }

                var value = series.Ys[index];
                if (double.IsNaN(value)) {
                    continue;
                }

                seriesValues.Add((series, value));
            }
        }

        var metricColors = new Dictionary<MetricType, ScottPlot.Color>();
        foreach (var (series, value) in seriesValues) {
            var color = ResolveSeriesVisualColor(series, value);
            series.SeriesColor = color;
            if (series.LegendPlottable is ScottPlot.Plottables.Scatter legendScatter) {
                legendScatter.Color = ScottPlot.Color.FromColor(color);
            }

            metricColors[series.Metric] = ScottPlot.Color.FromColor(color);
        }

        SetAxisNeutralState(_plot.Plot.Axes.Left);
        SetAxisNeutralState(_plot.Plot.Axes.Right);
        foreach (var axis in _extraAxes) {
            SetAxisNeutralState(axis);
        }

        foreach (var (metric, axis) in _metricAxes) {
            if (!_metricsWithData.Contains(metric)) {
                continue;
            }

            var axisColor = metricColors.TryGetValue(metric, out var resolvedColor)
                ? resolvedColor
                : ScottPlot.Color.FromColor(
                    _metricFallbackColors.TryGetValue(metric, out var fallbackColor)
                        ? fallbackColor
                        : (MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT
                            ? LightThemePrimaryColor
                            : Color.White));
            var labelText = _metricAxisLabels.TryGetValue(metric, out var label)
                ? label
                : BuildAxisLabel(metric, _plot.Height);

            StyleAxis(axis, labelText, axisColor);
            SetAxisVisibility(axis, true);
        }
    }

    /// <summary>
    /// Executes plot mouse wheel as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Plot_MouseWheel(object? sender, MouseEventArgs e) {
        DetectXAxisInteraction();
    }

    /// <summary>
    /// Executes plot mouse double click as part of series hover snapshot processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Plot_MouseDoubleClick(object? sender, MouseEventArgs e) {
        DetectXAxisInteraction();
    }

    /// <summary>
    /// Executes detect x axis interaction as part of series hover snapshot processing.
    /// </summary>
    private void DetectXAxisInteraction() {
        if (_plot == null) {
            return;
        }

        var limits = _plot.Plot.Axes.GetLimits();
        if (double.IsNaN(limits.Left) || double.IsNaN(limits.Right) || limits.Right <= limits.Left) {
            return;
        }

        if (_lastXAxisMin.HasValue && _lastXAxisMax.HasValue
            && Math.Abs(_lastXAxisMin.Value - limits.Left) < 1e-9
            && Math.Abs(_lastXAxisMax.Value - limits.Right) < 1e-9) {
            return;
        }

        var start = DateTime.FromOADate(limits.Left);
        var end = DateTime.FromOADate(limits.Right);
        EnterStaticMode(start, end, raiseEvents: true);
    }

    /// <summary>
    /// Executes remember current x axis limits as part of series hover snapshot processing.
    /// </summary>
    private void RememberCurrentXAxisLimits() {
        if (_plot == null) {
            return;
        }

        var limits = _plot.Plot.Axes.GetLimits();
        if (double.IsNaN(limits.Left) || double.IsNaN(limits.Right)) {
            return;
        }

        _lastXAxisMin = limits.Left;
        _lastXAxisMax = limits.Right;
    }
    #endregion Private Methods
}
