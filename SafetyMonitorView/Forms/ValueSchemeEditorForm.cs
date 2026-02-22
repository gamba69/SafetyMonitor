using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

public class ValueSchemeEditorForm : Form {
    #region Private Fields

    private readonly ValueSchemeService _valueSchemeService;
    private RadioButton _ascendingButton = null!;
    private Button _cancelButton = null!;
    private ValueScheme? _currentScheme;
    private Button _deleteButton = null!;
    private RadioButton _descendingButton = null!;
    private Button _duplicateButton = null!;
    private bool _isDirty;
    private bool _isLoading;
    private TextBox _nameTextBox = null!;
    private Button _newButton = null!;
    private Panel _previewPanel = null!;
    private bool _isPreviewPrimed;
    private Button _saveButton = null!;
    private ListBox _schemeList = null!;
    private List<ValueScheme> _schemes;
    private Panel _sortSegmentPanel = null!;
    private static Bitmap? MirrorIconVertically(Bitmap? source) {
        if (source == null) {
            return null;
        }

        var mirrored = (Bitmap)source.Clone();
        mirrored.RotateFlip(RotateFlipType.RotateNoneFlipY);
        source.Dispose();
        return mirrored;
    }
    private DataGridView _stopsGrid = null!;

    #endregion Private Fields

    #region Public Constructors

    public ValueSchemeEditorForm() {
        _valueSchemeService = new ValueSchemeService();
        _schemes = _valueSchemeService.LoadSchemes();
        InitializeComponent();
        ApplyTheme();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewValueSchemes);
        PopulateSchemeList();
        if (_schemeList.Items.Count > 0) {
            _schemeList.SelectedIndex = 0;
        }

        Shown += ValueSchemeEditorForm_Shown;
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

    #endregion Protected Methods

    #region Private Methods

    private void AddStop_Click(object? sender, EventArgs e) {
        _stopsGrid.Rows.Add("0", "", "");
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

        if (ValueSchemeService.IsBuiltIn(_currentScheme.Name)) {
            ThemedMessageBox.Show(this, "Built-in value schemes cannot be deleted.\nYou can duplicate it and modify the copy.",
                "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (ThemedMessageBox.Show(this, $"Delete value scheme \"{_currentScheme.Name}\"?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) {
            return;
        }

        _valueSchemeService.DeleteScheme(_currentScheme.Name);
        _schemes = _valueSchemeService.LoadSchemes();
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
        _valueSchemeService.SaveScheme(source);
        _schemes = _valueSchemeService.LoadSchemes();
        PopulateSchemeList();
        _schemeList.SelectedIndex = _schemes.FindIndex(s => s.Name == name);
        _isDirty = false;
    }

    private void InitializeComponent() {
        Text = "Value Schemes";
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
        var listLabel = new Label { Text = "Value Schemes:", Font = titleFont, Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
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
            Text = "Configure value schemes that transform metric values into descriptive text labels for value tiles and chart inspector.",
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
        var textLabel = new Label {
            Text = "• Text is the label shown when value matches the stop.",
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
            Text = "• Preview shows how the scheme will map values.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        headerPanel.Controls.Add(rangesLabel, 0, 1);
        headerPanel.Controls.Add(textLabel, 1, 1);
        headerPanel.Controls.Add(descriptionLabel, 0, 2);
        headerPanel.Controls.Add(previewLabel, 1, 2);

        const int editorFieldLabelWidth = 56;
        const int sortModeFieldWidth = 298;

        var namePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 35, WrapContents = false, AutoSize = false };
        namePanel.Controls.Add(new Label {
            Text = "Name:",
            Font = titleFont,
            AutoSize = false,
            Width = editorFieldLabelWidth,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 5, 0)
        });
        _nameTextBox = new TextBox { Width = sortModeFieldWidth, Font = normalFont, Margin = new Padding(0, 2, 15, 0) };
        _nameTextBox.TextChanged += (s, e) => UpdateDirtyState();
        namePanel.Controls.Add(_nameTextBox);

        var sortTogglePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, WrapContents = false, AutoSize = false, Padding = new Padding(0, 4, 0, 4) };
        var sortLabel = new Label {
            Text = "Order:",
            Font = titleFont,
            AutoSize = false,
            Width = editorFieldLabelWidth,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 5, 0)
        };

        _ascendingButton = new RadioButton {
            Text = "Ascending",
            Appearance = Appearance.Button,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
            Font = normalFont,
            Padding = new Padding(8, 0, 8, 0),
            AutoSize = false,
            Size = new Size(142, 30),
            Checked = true,
            Cursor = Cursors.Hand
        };
        _ascendingButton.CheckedChanged += (s, e) => { if (_ascendingButton.Checked) { SortGridByValue(); UpdateDirtyState(); UpdatePreview(); UpdateSortToggleAppearance(); } };

