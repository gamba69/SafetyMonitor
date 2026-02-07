using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class EditableTileControl : Panel {
    #region Private Fields

    private readonly Dashboard _dashboard;
    // ДОБАВЛЕНО: Сохраняем шрифт как поле
    private readonly Font _titleFont;

    private Button _deleteButton = null!;
    private Point _dragStartPoint;
    private Button _editButton = null!;
    private Label _infoLabel = null!;
    private bool _isDragging;

    private Label _titleLabel = null!;

    #endregion Private Fields

    #region Public Constructors

    public EditableTileControl(TileConfig config, Dashboard dashboard) {
        Config = config;
        _dashboard = dashboard;

        // ДОБАВЛЕНО: Создаем шрифт один раз
        _titleFont = new Font("Roboto", 10, FontStyle.Bold);

        InitializeUI();
    }

    #endregion Public Constructors

    #region Public Events

    // public event EventHandler? TileMoved;
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

    // ДОБАВЛЕНО: Освобождение ресурсов шрифта
    protected override void Dispose(bool disposing) {
        if (disposing) {
            _titleFont?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ДОБАВЛЕНО: Защита от изменения шрифтов MaterialSkinManager
    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);

        // Восстанавливаем наш шрифт
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
            Font = _titleFont,  // ИЗМЕНЕНО: используем сохраненный шрифт
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

        // CRITICAL: Wire mouse events for drag
        this.MouseDown += OnMouseDown;
        this.MouseMove += OnMouseMove;
        this.MouseUp += OnMouseUp;
        _titleLabel.MouseDown += OnMouseDown;
        _titleLabel.MouseMove += OnMouseMove;
        _titleLabel.MouseUp += OnMouseUp;
        _infoLabel.MouseDown += OnMouseDown;
        _infoLabel.MouseMove += OnMouseMove;
        _infoLabel.MouseUp += OnMouseUp;
    }

    private void OnDelete(object? sender, EventArgs e) {
        if (ThemedMessageBox.Show(this, $"Delete tile '{Config.Title}'?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
            TileDeleted?.Invoke(this, Config);
        }
    }

    private void OnEdit(object? sender, EventArgs e) {
        if (Config is ValueTileConfig vtc) {
            using var editor = new ValueTileEditorForm(vtc);
            if (editor.ShowDialog() == DialogResult.OK) {
                UpdateDisplay();
                TileEdited?.Invoke(this, Config);
            }
        } else if (Config is ChartTileConfig ctc) {
            using var editor = new ChartTileEditorForm(ctc);
            if (editor.ShowDialog() == DialogResult.OK) {
                UpdateDisplay();
                TileEdited?.Invoke(this, Config);
            }
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left) {
            _dragStartPoint = e.Location;
            if (sender != this && sender is Control child) {
                _dragStartPoint = new Point(e.X + child.Left, e.Y + child.Top);
            }
            _isDragging = false;
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e) {
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
    }

    #endregion Private Methods
}
