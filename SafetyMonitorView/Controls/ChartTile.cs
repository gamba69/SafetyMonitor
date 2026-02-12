using MaterialSkin;
using SafetyMonitorView.Forms;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ScottPlot.WinForms;
using System.Data;
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;

namespace SafetyMonitorView.Controls;

public class ChartTile : Panel {

    #region Private Fields

    private readonly ChartTileConfig _config;
    private readonly DataService _dataService;
    private readonly List<ScottPlot.IYAxis> _extraAxes = [];
    private bool _initialized;
    private readonly ThemedMenuRenderer _contextMenuRenderer = new();
    private const int MenuIconSize = 16;
    private ContextMenuStrip? _plotContextMenu;
    private List<ChartPeriodPreset> _periodPresets = [];
    private ComboBox? _periodSelector;
    private bool _suppressPeriodChange;
    private FormsPlot? _plot;
    private Label? _titleLabel;
    private Panel? _topPanel;

    #endregion Private Fields

    #region Public Events

    public event Action<ChartTile, ChartPeriod, TimeSpan?>? PeriodChanged;

    #endregion Public Events

    #region Public Constructors

    public ChartTile(ChartTileConfig config, DataService dataService) {
        _config = config;
        _dataService = dataService;
        Dock = DockStyle.Fill;
        BorderStyle = BorderStyle.FixedSingle;

        // Set a valid font to prevent GDI+ errors during auto-scaling
        // when child controls are added. The font is inherited from parent
        // and if parent has an invalid font (e.g., "Roboto" not installed),
        // Controls.Add() will throw when accessing FontHandle.
        Font = SystemFonts.DefaultFont;
    }

    #endregion Public Constructors

    #region Public Methods

