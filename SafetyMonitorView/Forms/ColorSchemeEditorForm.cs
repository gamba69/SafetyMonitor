using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using ColorScheme = SafetyMonitorView.Models.ColorScheme;

namespace SafetyMonitorView.Forms;

public class ColorSchemeEditorForm : Form {
    #region Private Fields

    private const int PreviewUpdateDelayMs = 1;

    private readonly ColorSchemeService _colorSchemeService;
    private Button _cancelButton = null!;
    private ColorScheme? _currentScheme;
    private Button _deleteButton = null!;
    private Button _duplicateButton = null!;
    private CheckBox _gradientCheckBox = null!;
    private bool _isDirty;
    private bool _isLoading;
    private TextBox _nameTextBox = null!;
    private Button _newButton = null!;
    private Panel _previewPanel = null!;
    private readonly System.Windows.Forms.Timer _previewUpdateTimer;
    private bool _isPreviewPrimed;
    private Button _saveButton = null!;
    private ListBox _schemeList = null!;
    private List<ColorScheme> _schemes;
    private DataGridView _stopsGrid = null!;

    #endregion Private Fields

    #region Public Constructors

    public ColorSchemeEditorForm() {
        _colorSchemeService = new ColorSchemeService();
        _schemes = _colorSchemeService.LoadSchemes();
        _previewUpdateTimer = new System.Windows.Forms.Timer { Interval = PreviewUpdateDelayMs };
        _previewUpdateTimer.Tick += PreviewUpdateTimer_Tick;
        InitializeComponent();
        ApplyTheme();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewColorSchemes);
        PopulateSchemeList();
        if (_schemeList.Items.Count > 0) {
            _schemeList.SelectedIndex = 0;
        }

