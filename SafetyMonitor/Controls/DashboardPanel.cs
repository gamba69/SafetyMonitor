using SafetyMonitor.Forms;
using SafetyMonitor.Models;
using SafetyMonitor.Services;

namespace SafetyMonitor.Controls;

public class DashboardPanel : TableLayoutPanel {

    #region Private Fields

    private readonly Dashboard _dashboard;
    private readonly DataService _dataService;
    private readonly Dictionary<Guid, Control> _tileControls = [];
    private DashboardChartLinkMode _chartLinkMode;
    private int _chartStaticModeTimeoutSeconds;
    private double _chartStaticAggregationPresetMatchTolerancePercent;
    private int _chartStaticAggregationTargetPointCount;
    private bool _tilesCreated;
    private bool _resetLinkStatePendingUntilTilesCreated;
    private readonly HashSet<ChartTile> _hoveredChartTiles = [];
    private readonly ThemedMenuRenderer _contextMenuRenderer = new();
    private ContextMenuStrip? _contextMenu;
    private const int MenuIconSize = 22;

    #endregion Private Fields

    #region Public Constructors

    public event Action? DashboardChanged;
    public event Action? DashboardResetToDefaultRequested;
    public event Action<TileConfig>? TileEditRequested;
    public event Action? DashboardEditRequested;

    public DashboardPanel(
        Dashboard dashboard,
        DataService dataService,
        int chartStaticModeTimeoutSeconds,
        double chartStaticAggregationPresetMatchTolerancePercent,
        int chartStaticAggregationTargetPointCount) {
        _dashboard = dashboard;
        _dataService = dataService;
        _chartStaticModeTimeoutSeconds = chartStaticModeTimeoutSeconds;
        _chartStaticAggregationPresetMatchTolerancePercent = Math.Clamp(chartStaticAggregationPresetMatchTolerancePercent, 0, 100);
        _chartStaticAggregationTargetPointCount = Math.Max(2, chartStaticAggregationTargetPointCount);
        // Set a valid font to prevent GDI+ errors when child controls inherit font
        Font = SystemFonts.DefaultFont;
        InitializeUI();
    }

    #endregion Public Constructors

    #region Public Methods

    public void RefreshData() {
        SuspendLayout();
        try {
            using var valueTileSnapshot = _dataService.BeginValueTileSnapshot();
            using var chartDataSnapshot = _dataService.BeginChartDataSnapshot();

            foreach (var control in _tileControls.Values) {
                if (!control.Visible) {
                    control.Visible = true;
                }
            }

            foreach (var control in _tileControls.Values) {
                if (control is ValueTile vt) {
                    vt.RefreshData();
                } else if (control is ChartTile ct) {
                    ct.RefreshData();
                }
            }
        } finally {
            ResumeLayout(true);
        }
    }

    /// <summary>
    /// Asynchronous version of <see cref="RefreshData"/>.
    /// Heavy database queries are executed on a background thread to keep the UI
    /// responsive; the results are then applied on the UI thread via the existing
    /// synchronous <c>RefreshData</c> methods of individual tiles which now hit
    /// the warm <see cref="DataService"/> snapshot caches.
    /// </summary>
    public async Task RefreshDataAsync(CancellationToken cancellationToken = default) {
        // Start snapshot scopes — they enable cross-tile caching in DataService.
        using var valueTileSnapshot = _dataService.BeginValueTileSnapshot();
        using var chartDataSnapshot = _dataService.BeginChartDataSnapshot();

        // Collect data requirements on UI thread (reads tile state).
        var hasValueTiles = _tileControls.Values.OfType<ValueTile>().Any();
        var chartRequirements = _tileControls.Values
            .OfType<ChartTile>()
            .SelectMany(ct => ct.GetDataFetchRequirements())
            .ToList();

        // Pre-fetch data on a background thread (populates DataService snapshot caches).
        await Task.Run(() => {
            if (hasValueTiles) {
                _dataService.GetLatestData();
            }

            foreach (var (period, start, end, duration, interval, function) in chartRequirements) {
                cancellationToken.ThrowIfCancellationRequested();
                _dataService.GetChartData(period, start, end, duration, interval, function);
            }
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Apply data on the UI thread (hits warm caches — near-instant).
        SuspendLayout();
        try {
            foreach (var control in _tileControls.Values) {
                if (!control.Visible) {
                    control.Visible = true;
                }
            }

            foreach (var control in _tileControls.Values) {
                if (control is ValueTile vt) {
                    vt.RefreshData();
                } else if (control is ChartTile ct) {
                    ct.RefreshData();
                }
            }
        } finally {
            ResumeLayout(true);
        }
    }

    public void SetChartLinkMode(DashboardChartLinkMode linkMode) {
        _chartLinkMode = linkMode;
        _hoveredChartTiles.Clear();

        if (_chartLinkMode == DashboardChartLinkMode.Disabled) {
            foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
                chartTile.ClearInspectorDisplay();
            }
        }
    }

    public void ResetLinkStateToDashboardDefaults() {
        _dashboard.EnsureLinkGroupPeriodDefaults();
        SetChartLinkMode(_dashboard.InitialChartLinkMode);

        if (!_tilesCreated || _tileControls.Count == 0) {
            _resetLinkStatePendingUntilTilesCreated = true;
            return;
        }

        ApplyChartRuntimeDefaults(refreshData: true);
    }

    public void SetChartStaticModeTimeoutSeconds(int seconds) {
        _chartStaticModeTimeoutSeconds = Math.Max(10, seconds);
        foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
            chartTile.SetStaticModeTimeout(TimeSpan.FromSeconds(_chartStaticModeTimeoutSeconds));
        }
    }