    public void RefreshData() {
        if (_plot == null) {
            return;
        }

        _plot.Plot.Clear();
        ClearExtraAxes();

        // Reset the default left axis label
        _plot.Plot.Axes.Left.Label.Text = string.Empty;

        if (_config.MetricAggregations.Count == 0) {
            _plot.Plot.Title("No metrics configured");
            ApplyThemeColors();
            _plot.Refresh();
            return;
        }

        var aggregationInterval = _config.CustomAggregationInterval
            ?? DataService.GetRecommendedAggregationInterval(_config.Period);
        var isLightTheme = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;

        // Determine distinct metric types to decide if multiple Y axes are needed
        var distinctMetrics = _config.MetricAggregations
            .Select(a => a.Metric)
            .Distinct()
            .ToList();

        bool useMultipleAxes = distinctMetrics.Count > 1;
        var axisMap = new Dictionary<MetricType, ScottPlot.IYAxis>();
        var axisStyleMap = new Dictionary<MetricType, (string LabelText, ScottPlot.Color Color)>();

        if (useMultipleAxes) {
            for (int i = 0; i < distinctMetrics.Count; i++) {
                var metric = distinctMetrics[i];
                var labelText = BuildAxisLabel(metric);

                // Use the color of the first series for this metric to tint the axis
                var representativeColor = ScottPlot.Color.FromColor(
                    _config.MetricAggregations.First(a => a.Metric == metric).GetColorForTheme(isLightTheme));
                axisStyleMap[metric] = (labelText, representativeColor);

                if (i == 0) {
                    // First metric — use the built-in left Y axis
                    StyleAxis(_plot.Plot.Axes.Left, labelText, representativeColor);
                    axisMap[metric] = _plot.Plot.Axes.Left;
                } else if (i == 1) {
                    // Second metric — use the built-in right Y axis
                    StyleAxis(_plot.Plot.Axes.Right, labelText, representativeColor);
                    axisMap[metric] = _plot.Plot.Axes.Right;
                } else {
                    // 3rd+ metrics — create additional right-side Y axes
                    var extraAxis = _plot.Plot.Axes.AddRightAxis();
                    StyleAxis(extraAxis, labelText, representativeColor);
                    axisMap[metric] = extraAxis;
                    _extraAxes.Add(extraAxis);
                }
            }
        } else if (distinctMetrics.Count == 1) {
            var metric = distinctMetrics[0];
            var labelText = BuildAxisLabel(metric);
            var representativeColor = ScottPlot.Color.FromColor(
                _config.MetricAggregations.First(a => a.Metric == metric).GetColorForTheme(isLightTheme));
            axisStyleMap[metric] = (labelText, representativeColor);
            StyleAxis(_plot.Plot.Axes.Left, labelText, representativeColor);
            axisMap[metric] = _plot.Plot.Axes.Left;
        }

        var hasVisibleSeries = false;
        var metricsWithData = new HashSet<MetricType>();

        foreach (var agg in _config.MetricAggregations) {
            var data = _dataService.GetChartData(
                _config.Period, _config.CustomStartTime, _config.CustomEndTime, _config.CustomPeriodDuration,
                aggregationInterval, agg.Function);
            if (data.Count == 0) {
                continue;
            }
            var timestamps = data.Select(d => DateTime.SpecifyKind(d.Timestamp, DateTimeKind.Utc)
                .ToLocalTime()
                .ToOADate()).ToArray();
            var values = data.Select(d => agg.Metric.GetValue(d) ?? double.NaN).ToArray();
            var validData = timestamps
                .Zip(values, (t, v) => new { Time = t, Value = v })
                .Where(x => !double.IsNaN(x.Value))
                .ToArray();
            if (validData.Length == 0) {
                continue;
            }
            var validTimes = validData.Select(x => x.Time).ToArray();
            var validValues = validData.Select(x => x.Value).ToArray();
            var scatter = _plot.Plot.Add.Scatter(validTimes, validValues);
            hasVisibleSeries = true;
            metricsWithData.Add(agg.Metric);
            scatter.LegendText = agg.Label;
            scatter.Color = ScottPlot.Color.FromColor(agg.GetColorForTheme(isLightTheme));
            scatter.LineWidth = agg.LineWidth;
            scatter.MarkerSize = agg.ShowMarkers ? 5 : 0;
            scatter.Smooth = agg.Smooth;
            ApplySmoothTension(scatter, agg);

            // Assign this series to its dedicated Y axis when multiple axes are active
            if (useMultipleAxes && axisMap.TryGetValue(agg.Metric, out var yAxis)) {
                scatter.Axes.YAxis = yAxis;
            }
        }

        _plot.Plot.Axes.DateTimeTicksBottom();

        // Clear any previously added axis rules
        _plot.Plot.Axes.Rules.Clear();

        if (hasVisibleSeries) {
            _plot.Plot.Axes.AutoScale();
        } else {
            var (startTime, endTime) = GetConfiguredPeriodRange();
            _plot.Plot.Axes.SetLimitsX(startTime.ToOADate(), endTime.ToOADate());
        }

        // Apply per-metric Y-axis rules from the global store
        ApplyAxisRules(axisMap);

        ApplyThemeColors();
        ApplyYAxisVisibility(axisMap, axisStyleMap, metricsWithData);
        if (_config.ShowLegend && _config.MetricAggregations.Count > 1) {
            _plot.Plot.ShowLegend();
        } else {
            _plot.Plot.HideLegend();
        }

        if (_config.ShowGrid) {
            var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
            _plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromColor(
                isLight ? Color.LightGray : Color.FromArgb(53, 70, 76));
            _plot.Plot.Grid.MajorLineWidth = 1;
        } else {
            _plot.Plot.HideGrid();
        }
        _plot.Refresh();
    }

    public void UpdateTheme() {
        if (_plot == null || _titleLabel == null) {
            return;
        }

        ApplyThemeColors();
        ApplyTileColors();
        ApplyPlotContextMenuTheme();
        // Restore title font that may have been overwritten by MaterialSkinManager
        var expectedFont = CreateSafeFont("Roboto", 11, System.Drawing.FontStyle.Bold);
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

    public void SetPeriod(ChartPeriod period, TimeSpan? customDuration = null, bool refreshData = true) {
        _config.Period = period;
        _config.CustomPeriodDuration = period == ChartPeriod.Custom ? customDuration : null;
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

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Prevents MaterialSkinManager font propagation from overwriting tile fonts.
    /// </summary>
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        if (_initialized) {
            ApplyTileColors();
        }
        if (_initialized && _titleLabel != null) {
            var oldFont = _titleLabel.Font;
            _titleLabel.Font = CreateSafeFont("Roboto", 11, System.Drawing.FontStyle.Bold);
            oldFont.Dispose();
        }
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);

        // Initialize UI only after handle is created (GDI+ is ready)
        if (!_initialized) {
            _initialized = true;
            InitializeUI();
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            ChartPeriodPresetStore.PresetsChanged -= HandlePresetsChanged;
            MetricAxisRuleStore.RulesChanged -= HandleAxisRulesChanged;
            _plot?.MouseUp -= Plot_MouseUp;
            _plotContextMenu?.Dispose();
            _plotContextMenu = null;
        }
        base.Dispose(disposing);
    }