        _descendingButton = new RadioButton {
            Text = "Descending",
            Appearance = Appearance.Button,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0, CheckedBackColor = Color.Transparent, MouseDownBackColor = Color.Transparent, MouseOverBackColor = Color.Transparent },
            Font = normalFont,
            Padding = new Padding(8, 0, 8, 0),
            AutoSize = false,
            Size = new Size(154, 30),
            Checked = false,
            Cursor = Cursors.Hand
        };
        _descendingButton.CheckedChanged += (s, e) => { if (_descendingButton.Checked) { SortGridByValue(); UpdateDirtyState(); UpdatePreview(); UpdateSortToggleAppearance(); } };

        _sortSegmentPanel = new Panel { Size = new Size(sortModeFieldWidth, 32), Padding = new Padding(1), Margin = new Padding(0, 2, 0, 0) };
        _ascendingButton.Location = new Point(1, 1);
        _descendingButton.Location = new Point(143, 1);
        _sortSegmentPanel.Controls.Add(_ascendingButton);
        _sortSegmentPanel.Controls.Add(_descendingButton);

        sortTogglePanel.Controls.Add(sortLabel);
        sortTogglePanel.Controls.Add(_sortSegmentPanel);

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
        _stopsGrid.CellValueChanged += (s, e) => { SortGridByValue(); UpdateDirtyState(); UpdatePreview(); };
        _stopsGrid.CellEndEdit += (s, e) => { SortGridByValue(); UpdateDirtyState(); UpdatePreview(); };

        var gridButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 3, 0, 3) };
        var addStopBtn = new Button { Text = "Add Stop", Width = 110, Height = 32, Font = normalFont };
        addStopBtn.Click += AddStop_Click;
        var removeStopBtn = new Button { Text = "Del Stop", Width = 110, Height = 32, Font = normalFont };
        removeStopBtn.Click += RemoveStop_Click;
        gridButtons.Controls.AddRange([addStopBtn, removeStopBtn]);

        _previewPanel = new PreviewPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(0, 6, 0, 0) };
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

        _newButton = new Button { Text = "Add", Width = 80, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 10, 0) };
        _newButton.Click += NewButton_Click;
        _duplicateButton = new Button { Text = "Dup", Width = 80, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 10, 0) };
        _duplicateButton.Click += DuplicateButton_Click;
        _deleteButton = new Button { Text = "Del", Width = 80, Height = 35, Font = normalFont, Margin = new Padding(0, 5, 0, 0) };
        _deleteButton.Click += DeleteButton_Click;

        leftButtonsPanel.Controls.AddRange([_newButton, _duplicateButton, _deleteButton]);

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
        rightPanel.Controls.Add(sortTogglePanel);
        rightPanel.Controls.Add(namePanel);
        rightPanel.Controls.Add(headerPanel);
        root.Controls.Add(rightPanel, 1, 0);
    }

    private void ValueSchemeEditorForm_Shown(object? sender, EventArgs e) {
        _isPreviewPrimed = true;
        UpdatePreview();
        UpdateSortToggleAppearance();
    }

    private void LoadSchemeToEditor(ValueScheme scheme) {
        _isLoading = true;
        _nameTextBox.Text = scheme.Name;
        _nameTextBox.ReadOnly = ValueSchemeService.IsBuiltIn(scheme.Name);
        _descendingButton.Checked = scheme.Descending;
        _ascendingButton.Checked = !scheme.Descending;

        _stopsGrid.Rows.Clear();
        var orderedStops = scheme.Descending
            ? scheme.Stops.OrderByDescending(s => s.Value)
            : scheme.Stops.OrderBy(s => s.Value);
        foreach (var stop in orderedStops) {
            _stopsGrid.Rows.Add(
                stop.Value.ToString(),
                stop.Text,
                stop.Description);
        }
        UpdatePreview();
        UpdateSortToggleAppearance();
        _isLoading = false;
    }

    private void NewButton_Click(object? sender, EventArgs e) {
        var name = "New Scheme";
        var counter = 1;
        while (_schemes.Any(s => s.Name == name)) {
            name = $"New Scheme {counter++}";
        }

        var scheme = new ValueScheme {
            Name = name,
            Stops = [
                new() { Value = 0, Text = "OK", Description = "Normal" },
                new() { Value = 50, Text = "WARN", Description = "Warning" },
                new() { Value = 100, Text = "CRIT", Description = "Critical" }
            ]
        };
        _valueSchemeService.SaveScheme(scheme);
        _schemes = _valueSchemeService.LoadSchemes();
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

        var sorted = scheme.Descending
            ? scheme.Stops.OrderByDescending(s => s.Value).ToList()
            : scheme.Stops.OrderBy(s => s.Value).ToList();
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var textColor = isLight ? Color.Black : Color.White;
        var borderColor = Color.FromArgb(120, 120, 120);
        var comparisonSymbol = scheme.Descending ? "\u2265" : "\u2264";

        using var borderPen = new Pen(borderColor);
        g.DrawRectangle(borderPen, rect);

        // Draw segments with text labels
        var segmentCount = sorted.Count;
        var segmentWidth = rect.Width / Math.Max(1, segmentCount);
        var previewFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        var smallFont = new Font("Segoe UI", 6.5f);

        Color[] segmentColors = [
            Color.FromArgb(60, 76, 175, 80),
            Color.FromArgb(60, 255, 152, 0),
            Color.FromArgb(60, 244, 67, 54),
            Color.FromArgb(60, 156, 39, 176),
            Color.FromArgb(60, 33, 150, 243),
            Color.FromArgb(60, 0, 150, 136)
        ];

        for (int i = 0; i < segmentCount; i++) {
            var segRect = new Rectangle(rect.X + i * segmentWidth, rect.Y + 1, segmentWidth, rect.Height - 2);
            if (i == segmentCount - 1) {
                segRect.Width = rect.Right - segRect.X - 1;
            }

            using var fillBrush = new SolidBrush(segmentColors[i % segmentColors.Length]);
            g.FillRectangle(fillBrush, segRect);

            if (i > 0) {
                using var sepPen = new Pen(borderColor);
                g.DrawLine(sepPen, segRect.X, segRect.Y, segRect.X, segRect.Bottom);
            }

            var textLabel = sorted[i].Text;
            var valueLabel = $"{comparisonSymbol}{sorted[i].Value}";
            using var textBrush = new SolidBrush(textColor);

            var textSize = g.MeasureString(textLabel, previewFont);
            var valueSize = g.MeasureString(valueLabel, smallFont);

            var textX = segRect.X + (segRect.Width - textSize.Width) / 2;
            var totalHeight = textSize.Height + valueSize.Height + 1;
            var textY = segRect.Y + (segRect.Height - totalHeight) / 2;
            var valueX = segRect.X + (segRect.Width - valueSize.Width) / 2;
            g.DrawString(textLabel, previewFont, textBrush, textX, textY);
            g.DrawString(valueLabel, smallFont, textBrush, valueX, textY + textSize.Height + 1);
        }

        previewFont.Dispose();
        smallFont.Dispose();
    }

    private ValueScheme ReadSchemeFromEditor() {
        var scheme = new ValueScheme {
            Name = _nameTextBox.Text.Trim(),
            Descending = _descendingButton.Checked,
            Stops = []
        };

        foreach (DataGridViewRow row in _stopsGrid.Rows) {
            var valStr = row.Cells["Value"].Value?.ToString()?.Trim() ?? "";
            var text = row.Cells["Text"].Value?.ToString() ?? "";
            var desc = row.Cells["Description"].Value?.ToString() ?? "";

            if (!double.TryParse(valStr, out var val)) {
                val = 0;
            }

            scheme.Stops.Add(new ValueStop {
                Value = val,
                Text = text,
                Description = desc
            });
        }
        return scheme;
    }

    private void RemoveStop_Click(object? sender, EventArgs e) {
        if (_stopsGrid.CurrentRow == null) {
            return;
        }

        _stopsGrid.Rows.Remove(_stopsGrid.CurrentRow);
        UpdateDirtyState();
        UpdatePreview();
    }

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
            ThemedMessageBox.Show(this, "At least one value stop is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (_currentScheme.Name != scheme.Name && !ValueSchemeService.IsBuiltIn(_currentScheme.Name)) {
            _valueSchemeService.DeleteScheme(_currentScheme.Name);
        }

        _valueSchemeService.SaveScheme(scheme);
        _schemes = _valueSchemeService.LoadSchemes();
        _isDirty = false;

        var selectedName = scheme.Name;
        PopulateSchemeList();
        var idx = _schemes.FindIndex(s => s.Name == selectedName);
        _schemeList.SelectedIndex = idx >= 0 ? idx : 0;

        return true;
    }

    private void SchemeList_SelectedIndexChanged(object? sender, EventArgs e) {
        if (_isDirty && _currentScheme != null) {
            var result = ThemedMessageBox.Show(this, $"Save changes to \"{_currentScheme.Name}\"?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) {
                SaveCurrentScheme();
            } else if (result == DialogResult.Cancel) {
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
        _stopsGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Text",
            HeaderText = "Text",
            FillWeight = 30,
            MinimumWidth = 100,
            ValueType = typeof(string)
        });
        _stopsGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Description",
            HeaderText = "Description",
            FillWeight = 50,
            MinimumWidth = 180,
            ValueType = typeof(string)
        });

        foreach (DataGridViewColumn column in _stopsGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
        }
    }

    private void SortGridByValue() {
        if (_isLoading || _stopsGrid.Rows.Count <= 1) {
            return;
        }

        var rows = new List<(double Value, string Text, string Description, string ValueStr)>();
        foreach (DataGridViewRow row in _stopsGrid.Rows) {
            var valStr = row.Cells["Value"].Value?.ToString()?.Trim() ?? "0";
            var text = row.Cells["Text"].Value?.ToString() ?? "";
            var desc = row.Cells["Description"].Value?.ToString() ?? "";
            double.TryParse(valStr, out var val);
            rows.Add((val, text, desc, valStr));
        }

        var sorted = _descendingButton.Checked
            ? rows.OrderByDescending(r => r.Value).ToList()
            : rows.OrderBy(r => r.Value).ToList();

        bool alreadySorted = true;
        for (int i = 0; i < sorted.Count; i++) {
            var currentVal = _stopsGrid.Rows[i].Cells["Value"].Value?.ToString()?.Trim() ?? "0";
            double.TryParse(currentVal, out var cv);
            if (cv != sorted[i].Value) {
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
            _stopsGrid.Rows.Add(item.ValueStr, item.Text, item.Description);
        }
        _isLoading = false;
    }

    private void UpdateSortToggleAppearance() {
        if (_sortSegmentPanel == null || _ascendingButton == null || _descendingButton == null) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var segmentBg = isLight ? Color.FromArgb(225, 232, 235) : Color.FromArgb(45, 58, 64);
        var activeBg = isLight ? Color.White : Color.FromArgb(62, 77, 84);
        var inactiveFg = isLight ? Color.FromArgb(78, 90, 96) : Color.FromArgb(186, 198, 205);
        var activeFg = isLight ? Color.FromArgb(21, 28, 31) : Color.White;
        var borderColor = isLight ? Color.FromArgb(196, 206, 211) : Color.FromArgb(70, 85, 92);

        _sortSegmentPanel.BackColor = borderColor;
        _ascendingButton.BackColor = _ascendingButton.Checked ? activeBg : segmentBg;
        _descendingButton.BackColor = _descendingButton.Checked ? activeBg : segmentBg;
        _ascendingButton.ForeColor = _ascendingButton.Checked ? activeFg : inactiveFg;
        _descendingButton.ForeColor = _descendingButton.Checked ? activeFg : inactiveFg;

        var iconColor = isLight ? Color.FromArgb(35, 47, 52) : Color.FromArgb(223, 234, 239);
        _ascendingButton.Image = MirrorIconVertically(MaterialIcons.GetIcon(MaterialIcons.Sort, iconColor, 22));
        _descendingButton.Image = MaterialIcons.GetIcon(MaterialIcons.Sort, iconColor, 22);
        _ascendingButton.ImageAlign = ContentAlignment.MiddleLeft;
        _descendingButton.ImageAlign = ContentAlignment.MiddleLeft;
        _ascendingButton.TextAlign = ContentAlignment.MiddleLeft;
        _descendingButton.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void UpdatePreview() {
        if (!_isPreviewPrimed) {
            return;
        }

        if (_previewPanel.IsDisposed) {
            return;
        }

        _previewPanel.Invalidate();
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

    private static bool AreSchemesEqual(ValueScheme original, ValueScheme updated) {
        if (!string.Equals(original.Name?.Trim() ?? "", updated.Name?.Trim() ?? "", StringComparison.Ordinal)) {
            return false;
        }

        if (original.Descending != updated.Descending) {
            return false;
        }

        if (original.Stops.Count != updated.Stops.Count) {
            return false;
        }

        var origSorted = original.Stops.OrderBy(s => s.Value).ToList();
        var updSorted = updated.Stops.OrderBy(s => s.Value).ToList();

        for (int i = 0; i < origSorted.Count; i++) {
            if (origSorted[i].Value != updSorted[i].Value) {
                return false;
            }

            if (!string.Equals(origSorted[i].Text ?? "", updSorted[i].Text ?? "", StringComparison.Ordinal)) {
                return false;
            }

            if (!string.Equals(origSorted[i].Description ?? "", updSorted[i].Description ?? "", StringComparison.Ordinal)) {
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
