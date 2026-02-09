using SafetyMonitorView.Models;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Controls;

public class DashboardPanel : TableLayoutPanel {

    #region Private Fields

    private readonly Dashboard _dashboard;
    private readonly DataService _dataService;
    private readonly Dictionary<Guid, Control> _tileControls = [];
    private bool _linkChartPeriods;
    private bool _tilesCreated;

    #endregion Private Fields

    #region Public Constructors

    public DashboardPanel(Dashboard dashboard, DataService dataService) {
        _dashboard = dashboard;
        _dataService = dataService;
        // Set a valid font to prevent GDI+ errors when child controls inherit font
        Font = SystemFonts.DefaultFont;
        InitializeUI();
    }

    #endregion Public Constructors

    #region Public Methods

    public void RefreshData() {
        foreach (var control in _tileControls.Values) {
            if (control is ValueTile vt) {
                vt.RefreshData();
            } else if (control is ChartTile ct) {
                ct.RefreshData();
            }
        }
    }
    public void SetLinkChartPeriods(bool linkChartPeriods) {
        _linkChartPeriods = linkChartPeriods;
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

    private void CreateTiles() {
        foreach (var tileConfig in _dashboard.Tiles) {
            Control? tileControl = tileConfig switch {
                ValueTileConfig vtc => new ValueTile(vtc, _dataService),
                ChartTileConfig ctc => new ChartTile(ctc, _dataService),
                _ => null
            };
            if (tileControl != null) {
                if (tileControl is ChartTile chartTile) {
                    chartTile.PeriodChanged += OnChartPeriodChanged;
                }
                Controls.Add(tileControl, tileConfig.Column, tileConfig.Row);
                SetColumnSpan(tileControl, tileConfig.ColumnSpan);
                SetRowSpan(tileControl, tileConfig.RowSpan);
                _tileControls[tileConfig.Id] = tileControl;
            }
        }
    }

    private void OnChartPeriodChanged(ChartTile source, ChartPeriod period, TimeSpan? customDuration) {
        if (!_linkChartPeriods) {
            return;
        }

        foreach (var chartTile in _tileControls.Values.OfType<ChartTile>()) {
            if (ReferenceEquals(chartTile, source)) {
                continue;
            }
            chartTile.SetPeriod(period, customDuration);
        }
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
    }

    #endregion Private Methods

}
