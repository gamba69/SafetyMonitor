using SafetyMonitor.Models;
using SafetyMonitor.Services;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents editable tile control and encapsulates its related behavior and state.
/// </summary>
public class EditableTileControl : Panel {
    #region Private Fields

    private const int ResizeBorderThreshold = 8;
    private const int UnifiedTileEditorIconSize = 22;
    private const float TileTypeGlyphScale = 0.90f;
    private const float LinkGroupGlyphScale = 1.0f;
    private const float HeaderActionGlyphScale = 0.90f;

    private readonly Dashboard _dashboard;
    private readonly Font _titleFont;
    private readonly string _materialColorScheme;

    private Button _deleteButton = null!;
    private Point _dragStartPoint;
    private Button _editButton = null!;
    private Button? _linkGroupButton;
    private Label _infoLabel = null!;
    private bool _isDragging;
    private bool _isResizing;
    private Point _resizeStartPoint;
    private int _resizeStartSpanColumns;
    private int _resizeStartSpanRows;

    private Label _titleLabel = null!;
    private PictureBox _tileTypeIcon = null!;
    private ContextMenuStrip? _linkGroupMenu;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EditableTileControl"/> class.
    /// </summary>
    /// <param name="config">Input value for config.</param>
    /// <param name="dashboard">Input value for dashboard.</param>
    /// <param name="materialColorScheme">Input value for material color scheme.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public EditableTileControl(TileConfig config, Dashboard dashboard, string materialColorScheme) {
        Config = config;
        _dashboard = dashboard;
        _materialColorScheme = AppColorizationService.Instance.NormalizeMaterialSchemeName(materialColorScheme);

        _titleFont = new Font("Segoe UI", 10, FontStyle.Bold);