    #endregion Protected Methods

    #region Private Methods

    /// <summary>
    /// Builds a human-readable axis label from a MetricType,
    /// e.g. "Temperature (°C)" or just "Safety" when no unit exists.
    /// </summary>
    private static string BuildAxisLabel(MetricType metric) {
        var unit = metric.GetUnit();
        return string.IsNullOrEmpty(unit)
            ? metric.GetDisplayName()
            : $"{metric.GetDisplayName()} ({unit})";
    }

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
    /// Safely creates a font with fallback to system default if the requested font is not available.
    /// </summary>
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
    /// Applies per-metric color styling to a Y axis so it visually matches
    /// the series that are plotted against it.
    /// </summary>
    private static void StyleAxis(ScottPlot.IYAxis axis, string labelText, ScottPlot.Color color) {
        axis.Label.Text = labelText;
        axis.Label.ForeColor = color;
        axis.TickLabelStyle.ForeColor = color;
        axis.FrameLineStyle.Color = color;
        axis.MajorTickStyle.Color = color;
        axis.MinorTickStyle.Color = color;
    }

    private (DateTime start, DateTime end) GetConfiguredPeriodRange() {
        var endTime = _config.CustomEndTime.HasValue
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

    private static void SetAxisNeutralState(ScottPlot.IYAxis axis) {
        SetAxisVisibility(axis, true);

        var neutralColor = ScottPlot.Color.FromColor(Color.White);
        axis.Label.Text = string.Empty;
        axis.Label.ForeColor = neutralColor;
        axis.TickLabelStyle.ForeColor = neutralColor;
        axis.FrameLineStyle.Color = neutralColor;
        axis.MajorTickStyle.Color = neutralColor;
        axis.MinorTickStyle.Color = neutralColor;
    }

    private static void SetAxisVisibility(object axis, bool isVisible) {
        var property = axis.GetType().GetProperty("IsVisible");
        if (property?.CanWrite == true && property.PropertyType == typeof(bool)) {
            property.SetValue(axis, isVisible);
        }
    }

    private void ApplyThemeColors() {
        if (_plot == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        if (isLight) {
            _plot.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.Axes.Color(ScottPlot.Color.FromColor(Color.Black));
            _plot.Plot.Legend.BackgroundColor = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.Legend.FontColor = ScottPlot.Color.FromColor(Color.Black);
            _plot.Plot.Legend.OutlineColor = ScottPlot.Color.FromColor(Color.LightGray);
        } else {
            _plot.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(Color.FromArgb(35, 47, 52));
            _plot.Plot.DataBackground.Color = ScottPlot.Color.FromColor(Color.FromArgb(35, 47, 52));
            _plot.Plot.Axes.Color(ScottPlot.Color.FromColor(Color.White));
            _plot.Plot.Legend.BackgroundColor = ScottPlot.Color.FromColor(Color.FromArgb(46, 61, 66));
            _plot.Plot.Legend.FontColor = ScottPlot.Color.FromColor(Color.White);
            _plot.Plot.Legend.OutlineColor = ScottPlot.Color.FromColor(Color.FromArgb(80, 102, 110));
        }

        // Axes.Color() resets ALL axes to the same color,
        // so re-apply per-metric colors when multi-axis mode is active.
        ReapplyAxisColors();
    }

    /// <summary>
    /// Applies correct theme colors to all child controls of the tile.
    /// Called from UpdateTheme and OnFontChanged to fight
    /// MaterialSkinManager overwriting our colors.
    /// </summary>
    private void ApplyTileColors() {
        if (_titleLabel == null) {
            return;
        }

        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var tileBg = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        var fg = isLight ? Color.Black : Color.White;

        if (BackColor != tileBg) {
            BackColor = tileBg;
        }

        _titleLabel.ForeColor = fg;
        if (_topPanel != null && _topPanel.BackColor != tileBg) {
            _topPanel.BackColor = tileBg;
        }
        if (_periodSelector != null) {
            var comboBg = isLight ? Color.White : Color.FromArgb(46, 61, 66);
            if (_periodSelector.BackColor != comboBg) {
                _periodSelector.BackColor = comboBg;
            }

            _periodSelector.ForeColor = fg;
            _periodSelector.Invalidate();
        }
    }

    private void ApplyPlotContextMenuTheme() {
        if (_plotContextMenu == null) {
            return;
        }

        ApplyPlotContextMenuTheme(_plotContextMenu);
    }

    private void ApplyPlotContextMenuTheme(ContextMenuStrip contextMenu) {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var menuBackground = isLight ? Color.White : Color.FromArgb(38, 52, 57);
        var menuText = isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);
        var menuIconColor = isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);

        _contextMenuRenderer.UpdateTheme();

        contextMenu.RenderMode = ToolStripRenderMode.Professional;
        contextMenu.Renderer = _contextMenuRenderer;
        contextMenu.ShowImageMargin = true;
        contextMenu.BackColor = menuBackground;
        contextMenu.ForeColor = menuText;
        contextMenu.ImageScalingSize = new Size(MenuIconSize, MenuIconSize);

        ApplyContextMenuItemColors(contextMenu.Items, menuBackground, menuText);
        UpdateContextMenuIcons(contextMenu.Items, menuIconColor);
    }

