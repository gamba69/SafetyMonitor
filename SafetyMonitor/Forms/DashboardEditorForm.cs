using MaterialSkin;
using SafetyMonitor.Services;
using SafetyMonitor.Models;

namespace SafetyMonitor.Forms;

public class DashboardEditorForm : ThemedCaptionForm {
    #region Private Fields

    private const int TileInsetX = 4;
    private const int TileInsetY = 6;

    private readonly Dashboard _dashboard;
    private readonly string _materialColorScheme;
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
        _materialColorScheme = AppColorizationService.Instance.NormalizeMaterialSchemeName(new AppSettingsService().LoadSettings().MaterialColorScheme);

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
        var control = new EditableTileControl(config, _dashboard, _materialColorScheme);
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
        var italicFont = new Font("Segoe UI", 10f, FontStyle.Italic);

        // Main layout
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            AutoSize = false
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Description
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Settings row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 2: Grid panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Add buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: Save/Cancel buttons

        var descriptionPanel = new FlowLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        descriptionPanel.Controls.Add(new Label {
            Text = "Use this editor to configure the dashboard name, grid, and tile set.",
            Font = titleFont,
            AutoSize = true,
            MaximumSize = new Size(1080, 0),
            Margin = new Padding(0, 0, 0, 2)
        });
        descriptionPanel.Controls.Add(new Label {
            Text = "Move tiles by dragging them on the grid, and resize them by dragging their right or bottom edge directly in this editor.",
            Font = italicFont,
            AutoSize = true,
            MaximumSize = new Size(1080, 0),
            Margin = new Padding(0)
        });
        mainLayout.Controls.Add(descriptionPanel, 0, 0);

        // Row 1: Settings row (Name, Grid Size, Quick Access)
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

        mainLayout.Controls.Add(settingsPanel, 0, 1);

        // Row 2: Grid Panel
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
        _gridPanel.Resize += (s, e) => RefreshAllTilePositions();
        mainLayout.Controls.Add(_gridPanel, 0, 2);

        // Row 3: Add tile buttons
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

        mainLayout.Controls.Add(addButtonPanel, 0, 3);

        // Row 4: Spacer
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 4);

        // Row 5: Save/Cancel buttons
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

        mainLayout.Controls.Add(buttonPanel, 0, 5);

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

        // Initial load can happen before final layout; recalc after controls are measured.
        if (IsHandleCreated) {
            BeginInvoke((Action)RefreshAllTilePositions);
        } else {
            Shown += OnInitialLayoutShown;
        }
    }

    private void OnGridDragDrop(object? sender, DragEventArgs e) {
        if (e.Data?.GetData(typeof(EditableTileControl)) is EditableTileControl tile) {
            GetCellSize(out var cellW, out var cellH);
            var point = _gridPanel.PointToClient(new Point(e.X, e.Y));

            int newCol = Math.Clamp((int)Math.Floor(point.X / cellW), 0, _dashboard.Columns - tile.Config.ColumnSpan);
            int newRow = Math.Clamp((int)Math.Floor(point.Y / cellH), 0, _dashboard.Rows - tile.Config.RowSpan);

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

    private void OnInitialLayoutShown(object? sender, EventArgs e) {
        Shown -= OnInitialLayoutShown;
        RefreshAllTilePositions();
    }

    private void OnGridPaint(object? sender, PaintEventArgs e) {
        GetCellSize(out var cellW, out var cellH);
        var gridWidth = _gridPanel.ClientSize.Width;
        var gridHeight = _gridPanel.ClientSize.Height;

        using var pen = new Pen(Color.LightGray, 1) {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
        };

        for (int i = 1; i < _dashboard.Columns; i++) {
            var x = (int)Math.Round(i * cellW);
            e.Graphics.DrawLine(pen, x, 0, x, gridHeight);
        }

        for (int i = 1; i < _dashboard.Rows; i++) {
            var y = (int)Math.Round(i * cellH);
            e.Graphics.DrawLine(pen, 0, y, gridWidth, y);
        }
    }

    private void OnResize(object? sender, EventArgs e) {
        _dashboard.Rows = (int)_rowsNumeric.Value;
        _dashboard.Columns = (int)_columnsNumeric.Value;

        RefreshAllTilePositions();
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

    private void RefreshAllTilePositions() {
        if (_gridPanel.ClientSize.Width <= 0 || _gridPanel.ClientSize.Height <= 0) {
            return;
        }

        foreach (var tile in _tileControls) {
            UpdateTilePosition(tile);
        }

        _gridPanel.Invalidate();
    }

    private void UpdateTilePosition(EditableTileControl control) {
        GetCellSize(out var cellW, out var cellH);

        var startX = control.Config.Column * cellW;
        var startY = control.Config.Row * cellH;
        var endX = (control.Config.Column + control.Config.ColumnSpan) * cellW;
        var endY = (control.Config.Row + control.Config.RowSpan) * cellH;

        // Keep tile strictly inside its grid area after insets; this avoids
        // occasional 1px overflow caused by midpoint rounding.
        var x = (int)Math.Ceiling(startX) + TileInsetX;
        var y = (int)Math.Ceiling(startY) + TileInsetY;
        var right = (int)Math.Floor(endX) - TileInsetX;
        var bottom = (int)Math.Floor(endY) - TileInsetY;

        var width = Math.Max(24, right - x);
        var height = Math.Max(24, bottom - y);

        control.Location = new Point(x, y);
        control.Size = new Size(width, height);
    }

    private void GetCellSize(out double cellW, out double cellH) {
        cellW = _dashboard.Columns > 0 ? _gridPanel.ClientSize.Width / (double)_dashboard.Columns : 0d;
        cellH = _dashboard.Rows > 0 ? _gridPanel.ClientSize.Height / (double)_dashboard.Rows : 0d;
    }

    #endregion Private Methods
}
