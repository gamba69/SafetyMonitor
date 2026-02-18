using MaterialSkin;
using SafetyMonitorView.Services;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class DashboardEditorForm : Form {
    #region Private Fields

    private readonly Dashboard _dashboard;
    private readonly List<EditableTileControl> _tileControls = [];
    private Button _addChartButton = null!;
    private Button _addValueButton = null!;
    private Button _cancelButton = null!;
    private NumericUpDown _columnsNumeric = null!;
    private Panel _gridPanel = null!;
    private TextBox _nameTextBox = null!;
    private CheckBox _quickAccessCheckBox = null!;
    private Button _resizeButton = null!;
    private NumericUpDown _rowsNumeric = null!;
    private Button _saveButton = null!;

    #endregion Private Fields

    #region Public Constructors

    public DashboardEditorForm(Dashboard dashboard) {
        _dashboard = dashboard;

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.DashboardEditCurrent);
        ApplyTheme();
        LoadDashboard();
    }

    #endregion Public Constructors

    #region Public Properties

    public bool Modified { get; private set; }

    #endregion Public Properties

    #region Private Methods

    private void AddEditableTile(TileConfig config) {
        var control = new EditableTileControl(config, _dashboard);
        control.TileDeleted += OnTileDeleted;
        control.TileEdited += OnTileEdited;

        UpdateTilePosition(control);
        _tileControls.Add(control);
        _gridPanel.Controls.Add(control);
        control.BringToFront();
        control.Visible = true;
    }

    private void AddTile(TileType type) {
        TileConfig config = type == TileType.Value
            ? new ValueTileConfig {
                Title = "New Value",
                Metric = MetricType.Temperature,
                Row = 0,
                Column = 0
            }
            : new ChartTileConfig {
                Title = "New Chart",
                Row = 0,
                Column = 0,
                RowSpan = 2,
                ColumnSpan = 2
            };

        // Find free position for the tile
        if (!FindFreePosition(config)) {
            ThemedMessageBox.Show(
                this,
                "No free space available for this tile. Try resizing existing tiles or increasing grid size.",
                "Cannot Add Tile",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _dashboard.Tiles.Add(config);
        AddEditableTile(config);
        Modified = true;
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        // Apply theme to grid panel
        _gridPanel.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);

        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);
            // Don't modify tiles inside grid panel - they have their own styling
            if (control.Parent == _gridPanel) {
                continue;
            }

            switch (control) {
                case Label lbl:
                    lbl.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case Button btn:
                    ThemedButtonStyler.Apply(btn, isLight);
                    break;
                case TextBox txt:
                    txt.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    txt.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case CheckBox chk:
                    chk.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case NumericUpDown num:
                    num.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    num.ForeColor = isLight ? Color.Black : Color.White;
                    break;
            }

            // Don't recurse into grid panel children
            if (control == _gridPanel) {
                continue;
            }

            ApplyThemeRecursive(control, isLight);
        }
    }

    private bool FindFreePosition(TileConfig config) {
        // Try to find a free position for the tile
        for (int row = 0; row <= _dashboard.Rows - config.RowSpan; row++) {
            for (int col = 0; col <= _dashboard.Columns - config.ColumnSpan; col++) {
                config.Row = row;
                config.Column = col;
                if (_dashboard.CanPlaceTile(config)) {
                    return true;
                }
            }
        }
        return false;
    }

    private void InitializeComponent() {
        Text = "Dashboard Editor";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Padding = new Padding(20);

        var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        var normalFont = new Font("Segoe UI", 10f);

        // Main layout
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = false
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Settings row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 1: Grid panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Add buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Save/Cancel buttons

        // Row 0: Settings row (Name, Grid Size, Quick Access)
        var settingsPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 15)
        };

        // Name
        settingsPanel.Controls.Add(new Label {
            Text = "Name:",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 7, 5, 0)
        });
        _nameTextBox = new TextBox {
            Font = normalFont,
            Width = 200,
            Margin = new Padding(0, 3, 20, 0)
        };
        _nameTextBox.TextChanged += (s, e) => Modified = true;
        settingsPanel.Controls.Add(_nameTextBox);

        // Grid Size
        settingsPanel.Controls.Add(new Label {
            Text = "Grid Size:",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 7, 5, 0)
        });
        _rowsNumeric = new NumericUpDown {
            Width = 60,
            Minimum = 1,
            Maximum = 10,
            Value = 4,
            Font = normalFont,
            Margin = new Padding(0, 3, 5, 0)
        };
        settingsPanel.Controls.Add(_rowsNumeric);

        settingsPanel.Controls.Add(new Label {
            Text = "×",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 7, 5, 0)
        });

        _columnsNumeric = new NumericUpDown {
            Width = 60,
            Minimum = 1,
            Maximum = 10,
            Value = 4,
            Font = normalFont,
            Margin = new Padding(0, 3, 10, 0)
        };
        settingsPanel.Controls.Add(_columnsNumeric);

        _resizeButton = new Button {
            Text = "Resize",
            Width = 100,
            Height = 28,
            Font = normalFont,
            Margin = new Padding(0, 0, 20, 0)
        };
        _resizeButton.Click += OnResize;
        settingsPanel.Controls.Add(_resizeButton);

        // Quick Access
        _quickAccessCheckBox = new CheckBox {
            Text = "Quick Access (max 7)",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 0)
        };
        _quickAccessCheckBox.CheckedChanged += (s, e) => Modified = true;
        settingsPanel.Controls.Add(_quickAccessCheckBox);

        mainLayout.Controls.Add(settingsPanel, 0, 0);

        // Row 1: Grid Panel
        _gridPanel = new Panel {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            AllowDrop = true,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 15)
        };
        _gridPanel.Paint += OnGridPaint;
        _gridPanel.DragEnter += OnGridDragEnter;
        _gridPanel.DragOver += OnGridDragOver;
        _gridPanel.DragDrop += OnGridDragDrop;
        mainLayout.Controls.Add(_gridPanel, 0, 1);

        // Row 2: Add tile buttons
        var addButtonPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10)
        };

        _addValueButton = new Button {
            Text = "Add Value Tile",
            Width = 150,
            Height = 35,
            Font = normalFont,
            Margin = new Padding(0, 0, 10, 0)
        };
        _addValueButton.Click += (s, e) => AddTile(TileType.Value);
        addButtonPanel.Controls.Add(_addValueButton);

        _addChartButton = new Button {
            Text = "Add Chart Tile",
            Width = 150,
            Height = 35,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 0)
        };
        _addChartButton.Click += (s, e) => AddTile(TileType.Chart);
        addButtonPanel.Controls.Add(_addChartButton);

        mainLayout.Controls.Add(addButtonPanel, 0, 2);

        // Row 3: Spacer
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 3);

        // Row 4: Save/Cancel buttons
        var buttonPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 10, 0, 0)
        };

        _cancelButton = new Button {
            Text = "Cancel",
            Width = 110,
            Height = 35,
            Font = normalFont,
            Margin = new Padding(0)
        };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);

        _saveButton = new Button {
            Text = "Save",
            Width = 110,
            Height = 35,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);

        mainLayout.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(mainLayout);

        // Set form size
        ClientSize = new Size(1150, 800);
    }
    private void LoadDashboard() {
        _nameTextBox.Text = _dashboard.Name;
        _rowsNumeric.Value = _dashboard.Rows;
        _columnsNumeric.Value = _dashboard.Columns;
        _quickAccessCheckBox.Checked = _dashboard.IsQuickAccess;

        foreach (var config in _dashboard.Tiles) {
            AddEditableTile(config);
        }
    }

    private void OnGridDragDrop(object? sender, DragEventArgs e) {
        if (e.Data?.GetData(typeof(EditableTileControl)) is EditableTileControl tile) {
            var cellW = _gridPanel.Width / _dashboard.Columns;
            var cellH = _gridPanel.Height / _dashboard.Rows;
            var point = _gridPanel.PointToClient(new Point(e.X, e.Y));

            int newCol = Math.Clamp(point.X / cellW, 0, _dashboard.Columns - tile.Config.ColumnSpan);
            int newRow = Math.Clamp(point.Y / cellH, 0, _dashboard.Rows - tile.Config.RowSpan);

            var oldRow = tile.Config.Row;
            var oldCol = tile.Config.Column;
            tile.Config.Row = newRow;
            tile.Config.Column = newCol;

            if (_dashboard.CanPlaceTile(tile.Config)) {
                UpdateTilePosition(tile);
                Modified = true;
            } else {
                tile.Config.Row = oldRow;
                tile.Config.Column = oldCol;
            }
        }
    }

    private void OnGridDragEnter(object? sender, DragEventArgs e) {
        if (e.Data != null && e.Data.GetDataPresent(typeof(EditableTileControl))) {
            e.Effect = DragDropEffects.Move;
        }
    }

    private void OnGridDragOver(object? sender, DragEventArgs e) {
        if (e.Data != null && e.Data.GetDataPresent(typeof(EditableTileControl))) {
            e.Effect = DragDropEffects.Move;
        }
    }

    private void OnGridPaint(object? sender, PaintEventArgs e) {
        var cellW = _gridPanel.Width / _dashboard.Columns;
        var cellH = _gridPanel.Height / _dashboard.Rows;

        using var pen = new Pen(Color.LightGray, 1) {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
        };

        for (int i = 1; i < _dashboard.Columns; i++) {
            e.Graphics.DrawLine(pen, i * cellW, 0, i * cellW, _gridPanel.Height);
        }

        for (int i = 1; i < _dashboard.Rows; i++) {
            e.Graphics.DrawLine(pen, 0, i * cellH, _gridPanel.Width, i * cellH);
        }
    }

    private void OnResize(object? sender, EventArgs e) {
        _dashboard.Rows = (int)_rowsNumeric.Value;
        _dashboard.Columns = (int)_columnsNumeric.Value;

        foreach (var tile in _tileControls) {
            UpdateTilePosition(tile);
        }

        _gridPanel.Invalidate();
        Modified = true;
    }

    private void OnTileDeleted(object? sender, TileConfig config) {
        var control = _tileControls.FirstOrDefault(c => c.Config.Id == config.Id);
        if (control != null) {
            _tileControls.Remove(control);
            _gridPanel.Controls.Remove(control);
            _dashboard.Tiles.Remove(config);
            control.Dispose();
            Modified = true;
        }
    }

    private void OnTileEdited(object? sender, TileConfig config) {
        if (sender is EditableTileControl control) {
            UpdateTilePosition(control);
            control.UpdateDisplay();
        }

        _gridPanel.Invalidate();
        Modified = true;
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        // CRITICAL: Save name and quick access flag
        _dashboard.Name = _nameTextBox.Text;
        _dashboard.IsQuickAccess = _quickAccessCheckBox.Checked;

        DialogResult = DialogResult.OK;
        Close();
    }
    private void UpdateTilePosition(EditableTileControl control) {
        var cellW = _gridPanel.Width / _dashboard.Columns;
        var cellH = _gridPanel.Height / _dashboard.Rows;

        control.Location = new Point(control.Config.Column * cellW, control.Config.Row * cellH);
        control.Size = new Size(control.Config.ColumnSpan * cellW - 4, control.Config.RowSpan * cellH - 4);
    }

    #endregion Private Methods
}