        Shown += ColorSchemeEditorForm_Shown;
    }

    #endregion Public Constructors

    #region Protected Methods

    protected override void OnFormClosing(FormClosingEventArgs e) {
        if (_isDirty && _currentScheme != null) {
            var result = ThemedMessageBox.Show(this, $"Save changes to \"{_currentScheme.Name}\"?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) {
                SaveCurrentScheme();
            } else if (result == DialogResult.Cancel) {
                e.Cancel = true;
                return;
            }
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e) {
        _previewUpdateTimer.Stop();
        _previewUpdateTimer.Dispose();
        base.OnFormClosed(e);
    }

    #endregion Protected Methods

    #region Private Methods

    private void AddStop_Click(object? sender, EventArgs e) {
        var rowIdx = _stopsGrid.Rows.Add("0", "", "");
        _stopsGrid.Rows[rowIdx].Tag = Color.Gray;
        SortGridByValue();
        UpdateDirtyState();
        UpdatePreview();
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        ApplyThemeRecursive(this, isLight);
    }

    // ── Theme ──
    private static void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);
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
                case ListBox lb:
                    lb.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    lb.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case Panel panel:
                    panel.BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
                    break;
                case DataGridView dgv:
                    dgv.BackgroundColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    dgv.DefaultCellStyle.BackColor = dgv.BackgroundColor;
                    dgv.DefaultCellStyle.ForeColor = isLight ? Color.Black : Color.White;
                    dgv.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(225, 245, 254) : Color.FromArgb(56, 78, 84);
                    dgv.DefaultCellStyle.SelectionForeColor = dgv.DefaultCellStyle.ForeColor;
                    dgv.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(238, 238, 238) : Color.FromArgb(55, 71, 79);
                    dgv.ColumnHeadersDefaultCellStyle.ForeColor = dgv.DefaultCellStyle.ForeColor;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                    dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = dgv.ColumnHeadersDefaultCellStyle.ForeColor;
                    dgv.EnableHeadersVisualStyles = false;
                    dgv.GridColor = isLight ? Color.FromArgb(224, 224, 224) : Color.FromArgb(78, 96, 103);
                    break;
            }
            ApplyThemeRecursive(control, isLight);
        }
    }

    private void DeleteButton_Click(object? sender, EventArgs e) {
        if (_currentScheme == null) {
            return;
        }

        if (ColorSchemeService.IsBuiltIn(_currentScheme.Name)) {
            ThemedMessageBox.Show(this, "Built-in color schemes cannot be deleted.\nYou can duplicate it and modify the copy.",
                "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (ThemedMessageBox.Show(this, $"Delete color scheme \"{_currentScheme.Name}\"?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) {
            return;
        }

        _colorSchemeService.DeleteScheme(_currentScheme.Name);
        _schemes = _colorSchemeService.LoadSchemes();
        _currentScheme = null;
        _isDirty = false;
        PopulateSchemeList();
        if (_schemeList.Items.Count > 0) {
            _schemeList.SelectedIndex = 0;
        }
    }

    private void DuplicateButton_Click(object? sender, EventArgs e) {
        if (_currentScheme == null) {
            return;
        }

        var source = ReadSchemeFromEditor();

        var name = source.Name + " (copy)";
        var counter = 1;
        while (_schemes.Any(s => s.Name == name)) {
            name = $"{source.Name} (copy {counter++})";
        }

        source.Name = name;
        _colorSchemeService.SaveScheme(source);
        _schemes = _colorSchemeService.LoadSchemes();
        PopulateSchemeList();
        _schemeList.SelectedIndex = _schemes.FindIndex(s => s.Name == name);
        _isDirty = false;
    }

    private void InitializeComponent() {
        Text = "Color Schemes";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(700, 520);
        ClientSize = new Size(780, 560);
        Padding = new Padding(10);

        var titleFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        var normalFont = new Font("Segoe UI", 9.5f);

        // ══════════════════════════════════════════════════════════
        // Root: TableLayoutPanel  2 columns × 2 rows
        //   Row 0 (fill):   [scheme list]  [editor area]
        //   Row 1 (45px):   [=== single bottom bar, ColumnSpan=2 ===]
        // ══════════════════════════════════════════════════════════
        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        // ── Row 0, Col 0: scheme list ───────────────────────
        var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
        var listLabel = new Label { Text = "Color Schemes:", Font = titleFont, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
        _schemeList = new ListBox { Dock = DockStyle.Fill, Font = normalFont, IntegralHeight = false };
        _schemeList.SelectedIndexChanged += SchemeList_SelectedIndexChanged;
        leftPanel.Controls.Add(_schemeList);
        leftPanel.Controls.Add(listLabel);
        root.Controls.Add(leftPanel, 0, 0);

        // ── Row 0, Col 1: editor area ───────────────────────
        var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 0, 0, 0) };

        var headerPanel = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerLabel = new Label {
            Text = "Configure color schemes used by value tiles to visualize metric ranges, thresholds, and state severity in dashboards.",
            Font = titleFont,
            AutoSize = true,
            MaximumSize = new Size(810, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        headerPanel.Controls.Add(headerLabel, 0, 0);
        headerPanel.SetColumnSpan(headerLabel, 2);

        var rangesLabel = new Label {
            Text = "• Stops define threshold values (compared as ≤).",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 2)
        };
        var gradientLabel = new Label {
            Text = "• Gradient interpolation blends between stop colors.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        var descriptionLabel = new Label {
            Text = "• Description is shown in legends/tooltips for readability.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        var previewLabel = new Label {
            Text = "• Preview shows how the scheme will look in tiles.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        headerPanel.Controls.Add(rangesLabel, 0, 1);
        headerPanel.Controls.Add(gradientLabel, 1, 1);
        headerPanel.Controls.Add(descriptionLabel, 0, 2);
        headerPanel.Controls.Add(previewLabel, 1, 2);

        var namePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 35, WrapContents = false, AutoSize = false };
        namePanel.Controls.Add(new Label { Text = "Name:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 6, 5, 0) });
        _nameTextBox = new TextBox { Width = 250, Font = normalFont, Margin = new Padding(0, 2, 15, 0) };
        _nameTextBox.TextChanged += (s, e) => UpdateDirtyState();
        namePanel.Controls.Add(_nameTextBox);
        _gradientCheckBox = new CheckBox { Text = "Gradient interpolation", Font = normalFont, AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
        _gradientCheckBox.CheckedChanged += (s, e) => { UpdateDirtyState(); UpdatePreview(); };
        namePanel.Controls.Add(_gradientCheckBox);

        var stopsLabel = new Label { Text = "Color Stops:", Font = titleFont, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 5, 0, 3) };

        _stopsGrid = new DataGridView {
            Dock = DockStyle.Fill,
            Font = normalFont,
            AutoGenerateColumns = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        SetupGridColumns();
        _stopsGrid.CellClick += StopsGrid_CellClick;
        _stopsGrid.CellValueChanged += (s, e) => { SortGridByValue(); UpdateDirtyState(); UpdatePreview(); };
        _stopsGrid.CellEndEdit += (s, e) => { SortGridByValue(); UpdateDirtyState(); UpdatePreview(); };

        var gridButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 3, 0, 3) };
        var addStopBtn = new Button { Text = "Add Stop", Width = 110, Height = 32, Font = normalFont };
        addStopBtn.Click += AddStop_Click;
        var removeStopBtn = new Button { Text = "Del Stop", Width = 110, Height = 32, Font = normalFont };
        removeStopBtn.Click += RemoveStop_Click;
        gridButtons.Controls.AddRange([addStopBtn, removeStopBtn]);

        _previewPanel = new PreviewPanel { Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(0, 5, 0, 0) };
        _previewPanel.Paint += PreviewPanel_Paint;


        // ── Row 1: SINGLE bottom bar spanning both columns ──
        var bottomBar = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
            Margin = new Padding(0),
            ColumnCount = 2,
            RowCount = 1
        };
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(bottomBar, 0, 1);
        root.SetColumnSpan(bottomBar, 2);

        var leftButtonsPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };

        // Left-side buttons inside bottomBar
        _newButton = new Button { Text = "Add", Width = 80, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 10, 0) };
        _newButton.Click += NewButton_Click;
        _duplicateButton = new Button { Text = "Dup", Width = 80, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 10, 0) };
        _duplicateButton.Click += DuplicateButton_Click;
        _deleteButton = new Button { Text = "Del", Width = 80, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 0, 0) };
        _deleteButton.Click += DeleteButton_Click;

        leftButtonsPanel.Controls.AddRange([_newButton, _duplicateButton, _deleteButton]);

        // Right-side buttons inside bottomBar (anchored to right)
        var rightButtonsPanel = new FlowLayoutPanel {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 0, 0) };
        _cancelButton.Click += (s, e) => Close();

        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 5, 10, 0) };
        _saveButton.Click += SaveButton_Click;

        rightButtonsPanel.Controls.Add(_cancelButton);
        rightButtonsPanel.Controls.Add(_saveButton);

        bottomBar.Controls.Add(leftButtonsPanel, 0, 0);
        bottomBar.Controls.Add(rightButtonsPanel, 1, 0);

        Controls.Add(root);

        rightPanel.Controls.Add(_stopsGrid);
        rightPanel.Controls.Add(gridButtons);
        rightPanel.Controls.Add(_previewPanel);
        rightPanel.Controls.Add(stopsLabel);
        rightPanel.Controls.Add(namePanel);
        rightPanel.Controls.Add(headerPanel);
        root.Controls.Add(rightPanel, 1, 0);
    }

    private void ColorSchemeEditorForm_Shown(object? sender, EventArgs e) {
        _isPreviewPrimed = true;
        UpdatePreview(forceDelay: true);
    }

    private void LoadSchemeToEditor(ColorScheme scheme) {
        _isLoading = true;
        _nameTextBox.Text = scheme.Name;
        _nameTextBox.ReadOnly = ColorSchemeService.IsBuiltIn(scheme.Name);
        _gradientCheckBox.Checked = scheme.IsGradient;

        _stopsGrid.Rows.Clear();
        foreach (var stop in scheme.Stops.OrderBy(s => s.Value)) {
            var rowIdx = _stopsGrid.Rows.Add(
                stop.Value.ToString(),
                "",
                stop.Description);
            _stopsGrid.Rows[rowIdx].Tag = stop.Color;
        }
        UpdatePreview();
        _isLoading = false;
    }

    private void NewButton_Click(object? sender, EventArgs e) {
        var name = "New Scheme";
        var counter = 1;
        while (_schemes.Any(s => s.Name == name)) {
            name = $"New Scheme {counter++}";
        }

        var scheme = new ColorScheme {
            Name = name,
            Stops = [
                new() { Value = 25, Color = Color.Green, Description = "Normal" },
                new() { Value = 50, Color = Color.Yellow, Description = "Warning" },
                new() { Value = 100, Color = Color.Red, Description = "Critical" }
            ]
        };
        _colorSchemeService.SaveScheme(scheme);
        _schemes = _colorSchemeService.LoadSchemes();
        PopulateSchemeList();
        _schemeList.SelectedIndex = _schemes.FindIndex(s => s.Name == name);
        _isDirty = false;
    }

    private void PopulateSchemeList() {
        _schemeList.Items.Clear();
        foreach (var s in _schemes) {
            _schemeList.Items.Add(s.Name);
        }
    }

    private void PreviewPanel_Paint(object? sender, PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(_previewPanel.BackColor);

        if (!_isPreviewPrimed) {
            return;
        }

        var rect = _previewPanel.ClientRectangle;
        rect.Inflate(-2, -5);

        var scheme = ReadSchemeFromEditor();
        if (scheme.Stops.Count == 0) {
            using var grayBrush = new SolidBrush(Color.Gray);
            g.FillRectangle(grayBrush, rect);
            return;
        }

        var sorted = scheme.Stops.OrderBy(s => s.Value).ToList();
        double firstVal = sorted[0].Value;
        double lastVal = sorted[^1].Value;

        if (firstVal >= lastVal) {
            // Single value or identical — fill with single color
            using var brush = new SolidBrush(scheme.GetColor(firstVal));
            g.FillRectangle(brush, rect);
            return;
        }

        double minVal, maxVal;

        if (!scheme.IsGradient) {
            // Discrete mode: 5% reserved for first value on the left, right edge = last value
            var dataRange = lastVal - firstVal;
            minVal = firstVal - dataRange * 0.05 / 0.95;
            maxVal = lastVal;
        } else {
            // Gradient mode: left edge = first value, right edge = last value, no margins
            minVal = firstVal;
            maxVal = lastVal;
        }

        // Draw color bar
        for (int x = rect.X; x < rect.Right; x++) {
            var value = minVal + (maxVal - minVal) * (x - rect.X) / rect.Width;
            var color = scheme.GetColor(value);
            using var pen = new Pen(color);
            g.DrawLine(pen, x, rect.Y, x, rect.Bottom);
        }

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(120, 120, 120));
        g.DrawRectangle(borderPen, rect);
    }

    private ColorScheme ReadSchemeFromEditor() {
        var scheme = new ColorScheme {
            Name = _nameTextBox.Text.Trim(),
            IsGradient = _gradientCheckBox.Checked,
            Stops = []
        };

        foreach (DataGridViewRow row in _stopsGrid.Rows) {
            var valStr = row.Cells["Value"].Value?.ToString()?.Trim() ?? "";
            var desc = row.Cells["Description"].Value?.ToString() ?? "";
            var color = row.Tag is Color c ? c : Color.Gray;

            if (!double.TryParse(valStr, out var val)) {
                val = 0;
            }

            scheme.Stops.Add(new ColorStop {
                Value = val,
                Color = color,
                Description = desc
            });
        }
        return scheme;
    }

    // ── Stop row operations ──
    private void RemoveStop_Click(object? sender, EventArgs e) {
        if (_stopsGrid.CurrentRow == null) {
            return;
        }

        _stopsGrid.Rows.Remove(_stopsGrid.CurrentRow);
        UpdateDirtyState();
        UpdatePreview();
    }

    // ── CRUD operations ──
    private void SaveButton_Click(object? sender, EventArgs e) {
        if (!SaveCurrentScheme()) {
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private bool SaveCurrentScheme() {
        if (_currentScheme == null) {
            return false;
        }

        _stopsGrid.EndEdit();

        var scheme = ReadSchemeFromEditor();

        if (string.IsNullOrWhiteSpace(scheme.Name)) {
            ThemedMessageBox.Show(this, "Scheme name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (scheme.Stops.Count == 0) {
            ThemedMessageBox.Show(this, "At least one color stop is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // If renaming, delete old file
        if (_currentScheme.Name != scheme.Name && !ColorSchemeService.IsBuiltIn(_currentScheme.Name)) {
            _colorSchemeService.DeleteScheme(_currentScheme.Name);
        }

        _colorSchemeService.SaveScheme(scheme);
        _schemes = _colorSchemeService.LoadSchemes();
        _isDirty = false;

        var selectedName = scheme.Name;
        PopulateSchemeList();
        var idx = _schemes.FindIndex(s => s.Name == selectedName);
        _schemeList.SelectedIndex = idx >= 0 ? idx : 0;

        return true;
    }

    // ── List management ──
    private void SchemeList_SelectedIndexChanged(object? sender, EventArgs e) {
        if (_isDirty && _currentScheme != null) {
            var result = ThemedMessageBox.Show(this, $"Save changes to \"{_currentScheme.Name}\"?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) {
                SaveCurrentScheme();
            } else if (result == DialogResult.Cancel) {
                // Revert selection without re-triggering
                _schemeList.SelectedIndexChanged -= SchemeList_SelectedIndexChanged;
                var idx = _schemes.FindIndex(s => s.Name == _currentScheme.Name);
                _schemeList.SelectedIndex = idx >= 0 ? idx : 0;
                _schemeList.SelectedIndexChanged += SchemeList_SelectedIndexChanged;
                return;
            }
        }

        if (_schemeList.SelectedIndex >= 0 && _schemeList.SelectedIndex < _schemes.Count) {
            _currentScheme = _schemes[_schemeList.SelectedIndex];
            LoadSchemeToEditor(_currentScheme);
            _isDirty = false;
        }
    }

    private void SetupGridColumns() {
        _stopsGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Value",
            HeaderText = "Value",
            FillWeight = 20,
            MinimumWidth = 80,
            ValueType = typeof(string)
        });
        _stopsGrid.Columns.Add(new DataGridViewButtonColumn {
            Name = "Color",
            HeaderText = "Color",
            FillWeight = 16,
            MinimumWidth = 80,
            FlatStyle = FlatStyle.Flat,
            Text = "",
            UseColumnTextForButtonValue = false
        });
        _stopsGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Description",
            HeaderText = "Description",
            FillWeight = 64,
            MinimumWidth = 180,
            ValueType = typeof(string)
        });

        foreach (DataGridViewColumn column in _stopsGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
        }

        _stopsGrid.CellFormatting += StopsGrid_CellFormatting;
    }

    private void StopsGrid_CellClick(object? sender, DataGridViewCellEventArgs e) {
        if (e.ColumnIndex == 1 && e.RowIndex >= 0) // Color column
        {
            var row = _stopsGrid.Rows[e.RowIndex];
            var currentColor = row.Tag is Color c ? c : Color.Gray;
            if (ThemedColorPicker.ShowPicker(currentColor, out var pickedColor) == DialogResult.OK) {
                row.Tag = pickedColor;
                _stopsGrid.InvalidateRow(e.RowIndex);
                UpdateDirtyState();
                UpdatePreview();
            }
        }
    }

    private void StopsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) {
        if (e.ColumnIndex == 1 && e.RowIndex >= 0) // Color column
        {
            var row = _stopsGrid.Rows[e.RowIndex];
            if (row.Tag is Color color) {
                e.Value = "";
                e.CellStyle!.BackColor = color;
                e.CellStyle.SelectionBackColor = color;
            }
        }
    }

    private void SortGridByValue() {
        if (_isLoading || _stopsGrid.Rows.Count <= 1) {
            return;
        }

        // Collect all rows data
        var rows = new List<(double Value, Color Color, string Description, string ValueStr)>();
        foreach (DataGridViewRow row in _stopsGrid.Rows) {
            var valStr = row.Cells["Value"].Value?.ToString()?.Trim() ?? "0";
            var desc = row.Cells["Description"].Value?.ToString() ?? "";
            var color = row.Tag is Color c ? c : Color.Gray;
            double.TryParse(valStr, out var val);
            rows.Add((val, color, desc, valStr));
        }

        var sorted = rows.OrderBy(r => r.Value).ToList();

        // Check if already sorted
        bool alreadySorted = true;
        for (int i = 0; i < sorted.Count; i++) {
            var currentVal = _stopsGrid.Rows[i].Cells["Value"].Value?.ToString()?.Trim() ?? "0";
            double.TryParse(currentVal, out var cv);
            if (cv != sorted[i].Value || (_stopsGrid.Rows[i].Tag is Color cc ? cc.ToArgb() : 0) != sorted[i].Color.ToArgb()) {
                alreadySorted = false;
                break;
            }
        }
        if (alreadySorted) {
            return;
        }

        _isLoading = true;
        _stopsGrid.Rows.Clear();
        foreach (var item in sorted) {
            var rowIdx = _stopsGrid.Rows.Add(item.ValueStr, "", item.Description);
            _stopsGrid.Rows[rowIdx].Tag = item.Color;
        }
        _isLoading = false;
    }

    // ── Preview ──

    private void PreviewUpdateTimer_Tick(object? sender, EventArgs e) {
        _previewUpdateTimer.Stop();
        if (!_previewPanel.IsDisposed) {
            _previewPanel.Invalidate();
        }
    }

    private void UpdatePreview(bool forceDelay = false) {
        if (!_isPreviewPrimed && !forceDelay) {
            return;
        }

        if (_previewPanel.IsDisposed) {
            return;
        }

        if (_previewPanel.IsHandleCreated) {
            _previewPanel.BeginInvoke(new MethodInvoker(() => {
                if (_previewPanel.IsDisposed) {
                    return;
                }

                _previewUpdateTimer.Stop();
                _previewUpdateTimer.Start();
            }));
            return;
        }

        _previewUpdateTimer.Stop();
        _previewUpdateTimer.Start();
    }

    private void UpdateDirtyState() {
        if (_isLoading) {
            return;
        }

        if (_currentScheme == null) {
            _isDirty = false;
            return;
        }

        _isDirty = !AreSchemesEqual(_currentScheme, ReadSchemeFromEditor());
    }

    private static bool AreSchemesEqual(ColorScheme original, ColorScheme updated) {
        if (!string.Equals(original.Name?.Trim() ?? "", updated.Name?.Trim() ?? "", StringComparison.Ordinal)) {
            return false;
        }

        if (original.IsGradient != updated.IsGradient) {
            return false;
        }

        if (original.Stops.Count != updated.Stops.Count) {
            return false;
        }

        var origSorted = original.Stops.OrderBy(s => s.Value).ToList();
        var updSorted = updated.Stops.OrderBy(s => s.Value).ToList();

        for (int i = 0; i < origSorted.Count; i++) {
            var originalStop = origSorted[i];
            var updatedStop = updSorted[i];

            if (originalStop.Value != updatedStop.Value) {
                return false;
            }

            if (originalStop.Color.ToArgb() != updatedStop.Color.ToArgb()) {
                return false;
            }

            if (!string.Equals(originalStop.Description ?? "", updatedStop.Description ?? "", StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    private sealed class PreviewPanel : Panel {
        public PreviewPanel() {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }

    #endregion Private Methods
}