    private ContextMenuStrip CreatePlotContextMenu() {
        var contextMenu = new ContextMenuStrip {
            ShowImageMargin = true,
            ImageScalingSize = new Size(MenuIconSize, MenuIconSize)
        };

        contextMenu.Items.Add(CreatePlotMenuItem("Save Image", "save", HandleSaveImageClick));
        contextMenu.Items.Add(CreatePlotMenuItem("Copy to Clipboard", "copy", HandleCopyImageClick));
        contextMenu.Items.Add(CreatePlotMenuItem("Autoscale", "refresh", HandleAutoscaleClick));
        contextMenu.Items.Add(CreatePlotMenuItem("Open in New Window", "folder", HandleOpenInWindowClick));

        contextMenu.Opening += (_, _) => ApplyPlotContextMenuTheme(contextMenu);
        return contextMenu;
    }

    private static ToolStripMenuItem CreatePlotMenuItem(string text, string iconName, EventHandler onClick) {
        var iconColor = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT
            ? Color.FromArgb(33, 33, 33)
            : Color.FromArgb(240, 240, 240);

        var item = new ToolStripMenuItem(text) {
            // Keep classic image+text mode for platforms where image margin works,
            // and also include an ASCII marker in text so item origin is always visible.
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize),
            ImageScaling = ToolStripItemImageScaling.None,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            Tag = iconName
        };