    public void SetChartStaticAggregationOptions(double presetMatchTolerancePercent, int targetPointCount) {
        _chartStaticAggregationPresetMatchTolerancePercent = Math.Clamp(presetMatchTolerancePercent, 0, 100);
        _chartStaticAggregationTargetPointCount = Math.Max(2, targetPointCount);

        foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
            chartTile.SetStaticAggregationSettings(
                _chartStaticAggregationPresetMatchTolerancePercent,
                _chartStaticAggregationTargetPointCount);
        }
    }


    public void UpdateTheme() {
        var skinManager = MaterialSkin.MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkin.MaterialSkinManager.Themes.LIGHT;
        BackColor = isLight ? Color.FromArgb(245, 245, 245) : Color.FromArgb(25, 36, 40);
        foreach (var control in _tileControls.Values) {
            if (control is ValueTile vt) {
                vt.UpdateTheme();
            } else if (control is ChartTile ct) {
                ct.UpdateTheme();
            }
        }
        Invalidate(true);
    }

    #endregion Public Methods

    #region Protected Methods

    // Защита от изменения шрифтов MaterialSkinManager - передаем на тайлы
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        // Принудительно восстанавливаем шрифты на всех тайлах
        foreach (var control in _tileControls.Values) {
            if (control is ValueTile vt) {
                vt.UpdateTheme();
            } else if (control is ChartTile ct) {
                ct.UpdateTheme();
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        // Create tiles only after handle is created (GDI+ is ready)
        if (!_tilesCreated) {
            _tilesCreated = true;
            CreateTiles();
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private void ApplyChartRuntimeDefaults(bool refreshData) {
        foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
            if (!chartTile.IsHandleCreated) {
                chartTile.CreateControl();
            }

            chartTile.SetAvailableLinkGroups(_dashboard.UsedLinkGroups);
            chartTile.SetLinkGroupPeriodShortNames(GetLinkGroupPeriodShortNames());
            var presetUid = _dashboard.GetLinkGroupPeriodPresetUid(chartTile.Config.LinkGroup);
            chartTile.ExitStaticMode(raiseEvents: false);
            chartTile.SetStaticPaused(false, raiseEvents: false);
            chartTile.SetPeriodPreset(presetUid, refreshData: false);
        }

        if (refreshData) {
            RefreshData();
        }
    }

    private void CreateTiles() {
        foreach (var tileConfig in _dashboard.Tiles) {
            Control? tileControl = tileConfig switch {
                ValueTileConfig vtc => new ValueTile(vtc, _dataService),
                ChartTileConfig ctc => new ChartTile(ctc, _dataService),
                _ => null
            };
            if (tileControl != null) {
                tileControl.Visible = false;

                if (tileControl is ValueTile valueTile) {
                    valueTile.EditDashboardRequested += OnTileDashboardEditRequested;
                    valueTile.EditRequested += OnValueTileEditRequested;
                    valueTile.ViewSettingsChanged += OnValueTileViewSettingsChanged;
                }

                if (tileControl is ChartTile chartTile) {
                    chartTile.SetAvailableLinkGroups(_dashboard.UsedLinkGroups);
                    chartTile.SetLinkGroupPeriodShortNames(GetLinkGroupPeriodShortNames());
                    chartTile.PeriodChanged += OnChartPeriodChanged;
                    chartTile.StaticRangeChanged += OnChartStaticRangeChanged;
                    chartTile.AutoModeRestored += OnChartAutoModeRestored;
                    chartTile.ViewSettingsChanged += OnChartViewSettingsChanged;
                    chartTile.InspectorToggled += OnChartInspectorToggled;
                    chartTile.PlotHoverPresenceChanged += OnPlotHoverPresenceChanged;
                    chartTile.HoverAnchorChanged += OnChartHoverAnchorChanged;
                    chartTile.StaticPauseChanged += OnChartStaticPauseChanged;
                    chartTile.LinkGroupChanged += OnChartLinkGroupChanged;
                    chartTile.EditDashboardRequested += OnTileDashboardEditRequested;
                    chartTile.SetStaticModeTimeout(TimeSpan.FromSeconds(_chartStaticModeTimeoutSeconds));
                    chartTile.SetStaticAggregationSettings(
                        _chartStaticAggregationPresetMatchTolerancePercent,
                        _chartStaticAggregationTargetPointCount);
                    chartTile.EditRequested += OnChartTileEditRequested;
                }
                Controls.Add(tileControl, tileConfig.Column, tileConfig.Row);
                SetColumnSpan(tileControl, tileConfig.ColumnSpan);
                SetRowSpan(tileControl, tileConfig.RowSpan);
                _tileControls[tileConfig.Id] = tileControl;
            }
        }

        if (_resetLinkStatePendingUntilTilesCreated) {
            _resetLinkStatePendingUntilTilesCreated = false;
            ApplyChartRuntimeDefaults(refreshData: false);
        }
    }

    private Dictionary<ChartLinkGroup, string> GetLinkGroupPeriodShortNames() {
        var result = new Dictionary<ChartLinkGroup, string>();
        foreach (var group in ChartLinkGroupInfo.All) {
            result[group] = _dashboard.GetLinkGroupPeriodShortName(group);
        }

        return result;
    }

    private void OnChartPeriodChanged(ChartTile source, string periodPresetUid) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled) {
            return;
        }

        foreach (var chartTile in GetLinkedChartTiles(source)) {
            if (ReferenceEquals(chartTile, source)) {
                continue;
            }

            chartTile.SetPeriodPreset(periodPresetUid);
        }

    }



    private void OnChartStaticRangeChanged(ChartTile source, DateTime start, DateTime end) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled) {
            return;
        }

        foreach (var chartTile in GetLinkedChartTiles(source)) {
            if (ReferenceEquals(chartTile, source)) {
                continue;
            }

            chartTile.SetStaticRange(start, end, raiseEvents: false);
        }

    }

    private void OnChartAutoModeRestored(ChartTile source) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled) {
            return;
        }

        foreach (var chartTile in GetLinkedChartTiles(source)) {
            if (ReferenceEquals(chartTile, source)) {
                continue;
            }

            chartTile.ExitStaticMode(raiseEvents: false);
        }

    }

    private void OnChartStaticPauseChanged(ChartTile source, bool paused) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled) {
            return;
        }

        foreach (var chartTile in GetLinkedChartTiles(source)) {
            if (ReferenceEquals(chartTile, source)) {
                continue;
            }

            chartTile.SetStaticPaused(paused, raiseEvents: false);
        }

    }

    private void OnChartViewSettingsChanged(ChartTile source) {
        DashboardChanged?.Invoke();
    }

    private void OnChartLinkGroupChanged(ChartTile source) {
        DashboardChanged?.Invoke();
        DashboardResetToDefaultRequested?.Invoke();
    }


    private void OnChartInspectorToggled(ChartTile source, bool enabled) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled) {
            return;
        }

        foreach (var chartTile in GetLinkedChartTiles(source)) {
            if (ReferenceEquals(chartTile, source)) {
                continue;
            }

            chartTile.SetInspectorEnabled(enabled, raiseEvents: false);
        }

        if (!enabled) {
            _hoveredChartTiles.Clear();
            foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
                chartTile.ClearInspectorDisplay();
            }
        }

    }

    private void OnPlotHoverPresenceChanged(ChartTile source, bool isHovered) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled || !source.IsInspectorEnabled) {
            if (!isHovered) {
                source.ClearInspectorDisplay();
            }
            return;
        }

        if (isHovered) {
            _hoveredChartTiles.Add(source);
            return;
        }

        _hoveredChartTiles.Remove(source);
        if (_hoveredChartTiles.Count > 0) {
            return;
        }

        foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
            chartTile.ClearInspectorDisplay();
        }
    }

    private void OnChartHoverAnchorChanged(ChartTile source, double x) {
        if (_chartLinkMode == DashboardChartLinkMode.Disabled || !source.IsInspectorEnabled) {
            return;
        }

        // MouseEnter is not guaranteed when the pointer is already over the plot
        // after runtime state changes, so treat anchor updates as active hover.
        _hoveredChartTiles.Add(source);

        foreach (var chartTile in GetLinkedChartTiles(source)) {
            if (!chartTile.IsInspectorEnabled) {
                continue;
            }

            chartTile.ShowInspectorAt(x);
        }
    }

    private IEnumerable<ChartTile> GetLinkedChartTiles(ChartTile source) {
        var allCharts = _tileControls.Values.OfType<ChartTile>();
        return _chartLinkMode switch {
            DashboardChartLinkMode.Full => allCharts,
            DashboardChartLinkMode.Grouped when _dashboard.UsedLinkGroups > 1 => allCharts.Where(tile => tile.Config.LinkGroup == source.Config.LinkGroup),
            DashboardChartLinkMode.Grouped => allCharts,
            _ => []
        };
    }

    private void OnChartTileEditRequested(ChartTile source) {
        var config = _dashboard.Tiles.OfType<ChartTileConfig>().FirstOrDefault(c =>
            _tileControls.TryGetValue(c.Id, out var ctrl) && ReferenceEquals(ctrl, source));
        if (config != null) {
            TileEditRequested?.Invoke(config);
        }
    }

    private void OnTileDashboardEditRequested(ValueTile source) {
        DashboardEditRequested?.Invoke();
    }

    private void OnTileDashboardEditRequested(ChartTile source) {
        DashboardEditRequested?.Invoke();
    }

    private void OnValueTileEditRequested(ValueTile source) {
        var config = _dashboard.Tiles.OfType<ValueTileConfig>().FirstOrDefault(c =>
            _tileControls.TryGetValue(c.Id, out var ctrl) && ReferenceEquals(ctrl, source));
        if (config != null) {
            TileEditRequested?.Invoke(config);
        }
    }

    private void OnValueTileViewSettingsChanged(ValueTile source) {
        DashboardChanged?.Invoke();
    }

    private void InitializeUI() {
        Dock = DockStyle.Fill;
        ColumnCount = _dashboard.Columns;
        RowCount = _dashboard.Rows;
        // Setup equal column/row sizes
        ColumnStyles.Clear();
        RowStyles.Clear();
        for (int i = 0; i < _dashboard.Columns; i++) {
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / _dashboard.Columns));
        }
        for (int i = 0; i < _dashboard.Rows; i++) {
            RowStyles.Add(new RowStyle(SizeType.Percent, 100f / _dashboard.Rows));
        }

        _contextMenu = CreateContextMenu();
        ContextMenuStrip = _contextMenu;
    }

    private ContextMenuStrip CreateContextMenu() {
        var contextMenu = new ContextMenuStrip {
            ShowImageMargin = true,
            ImageScalingSize = new Size(MenuIconSize, MenuIconSize),
            Cursor = Cursors.Hand
        };

        contextMenu.Opening += (_, _) => {
            var iconColor = MaterialSkin.MaterialSkinManager.Instance.Theme == MaterialSkin.MaterialSkinManager.Themes.LIGHT
                ? Color.FromArgb(33, 33, 33)
                : Color.FromArgb(240, 240, 240);
            if (contextMenu.Items.Count > 0 && contextMenu.Items[0] is ToolStripMenuItem item) {
                item.Image = MaterialIcons.GetIcon(MaterialIcons.CommonEdit, iconColor, MenuIconSize, IconRenderPreset.DarkOutlined);
            }
            _contextMenuRenderer.UpdateTheme();
            contextMenu.RenderMode = ToolStripRenderMode.Professional;
            contextMenu.Renderer = _contextMenuRenderer;
        };

        var menuItem = new ToolStripMenuItem("Edit Dashboard…") {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleLeft,
            ImageScaling = ToolStripItemImageScaling.None
        };
        menuItem.Click += (_, _) => DashboardEditRequested?.Invoke();
        contextMenu.Items.Add(menuItem);
        InteractiveCursorStyler.Apply(contextMenu.Items);
        return contextMenu;
    }

    #endregion Private Methods

}
