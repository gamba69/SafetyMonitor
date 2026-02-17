using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class EditableTileControl : Panel {
    #region Private Fields

    private const int ResizeBorderThreshold = 8;

    private readonly Dashboard _dashboard;
    private readonly Font _titleFont;

    private Button _deleteButton = null!;
    private Point _dragStartPoint;
    private Button _editButton = null!;
    private Label _infoLabel = null!;
    private bool _isDragging;
    private bool _isResizing;
    private Point _resizeStartPoint;
    private int _resizeStartSpanColumns;
    private int _resizeStartSpanRows;

    private Label _titleLabel = null!;

    #endregion Private Fields

    #region Public Constructors

    public EditableTileControl(TileConfig config, Dashboard dashboard) {
        Config = config;
        _dashboard = dashboard;

        _titleFont = new Font("Segoe UI", 10, FontStyle.Bold);

        InitializeUI();
    }

    #endregion Public Constructors

    #region Public Events

    public event EventHandler<TileConfig>? TileDeleted;

    public event EventHandler<TileConfig>? TileEdited;

    #endregion Public Events

    #region Public Properties

    public TileConfig Config { get; }

    #endregion Public Properties

    #region Public Methods

    public void UpdateDisplay() {
        _titleLabel.Text = Config.Title;
        _infoLabel.Text = GetInfoText();
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _titleFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);
        _titleLabel?.Font = _titleFont;
    }

    #endregion Protected Methods

    #region Private Methods

    private string GetInfoText() {
        if (Config is ValueTileConfig vtc) {
            return $"Type: Value\nMetric: {vtc.Metric.GetDisplayName()}\nPos: ({Config.Row}, {Config.Column})\nSize: {Config.RowSpan}×{Config.ColumnSpan}";
        } else if (Config is ChartTileConfig ctc) {
            return $"Type: Chart\nMetrics: {ctc.MetricAggregations.Count}\nPos: ({Config.Row}, {Config.Column})\nSize: {Config.RowSpan}×{Config.ColumnSpan}";
        }
        return $"Type: {Config.Type}\nPos: ({Config.Row}, {Config.Column})\nSize: {Config.RowSpan}×{Config.ColumnSpan}";
    }

    private void InitializeUI() {
        BorderStyle = BorderStyle.FixedSingle;
        var isLight = MaterialSkin.MaterialSkinManager.Instance.Theme == MaterialSkin.MaterialSkinManager.Themes.LIGHT;
        BackColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        Cursor = Cursors.SizeAll;

        var header = new Panel {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(0, 121, 107)
        };

        _titleLabel = new Label {
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = _titleFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0),
            Text = Config.Title
        };

        _editButton = new Button {
            Text = "✏",
            Dock = DockStyle.Right,
            Width = 30,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _editButton.FlatAppearance.BorderSize = 0;
        _editButton.Click += OnEdit;

        _deleteButton = new Button {
            Text = "✖",
            Dock = DockStyle.Right,
            Width = 30,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        _deleteButton.FlatAppearance.BorderSize = 0;
        _deleteButton.Click += OnDelete;

        header.Controls.Add(_titleLabel);
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

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        _titleLabel.MouseDown += OnMouseDown;
        _titleLabel.MouseMove += OnMouseMove;
        _titleLabel.MouseUp += OnMouseUp;
        _infoLabel.MouseDown += OnMouseDown;
        _infoLabel.MouseMove += OnMouseMove;
        _infoLabel.MouseUp += OnMouseUp;
    }

    private static bool IsNearBottomBorder(Point point, Size controlSize) {
        return controlSize.Height - point.Y <= ResizeBorderThreshold;
    }

    private static bool IsNearRightBorder(Point point, Size controlSize) {
        return controlSize.Width - point.X <= ResizeBorderThreshold;
    }

    private void OnDelete(object? sender, EventArgs e) {
        if (ThemedMessageBox.Show(this, $"Delete tile '{Config.Title}'?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
            TileDeleted?.Invoke(this, Config);
        }
    }

    private void OnEdit(object? sender, EventArgs e) {
        if (Config is ValueTileConfig vtc) {
            using var editor = new ValueTileEditorForm(vtc, _dashboard);
            if (editor.ShowDialog() == DialogResult.OK) {
                UpdateDisplay();
                TileEdited?.Invoke(this, Config);
            }
        } else if (Config is ChartTileConfig ctc) {
            using var editor = new ChartTileEditorForm(ctc, _dashboard);
            if (editor.ShowDialog() == DialogResult.OK) {
                UpdateDisplay();
                TileEdited?.Invoke(this, Config);
            }
        }
    }

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

            var cellW = Parent.Width / _dashboard.Columns;
            var cellH = Parent.Height / _dashboard.Rows;
            if (cellW <= 0 || cellH <= 0) {
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

    private void OnMouseUp(object? sender, MouseEventArgs e) {
        _isDragging = false;
        _isResizing = false;
    }

    #endregion Private Methods
}