        item.Click += onClick;
        return item;
    }

    private static void UpdateContextMenuIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is not ToolStripMenuItem menuItem) {
                continue;
            }

            if (menuItem.Tag is string iconName && !string.IsNullOrWhiteSpace(iconName)) {
                menuItem.Image?.Dispose();
                menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize);
            }

            if (menuItem.DropDownItems.Count > 0) {
                UpdateContextMenuIcons(menuItem.DropDownItems, iconColor);
            }
        }
    }

    private void HandleSaveImageClick(object? sender, EventArgs e) {
        if (_plot == null) {
            return;
        }

        using var dialog = new SaveFileDialog {
            Filter = "PNG Image|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = $"{_config.Title}.png"
        };

        if (dialog.ShowDialog() != DialogResult.OK) {
            return;
        }

        using var bmp = CapturePlotBitmap();
        bmp?.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
    }

    private void HandleCopyImageClick(object? sender, EventArgs e) {
        using var bmp = CapturePlotBitmap();
        if (bmp != null) {
            Clipboard.SetImage((Bitmap)bmp.Clone());
        }
    }

    private void HandleAutoscaleClick(object? sender, EventArgs e) {
        if (_plot == null) {
            return;
        }

        _plot.Plot.Axes.AutoScale();
        _plot.Refresh();
    }

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

    private Bitmap? CapturePlotBitmap() {
        if (_plot == null || _plot.Width <= 0 || _plot.Height <= 0) {
            return null;
        }

        var bmp = new Bitmap(_plot.Width, _plot.Height);
        _plot.DrawToBitmap(bmp, new Rectangle(Point.Empty, _plot.Size));
        return bmp;
    }

    private void Plot_MouseUp(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Right || _plot == null || _plotContextMenu == null) {
            return;
        }

        ApplyPlotContextMenuTheme();
        _plotContextMenu.Show(_plot, e.Location);
    }

    private void TryDisableScottPlotBuiltInContextMenu() {
        if (_plot == null) {
            return;
        }

        try {
            var target = _plot as object;
            // Best-effort compatibility across ScottPlot versions:
            // disable any bool menu-related switch and clear menu-like object properties.
            DisableMenuMembersRecursive(target, 0);
        } catch {
            // no-op (feature is best effort)
        }
    }

    private static void DisableMenuMembersRecursive(object target, int depth) {
        if (depth > 2) {
            return;
        }

        var type = target.GetType();
        foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) {
            if (!prop.CanRead) {
                continue;
            }

            var name = prop.Name.ToLowerInvariant();
            object? value;
            try {
                value = prop.GetValue(target);
            } catch {
                continue;
            }

            if (prop.CanWrite) {
                try {
                    if (prop.PropertyType == typeof(bool) && (name.Contains("menu") || name.Contains("context"))) {
                        prop.SetValue(target, false);
                    } else if (!prop.PropertyType.IsValueType && (name.Contains("menu") || name.Contains("context"))) {
                        prop.SetValue(target, null);
                    }
                } catch {
                    // ignore setter failures
                }
            }

            if (value == null) {
                continue;
            }

            var valueType = value.GetType();
            if (valueType.Namespace != null && valueType.Namespace.StartsWith("ScottPlot", StringComparison.Ordinal)) {
                DisableMenuMembersRecursive(value, depth + 1);
            }
        }
    }

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
    /// Removes any extra axes added during the previous RefreshData call.
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

    private void InitializeUI() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var tileBg = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        var fg = isLight ? Color.Black : Color.White;

        _topPanel = new Panel {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(5),
            BackColor = tileBg
        };

        _titleLabel = new Label {
            Text = _config.Title,
            Dock = DockStyle.Fill,
            Font = CreateSafeFont("Roboto", 11, System.Drawing.FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0),
            ForeColor = fg
        };

        _periodSelector = new ComboBox {
            Dock = DockStyle.Right,
            Width = 120,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66),
            ForeColor = fg
        };
        _periodSelector.DrawItem += PeriodSelector_DrawItem;
        LoadPeriodPresets();
        SetSelectedPeriodPreset();
        _periodSelector.SelectionChangeCommitted += (s, e) => UpdatePeriodFromSelection();
        ChartPeriodPresetStore.PresetsChanged += HandlePresetsChanged;
        MetricAxisRuleStore.RulesChanged += HandleAxisRulesChanged;

        _topPanel.Controls.Add(_titleLabel);
        _topPanel.Controls.Add(_periodSelector);

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
        _periodSelector.BackColorChanged += (s, e) => {
            var sm = MaterialSkinManager.Instance;
            var light = sm.Theme == MaterialSkinManager.Themes.LIGHT;
            var expected = light ? Color.White : Color.FromArgb(46, 61, 66);
            if (_periodSelector.BackColor != expected) {
                _periodSelector.BackColor = expected;
            }
        };

        _plot = new FormsPlot {
            Dock = DockStyle.Fill,         // Use explicit safe font (Segoe UI is guaranteed on Windows)
            Font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular)
        };
        _plotContextMenu = CreatePlotContextMenu();
        _plot.ContextMenuStrip = null;
        _plot.MouseUp += Plot_MouseUp;
        TryDisableScottPlotBuiltInContextMenu();
        ApplyPlotContextMenuTheme();

        // Add _topPanel first (it's safe)
        Controls.Add(_topPanel);

        // Use BeginInvoke to defer adding FormsPlot until message loop is ready
        // This avoids GDI+ font errors during auto-scaling
        BeginInvoke(() => {
            Controls.Add(_plot);
            Controls.SetChildIndex(_plot, 0); // Move to back (Fill dock)
            UpdateTheme();
        });
    }
    /// <summary>
    /// Owner-draw handler for ComboBox. WinForms ComboBox with DropDownList
    /// ignores BackColor for the main field — we must paint it ourselves.
    /// </summary>
    private void PeriodSelector_DrawItem(object? sender, DrawItemEventArgs e) {
        if (e.Index < 0 || _periodSelector == null) {
            return;
        }

        var bg = _periodSelector.BackColor;
        var fg = _periodSelector.ForeColor;

        // Use highlight colors for selected item in dropdown
        if ((e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0) {
            bg = SystemColors.Highlight;
            fg = SystemColors.HighlightText;
        }

        using var bgBrush = new SolidBrush(bg);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        var text = _periodSelector.Items[e.Index]?.ToString() ?? "";
        using var fgBrush = new SolidBrush(fg);
        e.Graphics.DrawString(text, e.Font ?? _periodSelector.Font, fgBrush, e.Bounds);
    }

    private void ApplyAxisRules(Dictionary<MetricType, ScottPlot.IYAxis> axisMap) {
        if (_plot == null) {
            return;
        }

        foreach (var (metric, yAxis) in axisMap) {
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

            // MinimumBoundary вЂ” prevent panning/zooming below this Y value
            if (rule.MinBoundary.HasValue && !rule.MaxBoundary.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumBoundary(xAxis, yAxis,
                    new ScottPlot.AxisLimits(
                        double.NegativeInfinity, double.PositiveInfinity,
                        rule.MinBoundary.Value, double.PositiveInfinity)));
            }

            // MaximumBoundary вЂ” prevent panning/zooming above this Y value
            if (rule.MaxBoundary.HasValue && !rule.MinBoundary.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumBoundary(xAxis, yAxis,
                    new ScottPlot.AxisLimits(
                        double.NegativeInfinity, double.PositiveInfinity,
                        double.NegativeInfinity, rule.MaxBoundary.Value)));
            }

            // MaximumSpan вЂ” limit how far apart Y axis limits can be (limits zoom-out)
            if (rule.MaxSpan.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MaximumSpan(xAxis, yAxis, double.PositiveInfinity, rule.MaxSpan.Value));
            }

            // MinimumSpan вЂ” minimum Y-axis range (limits zoom-in)
            if (rule.MinSpan.HasValue) {
                _plot.Plot.Axes.Rules.Add(
                new ScottPlot.AxisRules.MinimumSpan(xAxis, yAxis, 0, rule.MinSpan.Value));
            }
        }
    }

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

    private void SetSelectedPeriodPreset() {
        if (_periodSelector == null) {
            return;
        }

        var index = ChartPeriodPresetStore.FindMatchingPresetIndex(
            _config.CustomPeriodDuration, _config.Period, _periodPresets);
        if (index >= 0) {
            _periodSelector.SelectedIndex = index;
            return;
        }

        if (_config.CustomPeriodDuration.HasValue) {
            var label = $"Custom ({ChartPeriodPresetStore.FormatDuration(_config.CustomPeriodDuration.Value)})";
            _periodPresets.Add(new ChartPeriodPreset(label, _config.CustomPeriodDuration.Value, ChartPeriod.Custom));
            _periodSelector.Items.Add(label);
            _periodSelector.SelectedIndex = _periodPresets.Count - 1;
        } else if (_periodSelector.Items.Count > 0) {
            _periodSelector.SelectedIndex = 0;
        }
    }

    private void UpdatePeriodFromSelection() {
        if (_periodSelector == null || _suppressPeriodChange) {
            return;
        }

        var index = _periodSelector.SelectedIndex;
        if (index < 0 || index >= _periodPresets.Count) {
            return;
        }

        var preset = _periodPresets[index];
        _config.Period = preset.Period;
        _config.CustomPeriodDuration = preset.Period == ChartPeriod.Custom ? preset.Duration : null;
        RefreshData();
        PeriodChanged?.Invoke(this, _config.Period, _config.CustomPeriodDuration);
    }

    /// <summary>
    /// When multiple Y axes are active, re-applies the per-metric color to each axis
    /// so they remain visually distinguishable after a global theme color reset.
    /// </summary>
    private void ReapplyAxisColors() {
        if (_plot == null) {
            return;
        }

        var distinctMetrics = _config.MetricAggregations
            .Select(a => a.Metric)
            .Distinct()
            .ToList();
        var isLightTheme = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;

        for (int i = 0; i < distinctMetrics.Count; i++) {
            var metric = distinctMetrics[i];
            var representativeColor = ScottPlot.Color.FromColor(
                _config.MetricAggregations.First(a => a.Metric == metric).GetColorForTheme(isLightTheme));

            ScottPlot.IYAxis? axis = null;
            if (i == 0) {
                axis = _plot.Plot.Axes.Left;
            } else if (i == 1) {
                axis = _plot.Plot.Axes.Right;
            } else if (i - 2 < _extraAxes.Count) {
                axis = _extraAxes[i - 2];
            }

            if (axis == null) {
                continue;
            }

            StyleAxis(axis, BuildAxisLabel(metric), representativeColor);
        }
    }

    #endregion Private Methods
}
