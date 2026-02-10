using MaterialSkin;
using SafetyMonitorView.Forms;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ScottPlot.WinForms;
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;

namespace SafetyMonitorView.Controls;

public class ChartTile : Panel {

    #region Private Fields

    private readonly ChartTileConfig _config;
    private readonly DataService _dataService;
    private readonly List<ScottPlot.IAxis> _extraAxes = [];
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

        // Determine distinct metric types to decide if multiple Y axes are needed
        var distinctMetrics = _config.MetricAggregations
            .Select(a => a.Metric)
            .Distinct()
            .ToList();

        bool useMultipleAxes = distinctMetrics.Count > 1;
        var axisMap = new Dictionary<MetricType, ScottPlot.IYAxis>();

        if (useMultipleAxes) {
            for (int i = 0; i < distinctMetrics.Count; i++) {
                var metric = distinctMetrics[i];
                var labelText = BuildAxisLabel(metric);

                // Use the color of the first series for this metric to tint the axis
                var representativeColor = ScottPlot.Color.FromColor(
                    _config.MetricAggregations.First(a => a.Metric == metric).Color);

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
                _config.MetricAggregations.First(a => a.Metric == metric).Color);
            StyleAxis(_plot.Plot.Axes.Left, labelText, representativeColor);
        }

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
            scatter.LegendText = agg.Label;
            scatter.Color = ScottPlot.Color.FromColor(agg.Color);
            scatter.LineWidth = agg.LineWidth;
            scatter.MarkerSize = agg.ShowMarkers ? 5 : 0;
            scatter.Smooth = agg.Smooth;

            // Assign this series to its dedicated Y axis when multiple axes are active
            if (useMultipleAxes && axisMap.TryGetValue(agg.Metric, out var yAxis)) {
                scatter.Axes.YAxis = yAxis;
            }
        }

        _plot.Plot.Axes.DateTimeTicksBottom();
        _plot.Plot.Axes.AutoScale();
        ApplyThemeColors();
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
        _plot.Refresh();
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
            DetachPlotContextMenu();
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
        if (_plot?.ContextMenuStrip == null) {
            return;
        }

        AttachPlotContextMenu(_plot.ContextMenuStrip);
        ApplyPlotContextMenuTheme(_plot.ContextMenuStrip);
    }

    private void ApplyPlotContextMenuTheme(ContextMenuStrip contextMenu) {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        var menuBackground = isLight ? Color.White : Color.FromArgb(38, 52, 57);
        var menuText = isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);

        _contextMenuRenderer.UpdateTheme();

        contextMenu.RenderMode = ToolStripRenderMode.Professional;
        contextMenu.Renderer = _contextMenuRenderer;
        contextMenu.ShowImageMargin = true;
        contextMenu.BackColor = menuBackground;
        contextMenu.ForeColor = menuText;

        ApplyContextMenuItemColors(contextMenu.Items, menuBackground, menuText);
        ApplyContextMenuIcons(contextMenu.Items, menuText);
    }

    private void AttachPlotContextMenu(ContextMenuStrip contextMenu) {
        if (ReferenceEquals(_plotContextMenu, contextMenu)) {
            return;
        }

        DetachPlotContextMenu();
        _plotContextMenu = contextMenu;
        _plotContextMenu.Opening += PlotContextMenu_Opening;
    }

    private void DetachPlotContextMenu() {
        if (_plotContextMenu == null) {
            return;
        }

        _plotContextMenu.Opening -= PlotContextMenu_Opening;
        _plotContextMenu = null;
    }

    private void PlotContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e) {
        if (sender is ContextMenuStrip contextMenu) {
            ApplyPlotContextMenuTheme(contextMenu);
        }
    }

    private static void ApplyContextMenuIcons(ToolStripItemCollection items, Color iconColor) {
        foreach (ToolStripItem item in items) {
            if (item is not ToolStripMenuItem menuItem) {
                continue;
            }

            if (menuItem.DropDown is ToolStripDropDownMenu dropDownMenu) {
                dropDownMenu.ShowImageMargin = true;
            }

            var iconName = GetContextMenuIconName(menuItem.Text);
            if (!string.IsNullOrEmpty(iconName)) {
                menuItem.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                menuItem.Image?.Dispose();
                menuItem.Image = MaterialIcons.GetIcon(iconName, iconColor, MenuIconSize);
            }

            if (menuItem.DropDownItems.Count > 0) {
                ApplyContextMenuIcons(menuItem.DropDownItems, iconColor);
            }
        }
    }

    private static string GetContextMenuIconName(string? menuText) {
        if (string.IsNullOrWhiteSpace(menuText)) {
            return string.Empty;
        }

        var normalizedText = menuText.ToLowerInvariant();
        return normalizedText switch {
            var t when t.Contains("copy") => "copy",
            var t when t.Contains("save") => "save",
            var t when t.Contains("open") => "folder",
            var t when t.Contains("help") => "help",
            var t when t.Contains("reset") => "refresh",
            var t when t.Contains("autoscale") => "refresh",
            _ => string.Empty
        };
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
        _plot.ContextMenuStripChanged += (s, e) => {
            if (_plot?.ContextMenuStrip != null) {
                AttachPlotContextMenu(_plot.ContextMenuStrip);
            } else {
                DetachPlotContextMenu();
            }
            ApplyPlotContextMenuTheme();
        };

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
        _periodPresets = ChartPeriodPresetStore.GetPresetItems().ToList();
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

        for (int i = 0; i < distinctMetrics.Count; i++) {
            var metric = distinctMetrics[i];
            var representativeColor = ScottPlot.Color.FromColor(
                _config.MetricAggregations.First(a => a.Metric == metric).Color);

            ScottPlot.IYAxis? axis = null;
            if (i == 0) {
                axis = _plot.Plot.Axes.Left;
            } else if (i == 1) {
                axis = _plot.Plot.Axes.Right;
            } else if (i - 2 < _extraAxes.Count) {
                axis = _extraAxes[i - 2] as ScottPlot.IYAxis;
            }

            if (axis == null) {
                continue;
            }

            StyleAxis(axis, BuildAxisLabel(metric), representativeColor);
        }
    }

    #endregion Private Methods
}