        InitializeUI();
    }

    #endregion Public Constructors

    #region Public Events

    public event EventHandler<TileConfig>? TileDeleted;

    public event EventHandler<TileConfig>? TileEdited;

    #endregion Public Events

    #region Public Properties

    /// <summary>
    /// Gets or sets the config for editable tile control. Holds part of the component state used by higher-level application logic.
    /// </summary>
    public TileConfig Config { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Updates the display for editable tile control.
    /// </summary>
    public void UpdateDisplay() {
        _titleLabel.Text = Config.Title;
        _infoLabel.Text = GetInfoText();
        RefreshLinkGroupUI();
    }

    /// <summary>
    /// Refreshes the link group ui for editable tile control.
    /// </summary>
    public void RefreshLinkGroupUI() {
        if (_linkGroupButton == null || Config is not ChartTileConfig ctc) {
            return;
        }

        ctc.LinkGroup = ChartLinkGroupInfo.NormalizeGroup(ctc.LinkGroup, _dashboard.UsedLinkGroups);
        var shouldShowGroupControls = _dashboard.UsedLinkGroups > 1;
        _linkGroupButton.Visible = shouldShowGroupControls;

        if (!shouldShowGroupControls) {
            return;
        }

        _linkGroupButton.Image?.Dispose();
        _linkGroupButton.Image = CreateLinkGroupIcon(ctc.LinkGroup, _linkGroupButton.ForeColor);
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Executes dispose as part of editable tile control processing.
    /// </summary>
    /// <param name="disposing">Input value for disposing.</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _titleFont?.Dispose();
            _tileTypeIcon?.Image?.Dispose();
            _linkGroupMenu?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Executes on font changed as part of editable tile control processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        _titleLabel?.Font = _titleFont;
    }

    #endregion Protected Methods

    #region Private Methods

    /// <summary>
    /// Gets the info text for editable tile control.
    /// </summary>
    /// <returns>The resulting string value.</returns>
    private string GetInfoText() {
        if (Config is ValueTileConfig vtc) {
            return $"{vtc.Metric.GetDisplayName()}\n{GetValueDisplayModeName(vtc.DisplayMode)}";
        } else if (Config is ChartTileConfig ctc) {
            var metricSummary = ctc.MetricAggregations.Count == 0
                ? "No metrics"
                : string.Join(", ",
                    ctc.MetricAggregations.Select(agg =>
                        $"{agg.Metric.GetDisplayName()} ({GetAggregationAbbreviation(agg.Function)})"));

            var linkSummary = $"{ctc.LinkGroup.GetDisplayName()} · {GetLinkGroupPeriodDisplayName(ctc.LinkGroup)}";
            return $"{metricSummary}\n{linkSummary}";
        }
        return Config.Type.ToString();
    }

    /// <summary>
    /// Gets the link group period display name for editable tile control.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <returns>The resulting string value.</returns>
    private string GetLinkGroupPeriodDisplayName(ChartLinkGroup group) {
        var periodPresetUid = _dashboard.GetLinkGroupPeriodPresetUid(group);
        var preset = ChartPeriodPresetStore
            .GetPresetItems()
            .FirstOrDefault(item => string.Equals(item.Uid, periodPresetUid, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(preset.Label) ? "Default period" : preset.Label;
    }

    /// <summary>
    /// Gets the value display mode name for editable tile control.
    /// </summary>
    /// <param name="mode">Input value for mode.</param>
    /// <returns>The resulting string value.</returns>
    private static string GetValueDisplayModeName(ValueTileDisplayMode mode) => mode switch {
        ValueTileDisplayMode.ValueOnly => "Value Only",
        ValueTileDisplayMode.TextOnly => "Text Only",
        ValueTileDisplayMode.TextAndValue => "Text + Value",
        _ => mode.ToString()
    };

    /// <summary>
    /// Gets the aggregation abbreviation for editable tile control.
    /// </summary>
    /// <param name="function">Input value for function.</param>
    /// <returns>The resulting string value.</returns>
    private static string GetAggregationAbbreviation(DataStorage.Models.AggregationFunction function) => function switch {
        DataStorage.Models.AggregationFunction.Average => "Avg",
        DataStorage.Models.AggregationFunction.Minimum => "Min",
        DataStorage.Models.AggregationFunction.Maximum => "Max",
        DataStorage.Models.AggregationFunction.Sum => "Sum",
        DataStorage.Models.AggregationFunction.Count => "Cnt",
        DataStorage.Models.AggregationFunction.First => "Fst",
        DataStorage.Models.AggregationFunction.Last => "Lst",
        _ => function.ToString()
    };

    /// <summary>
    /// Initializes editable tile control state and required resources.
    /// </summary>
    private void InitializeUI() {
        BorderStyle = BorderStyle.FixedSingle;
        var isLight = MaterialSkin.MaterialSkinManager.Instance.Theme == MaterialSkin.MaterialSkinManager.Themes.LIGHT;
        BackColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        Cursor = Cursors.SizeAll;

        var headerBackgroundColor = AppColorizationService.Instance.GetPrimaryActionColor(_materialColorScheme);
        var headerTextColor = AppColorizationService.Instance.GetPrimaryActionTextColor(_materialColorScheme);

        var header = new Panel {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = headerBackgroundColor
        };

        var tileIconName = Config is ValueTileConfig
            ? MaterialIcons.WindowTileValue
            : Config is ChartTileConfig
                ? MaterialIcons.WindowTileChart
                : MaterialIcons.DashboardTab;

        _tileTypeIcon = new PictureBox {
            Dock = DockStyle.Left,
            Width = 30,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Image = CreateTileTypeIcon(tileIconName, headerTextColor)
        };

        _titleLabel = new Label {
            Dock = DockStyle.Fill,
            ForeColor = headerTextColor,
            Font = _titleFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0),
            Text = Config.Title
        };

        _editButton = new Button {
            Dock = DockStyle.Right,
            Width = 30,
            FlatStyle = FlatStyle.Flat,
            ForeColor = headerTextColor,
            Cursor = Cursors.Hand,
            Image = CreateHeaderActionIcon(MaterialIcons.CommonEdit, headerTextColor)
        };
        _editButton.FlatAppearance.BorderSize = 0;
        _editButton.ImageAlign = ContentAlignment.MiddleCenter;
        _editButton.Click += OnEdit;

        if (Config is ChartTileConfig chartConfig) {
            _linkGroupButton = new Button {
                Dock = DockStyle.Right,
                Width = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = headerTextColor,
                Cursor = Cursors.Hand,
                Image = CreateLinkGroupIcon(chartConfig.LinkGroup, headerTextColor)
            };
            _linkGroupButton.FlatAppearance.BorderSize = 0;
            _linkGroupButton.ImageAlign = ContentAlignment.MiddleCenter;
            _linkGroupButton.Click += (_, _) => {
                _linkGroupMenu?.Dispose();
                _linkGroupMenu = CreateLinkGroupMenu();
                _linkGroupMenu.Show(_linkGroupButton, new Point(0, _linkGroupButton.Height));
            };
        }

        _deleteButton = new Button {
            Dock = DockStyle.Right,
            Width = 30,
            FlatStyle = FlatStyle.Flat,
            ForeColor = headerTextColor,
            Cursor = Cursors.Hand,
            Image = CreateHeaderActionIcon(MaterialIcons.CommonClose, headerTextColor)
        };
        _deleteButton.FlatAppearance.BorderSize = 0;
        _deleteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
        _deleteButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(139, 10, 20);
        _deleteButton.ImageAlign = ContentAlignment.MiddleCenter;
        _deleteButton.Click += OnDelete;

        header.Controls.Add(_titleLabel);
        header.Controls.Add(_tileTypeIcon);
        if (_linkGroupButton != null) {
            header.Controls.Add(_linkGroupButton);
        }
        header.Controls.Add(_editButton);
        header.Controls.Add(_deleteButton);

        _infoLabel = new Label {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = isLight ? Color.Black : Color.FromArgb(220, 220, 220),
            Text = GetInfoText()
        };

        Controls.Add(_infoLabel);
        Controls.Add(header);
        RefreshLinkGroupUI();

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        _titleLabel.MouseDown += OnMouseDown;
        _tileTypeIcon.MouseDown += OnMouseDown;
        _titleLabel.MouseMove += OnMouseMove;
        _tileTypeIcon.MouseMove += OnMouseMove;
        _titleLabel.MouseUp += OnMouseUp;
        _tileTypeIcon.MouseUp += OnMouseUp;
        _infoLabel.MouseDown += OnMouseDown;
        _infoLabel.MouseMove += OnMouseMove;
        _infoLabel.MouseUp += OnMouseUp;
    }

    /// <summary>
    /// Creates the link group menu for editable tile control.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private ContextMenuStrip CreateLinkGroupMenu() {
        var menu = new ContextMenuStrip {
            ImageScalingSize = new Size(UnifiedTileEditorIconSize, UnifiedTileEditorIconSize)
        };
        menu.Opening += (_, _) => UpdateLinkGroupMenuSelection(menu);
        foreach (var group in _dashboard.GetAvailableLinkGroups()) {
            var item = new ToolStripMenuItem(group.GetDisplayName(_dashboard.GetLinkGroupPeriodShortName(group))) {
                Tag = group,
                Image = CreateLinkGroupIcon(group, ForeColor),
                ImageScaling = ToolStripItemImageScaling.None,
                CheckOnClick = false
            };
            item.Click += (_, _) => {
                if (Config is not ChartTileConfig ctc) {
                    return;
                }
                ctc.LinkGroup = group;
                UpdateLinkGroupMenuSelection(menu);
                UpdateDisplay();
                if (_linkGroupButton != null) {
                    _linkGroupButton.Image?.Dispose();
                    _linkGroupButton.Image = CreateLinkGroupIcon(group, _linkGroupButton.ForeColor);
                }
                TileEdited?.Invoke(this, Config);
            };
            menu.Items.Add(item);
        }
        return menu;
    }

    /// <summary>
    /// Updates the link group menu selection for editable tile control.
    /// </summary>
    /// <param name="menu">Input value for menu.</param>
    private void UpdateLinkGroupMenuSelection(ContextMenuStrip menu) {
        if (Config is not ChartTileConfig ctc) {
            return;
        }

        foreach (var item in menu.Items.OfType<ToolStripMenuItem>()) {
            if (item.Tag is not ChartLinkGroup itemGroup) {
                continue;
            }

            item.Checked = itemGroup == ctc.LinkGroup;
        }
    }


    /// <summary>
    /// Creates the link group icon for editable tile control.
    /// </summary>
    /// <param name="group">Input value for group.</param>
    /// <param name="color">Input value for color.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap? CreateLinkGroupIcon(ChartLinkGroup group, Color color) {
        return MaterialIcons.GetIcon(GetLinkGroupIcon(group), color, UnifiedTileEditorIconSize, new IconRenderOptions { GlyphScale = LinkGroupGlyphScale, Axes = IconRenderPresetService.Get(IconRenderPreset.DarkOutlined).Axes });
    }

    /// <summary>
    /// Creates the tile type icon for editable tile control.
    /// </summary>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="color">Input value for color.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap? CreateTileTypeIcon(string iconName, Color color) {
        return MaterialIcons.GetIcon(iconName, color, UnifiedTileEditorIconSize, new IconRenderOptions { GlyphScale = TileTypeGlyphScale, Axes = IconRenderPresetService.Get(IconRenderPreset.DarkOutlined).Axes });
    }

    /// <summary>
    /// Creates the header action icon for editable tile control.
    /// </summary>
    /// <param name="iconName">Input value for icon name.</param>
    /// <param name="color">Input value for color.</param>
    /// <returns>The result of the operation.</returns>
    private static Bitmap? CreateHeaderActionIcon(string iconName, Color color) {
        return MaterialIcons.GetIcon(iconName, color, UnifiedTileEditorIconSize, new IconRenderOptions { GlyphScale = HeaderActionGlyphScale, Axes = IconRenderPresetService.Get(IconRenderPreset.DarkOutlined).Axes });
    }

    /// <summary>
    /// Gets the link group icon for editable tile control.
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
    /// Determines whether is near bottom border for editable tile control.
    /// </summary>
    /// <param name="point">Input value for point.</param>
    /// <param name="controlSize">Input value for control size.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool IsNearBottomBorder(Point point, Size controlSize) {
        return controlSize.Height - point.Y <= ResizeBorderThreshold;
    }

    /// <summary>
    /// Determines whether is near right border for editable tile control.
    /// </summary>
    /// <param name="point">Input value for point.</param>
    /// <param name="controlSize">Input value for control size.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool IsNearRightBorder(Point point, Size controlSize) {
        return controlSize.Width - point.X <= ResizeBorderThreshold;
    }

    /// <summary>
    /// Executes on delete as part of editable tile control processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void OnDelete(object? sender, EventArgs e) {
        if (ThemedMessageBox.Show(this, $"Delete tile '{Config.Title}'?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
            TileDeleted?.Invoke(this, Config);
        }
    }

    /// <summary>
    /// Executes on edit as part of editable tile control processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void OnEdit(object? sender, EventArgs e) {
        if (Config is ValueTileConfig vtc) {
            using var editor = new ValueTileEditorForm(vtc);
            if (editor.ShowDialog() == DialogResult.OK) {
                UpdateDisplay();
                TileEdited?.Invoke(this, Config);
            }
        } else if (Config is ChartTileConfig ctc) {
            using var editor = new ChartTileEditorForm(ctc, _dashboard);
            if (editor.ShowDialog() == DialogResult.OK) {
                ctc.LinkGroup = ChartLinkGroupInfo.NormalizeGroup(ctc.LinkGroup, _dashboard.UsedLinkGroups);
                UpdateDisplay();
                TileEdited?.Invoke(this, Config);
            }
        }
    }

    /// <summary>
    /// Executes on mouse down as part of editable tile control processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void OnMouseDown(object? sender, MouseEventArgs e) {
        if (e.Button != MouseButtons.Left) {
            return;
        }

        var localPoint = PointToClient(MousePosition);
        var nearRight = IsNearRightBorder(localPoint, Size);
        var nearBottom = IsNearBottomBorder(localPoint, Size);

        if (nearRight || nearBottom) {
            if (Parent == null) {
                return;
            }

            _isResizing = true;
            _resizeStartPoint = Parent.PointToClient(MousePosition);
            _resizeStartSpanRows = Config.RowSpan;
            _resizeStartSpanColumns = Config.ColumnSpan;
            return;
        }

        _dragStartPoint = e.Location;
        if (sender != this && sender is Control child) {
            _dragStartPoint = new Point(e.X + child.Left, e.Y + child.Top);
        }
        _isDragging = false;
    }

    /// <summary>
    /// Executes on mouse move as part of editable tile control processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void OnMouseMove(object? sender, MouseEventArgs e) {
        var localPoint = PointToClient(MousePosition);
        var nearRight = IsNearRightBorder(localPoint, Size);
        var nearBottom = IsNearBottomBorder(localPoint, Size);
        Cursor = nearRight && nearBottom
            ? Cursors.SizeNWSE
            : nearRight
                ? Cursors.SizeWE
                : nearBottom
                    ? Cursors.SizeNS
                    : Cursors.SizeAll;

        if (_isResizing) {
            if (Parent == null) {
                return;
            }

            var current = Parent.PointToClient(MousePosition);
            var deltaX = current.X - _resizeStartPoint.X;
            var deltaY = current.Y - _resizeStartPoint.Y;

            var cellW = _dashboard.Columns > 0 ? Parent.ClientSize.Width / (double)_dashboard.Columns : 0d;
            var cellH = _dashboard.Rows > 0 ? Parent.ClientSize.Height / (double)_dashboard.Rows : 0d;
            if (cellW <= 0d || cellH <= 0d) {
                return;
            }

            var newColSpan = Math.Clamp(_resizeStartSpanColumns + (int)Math.Round(deltaX / (double)cellW), 1, _dashboard.Columns - Config.Column);
            var newRowSpan = Math.Clamp(_resizeStartSpanRows + (int)Math.Round(deltaY / (double)cellH), 1, _dashboard.Rows - Config.Row);

            if (newColSpan == Config.ColumnSpan && newRowSpan == Config.RowSpan) {
                return;
            }

            var oldColSpan = Config.ColumnSpan;
            var oldRowSpan = Config.RowSpan;
            Config.ColumnSpan = newColSpan;
            Config.RowSpan = newRowSpan;

            if (_dashboard.CanPlaceTile(Config)) {
                TileEdited?.Invoke(this, Config);
                UpdateDisplay();
            } else {
                Config.ColumnSpan = oldColSpan;
                Config.RowSpan = oldRowSpan;
            }

            return;
        }

        if (e.Button == MouseButtons.Left && !_isDragging) {
            var currentPoint = e.Location;
            if (sender != this && sender is Control child) {
                currentPoint = new Point(e.X + child.Left, e.Y + child.Top);
            }

            var distance = Math.Sqrt(
                Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2)
            );

            if (distance > 10) {
                _isDragging = true;
                DoDragDrop(this, DragDropEffects.Move);
            }
        }
    }

    /// <summary>
    /// Executes on mouse up as part of editable tile control processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void OnMouseUp(object? sender, MouseEventArgs e) {
        _isDragging = false;
        _isResizing = false;
    }

    #endregion Private Methods
}
