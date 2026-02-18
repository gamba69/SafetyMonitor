using DataStorage.Models;
using MaterialSkin;
using SafetyMonitorView.Services;
using SafetyMonitorView.Models;
using System.Collections;

namespace SafetyMonitorView.Forms;

public class ChartTileEditorForm : Form {

    #region Private Types

    private sealed class MetricRowColors {

        #region Public Fields

        public Color Dark = Color.LightSkyBlue;
        public Color Light = Color.Blue;

        #endregion Public Fields
    }

    #endregion Private Types

    #region Private Fields

    private readonly ChartTileConfig _config;
    private readonly Dashboard _dashboard;
    private Color _inputBackColor;
    private Color _inputForeColor;

    private Button _cancelButton = null!;
    private NumericUpDown _columnSpanNumeric = null!;
    private DataGridView _metricsGrid = null!;
    private ComboBox _periodComboBox = null!;
    private List<ChartPeriodPreset> _periodPresets = [];
    private NumericUpDown _rowSpanNumeric = null!;
    private Button _saveButton = null!;
    private CheckBox _showGridCheckBox = null!;
    private CheckBox _showHoverInspectorCheckBox = null!;
    private CheckBox _showLegendCheckBox = null!;
    private TextBox _titleTextBox = null!;

    #endregion Private Fields

    #region Public Constructors

    public ChartTileEditorForm(ChartTileConfig config, Dashboard dashboard) {
        _config = config;
        _dashboard = dashboard;

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.WindowTileChart);
        ApplyTheme();
        LoadConfig();
    }

    #endregion Public Constructors

    #region Private Methods

    private static Panel CreateLabeledControl(string labelText, Control control, Font labelFont) {
        var panel = new Panel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 5, 0, 5) };
        var label = new Label { Text = labelText, Font = labelFont, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 5) };
        control.Dock = DockStyle.Top;
        panel.Controls.Add(control);
        panel.Controls.Add(label);
        return panel;
    }

    private void AddMetricButton_Click(object? sender, EventArgs e) {
        var newRow = _metricsGrid.Rows.Add(MetricType.Temperature, AggregationFunction.Average, "Metric", null!, null!, 2.0f, false, 0.5f, false);
        _metricsGrid.Rows[newRow].Tag = new MetricRowColors();
        _metricsGrid.InvalidateRow(newRow);
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        _inputBackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _inputForeColor = isLight ? Color.Black : Color.White;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = _inputForeColor;

        // DataGridView special handling
        _metricsGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        _metricsGrid.DefaultCellStyle.BackColor = _inputBackColor;
        _metricsGrid.DefaultCellStyle.ForeColor = _inputForeColor;
        var darkSelectionColor = Color.FromArgb(0, 121, 107);
        _metricsGrid.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(33, 150, 243) : darkSelectionColor;
        _metricsGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        _metricsGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(53, 70, 76);
        _metricsGrid.ColumnHeadersDefaultCellStyle.ForeColor = _inputForeColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(240, 240, 240) : darkSelectionColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _inputForeColor;
        _metricsGrid.EnableHeadersVisualStyles = false;
        _metricsGrid.GridColor = isLight ? Color.LightGray : Color.FromArgb(70, 90, 98);

        foreach (var comboBoxColumn in _metricsGrid.Columns.OfType<DataGridViewComboBoxColumn>()) {
            comboBoxColumn.FlatStyle = FlatStyle.Flat;
            comboBoxColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            comboBoxColumn.DefaultCellStyle.BackColor = _inputBackColor;
            comboBoxColumn.DefaultCellStyle.ForeColor = _inputForeColor;
            comboBoxColumn.DefaultCellStyle.SelectionBackColor = _metricsGrid.DefaultCellStyle.SelectionBackColor;
            comboBoxColumn.DefaultCellStyle.SelectionForeColor = _metricsGrid.DefaultCellStyle.SelectionForeColor;
        }

        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            switch (control) {
                case Label lbl:
                    lbl.ForeColor = isLight ? Color.Black : Color.White;
                    break;

                case Button btn:
                    ThemedButtonStyler.Apply(btn, isLight);
                    break;

                case TextBox txt:
                    txt.BackColor = _inputBackColor;
                    txt.ForeColor = _inputForeColor;
                    break;

                case ComboBox cmb:
                    ThemedComboBoxStyler.Apply(cmb, isLight);
                    break;

                case NumericUpDown num:
                    num.BackColor = _inputBackColor;
                    num.ForeColor = _inputForeColor;
                    break;

                case CheckBox chk:
                    chk.ForeColor = isLight ? Color.Black : Color.White;
                    break;

            }
            ApplyThemeRecursive(control, isLight);
        }
    }

    private void InitializeComponent() {
        Text = "Chart Tile Editor";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Padding = new Padding(15);

        var titleFont = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold);
        var normalFont = CreateSafeFont("Segoe UI", 9.5f);

        // Main layout
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Metrics label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); // 2: Metrics grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Add/Remove buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Period row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: Size row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 6: Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 7: Buttons

        // Row 0: Title
        var titlePanel = CreateLabeledControl("Title:", _titleTextBox = new TextBox { Font = normalFont, Dock = DockStyle.Fill }, titleFont);
        mainLayout.Controls.Add(titlePanel, 0, 0);

        // Row 1: Metrics label
        var metricsLabel = new Label { Text = "Metrics and Aggregations:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 10, 0, 5) };
        mainLayout.Controls.Add(metricsLabel, 0, 1);

        // Row 2: Metrics grid
        _metricsGrid = new DataGridView {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 5)
        };

        _metricsGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "Metric",
            HeaderText = "Metric",
            FillWeight = 32,
            DataSource = Enum.GetValues<MetricType>().Select(m => new { Value = m, Display = m.GetDisplayName() }).ToList(),
            DisplayMember = "Display",
            ValueMember = "Value",
            ValueType = typeof(MetricType)
        });

        _metricsGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "Function",
            HeaderText = "Agg.",
            FillWeight = 22,
            DataSource = Enum.GetValues<AggregationFunction>()
                .Select(f => new { Value = f, Display = f.ToString() })
                .ToList(),
            DisplayMember = "Display",
            ValueMember = "Value",
            ValueType = typeof(AggregationFunction)
        });

        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label", FillWeight = 18 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LightColor", HeaderText = "Light", FillWeight = 8, ReadOnly = true });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DarkColor", HeaderText = "Dark", FillWeight = 8, ReadOnly = true });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineWidth", HeaderText = "W", FillWeight = 7 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Smooth", HeaderText = "Smth", FillWeight = 8 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tension", HeaderText = "Tns", FillWeight = 8 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "ShowMarkers", HeaderText = "Mark", FillWeight = 12 });

        foreach (DataGridViewColumn column in _metricsGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _metricsGrid.CellClick += MetricsGrid_CellClick;
        _metricsGrid.CellFormatting += MetricsGrid_CellFormatting;
        _metricsGrid.DataError += MetricsGrid_DataError;
        _metricsGrid.EditingControlShowing += MetricsGrid_EditingControlShowing;
        mainLayout.Controls.Add(_metricsGrid, 0, 2);

        // Row 3: Add/Remove buttons
        var gridButtonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 10) };
        var addMetricButton = new Button { Text = "Add Metric", Width = 130, Height = 32, Font = normalFont, Margin = new Padding(0, 0, 10, 0) };
        addMetricButton.Click += AddMetricButton_Click;
        gridButtonPanel.Controls.Add(addMetricButton);
        var removeMetricButton = new Button { Text = "Delete", Width = 110, Height = 32, Font = normalFont, Margin = new Padding(0) };
        removeMetricButton.Click += RemoveMetricButton_Click;
        gridButtonPanel.Controls.Add(removeMetricButton);
        mainLayout.Controls.Add(gridButtonPanel, 0, 3);

        // Row 4: Period
        var periodPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 5) };
        periodPanel.Controls.Add(new Label { Text = "Period:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _periodComboBox = new ComboBox { Width = 140, Font = normalFont, Margin = new Padding(0, 0, 15, 0), DropDownStyle = ComboBoxStyle.DropDownList };
        LoadPeriodPresets();
        periodPanel.Controls.Add(_periodComboBox);
        mainLayout.Controls.Add(periodPanel, 0, 4);

        // Row 5: Options + Size
        var sizePanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 10) };
        _showLegendCheckBox = new CheckBox { Text = "Legend", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 4, 10, 0) };
        sizePanel.Controls.Add(_showLegendCheckBox);
        _showGridCheckBox = new CheckBox { Text = "Grid", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 4, 15, 0) };
        sizePanel.Controls.Add(_showGridCheckBox);
        _showHoverInspectorCheckBox = new CheckBox { Text = "Hover Inspector", Font = normalFont, AutoSize = true, Checked = false, Margin = new Padding(0, 4, 15, 0) };
        sizePanel.Controls.Add(_showHoverInspectorCheckBox);
        sizePanel.Controls.Add(new Label { Text = "Size:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 10, 0) });
        sizePanel.Controls.Add(new Label { Text = "Rows:", Font = normalFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _rowSpanNumeric = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 2, Font = normalFont, Margin = new Padding(0, 0, 15, 0) };
        sizePanel.Controls.Add(_rowSpanNumeric);
        sizePanel.Controls.Add(new Label { Text = "Columns:", Font = normalFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _columnSpanNumeric = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 2, Font = normalFont };
        sizePanel.Controls.Add(_columnSpanNumeric);
        mainLayout.Controls.Add(sizePanel, 0, 5);

        // Row 6: Spacer (empty)
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 6);

        // Row 7: Buttons
        var buttonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 10, 0, 0) };
        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont, Margin = new Padding(0) };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);
        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);
        mainLayout.Controls.Add(buttonPanel, 0, 7);

        Controls.Add(mainLayout);

        // Set form size
        MinimumSize = new Size(760, 590);
        ClientSize = new Size(760, 550);
    }
    private void LoadConfig() {
        _titleTextBox.Text = _config.Title;
        SetSelectedPeriodPreset();

        _showLegendCheckBox.Checked = _config.ShowLegend;
        _showGridCheckBox.Checked = _config.ShowGrid;
        _showHoverInspectorCheckBox.Checked = _config.ShowHoverInspector;
        _rowSpanNumeric.Value = _config.RowSpan;
        _columnSpanNumeric.Value = _config.ColumnSpan;

        foreach (var agg in _config.MetricAggregations) {
            var rowIndex = _metricsGrid.Rows.Add(agg.Metric, agg.Function, agg.Label, null!, null!, agg.LineWidth, agg.Smooth, agg.Tension, agg.ShowMarkers);
            _metricsGrid.Rows[rowIndex].Tag = new MetricRowColors {
                Light = agg.Color,
                Dark = agg.DarkThemeColor.IsEmpty ? agg.Color : agg.DarkThemeColor
            };
            _metricsGrid.InvalidateRow(rowIndex);
        }
    }
    private void MetricsGrid_CellClick(object? sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0) {
            return;
        }

        var columnName = _metricsGrid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("LightColor" or "DarkColor")) {
            return;
        }

        var rowColors = EnsureRowColors(_metricsGrid.Rows[e.RowIndex]);
        var currentColor = columnName == "LightColor" ? rowColors.Light : rowColors.Dark;
        if (ThemedColorPicker.ShowPicker(currentColor, out var pickedColor) != DialogResult.OK) {
            return;
        }

        if (columnName == "LightColor") {
            rowColors.Light = pickedColor;
        } else {
            rowColors.Dark = pickedColor;
        }

        _metricsGrid.InvalidateRow(e.RowIndex);
    }

    private void MetricsGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e) {
        if (e.RowIndex < 0) {
            return;
        }

        var columnName = _metricsGrid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("LightColor" or "DarkColor")) {
            return;
        }

        var colors = EnsureRowColors(_metricsGrid.Rows[e.RowIndex]);
        var color = columnName == "LightColor" ? colors.Light : colors.Dark;
        e.Value = string.Empty;
        e.CellStyle!.BackColor = color;
        e.CellStyle.SelectionBackColor = color;
    }

    private void MetricsGrid_DataError(object? sender, DataGridViewDataErrorEventArgs e) {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) {
            return;
        }

        if (e.Context.HasFlag(DataGridViewDataErrorContexts.Commit)) {
            e.ThrowException = false;
            return;
        }

        if (_metricsGrid.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn comboColumn) {
            var cellValue = _metricsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            if (cellValue is string rawValue && comboColumn.ValueType?.IsEnum == true) {
                if (Enum.TryParse(comboColumn.ValueType, rawValue, true, out var parsed)) {
                    _metricsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = parsed;
                    e.ThrowException = false;
                    return;
                }
            }

            if (e.Context.HasFlag(DataGridViewDataErrorContexts.Formatting)) {
                var fallback = GetComboBoxFallbackValue(comboColumn);
                if (fallback != null) {
                    _metricsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = fallback;
                }
            }
        }

        e.ThrowException = false;
    }

    private void MetricsGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (e.Control is ComboBox comboBox) {
            comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
            var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
            ThemedComboBoxStyler.Apply(comboBox, isLight);
        }
    }


    private static float ParseTension(string? rawValue) {
        if (!float.TryParse(rawValue, out var tension)) {
            return 0.5f;
        }

        return Math.Clamp(tension, 0f, 3f);
    }

    private static MetricRowColors EnsureRowColors(DataGridViewRow row) {
        if (row.Tag is MetricRowColors colors) {
            return colors;
        }

        if (row.Tag is Color color) {
            var migratedColors = new MetricRowColors { Light = color, Dark = color };
            row.Tag = migratedColors;
            return migratedColors;
        }

        var defaultColors = new MetricRowColors();
        row.Tag = defaultColors;
        return defaultColors;
    }

    private static object? GetComboBoxFallbackValue(DataGridViewComboBoxColumn column) {
        if (!string.IsNullOrWhiteSpace(column.ValueMember) && column.DataSource is IEnumerable source) {
            var first = source.Cast<object>().FirstOrDefault();
            if (first != null) {
                var property = first.GetType().GetProperty(column.ValueMember);
                if (property != null) {
                    return property.GetValue(first);
                }
            }
        }

        return column.Items.Count > 0 ? column.Items[0] : null;
    }

    private void RemoveMetricButton_Click(object? sender, EventArgs e) {
        if (_metricsGrid.SelectedRows.Count > 0) {
            foreach (DataGridViewRow row in _metricsGrid.SelectedRows) {
                if (!row.IsNewRow) {
                    _metricsGrid.Rows.Remove(row);
                }
            }
        }
    }
    private void SaveButton_Click(object? sender, EventArgs e) {
        var newRowSpan = (int)_rowSpanNumeric.Value;
        var newColumnSpan = (int)_columnSpanNumeric.Value;
        var oldRowSpan = _config.RowSpan;
        var oldColumnSpan = _config.ColumnSpan;

        _config.RowSpan = newRowSpan;
        _config.ColumnSpan = newColumnSpan;
        if (!_dashboard.CanPlaceTile(_config)) {
            _config.RowSpan = oldRowSpan;
            _config.ColumnSpan = oldColumnSpan;
            ThemedMessageBox.Show(this,
                "Tile with selected size does not fit the dashboard at its current position.",
                "Invalid Size",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _config.Title = _titleTextBox.Text;
        if (_periodComboBox.SelectedIndex < 0 || _periodComboBox.SelectedIndex >= _periodPresets.Count) {
            ThemedMessageBox.Show(this, "Please select a chart period preset.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedPreset = _periodPresets[_periodComboBox.SelectedIndex];
        _config.PeriodPresetUid = selectedPreset.Uid;
        _config.Period = selectedPreset.Period;
        _config.CustomPeriodDuration = selectedPreset.Period == ChartPeriod.Custom ? selectedPreset.Duration : null;
        _config.CustomAggregationInterval = selectedPreset.AggregationInterval;

        _config.CustomEndTime = null;

        _config.ShowLegend = _showLegendCheckBox.Checked;
        _config.ShowGrid = _showGridCheckBox.Checked;
        _config.ShowHoverInspector = _showHoverInspectorCheckBox.Checked;
        _config.MetricAggregations.Clear();
        foreach (DataGridViewRow row in _metricsGrid.Rows) {
            if (row.Cells["Metric"].Value == null) {
                continue;
            }

            var colors = EnsureRowColors(row);
            var agg = new MetricAggregation {
                Metric = (MetricType)row.Cells["Metric"].Value!,
                Function = (AggregationFunction)row.Cells["Function"].Value!,
                Label = row.Cells["Label"].Value?.ToString() ?? "",
                Color = colors.Light,
                DarkThemeColor = colors.Dark,
                LineWidth = float.Parse(row.Cells["LineWidth"].Value?.ToString() ?? "2"),
                Smooth = (bool)(row.Cells["Smooth"].Value ?? false),
                Tension = ParseTension(row.Cells["Tension"].Value?.ToString()),
                ShowMarkers = (bool)(row.Cells["ShowMarkers"].Value ?? false)
            };
            _config.MetricAggregations.Add(agg);
        }

        if (_config.MetricAggregations.Count == 0) {
            ThemedMessageBox.Show(this, "Please add at least one metric", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void LoadPeriodPresets() {
        _periodComboBox.Items.Clear();
        _periodPresets = [.. ChartPeriodPresetStore.GetPresetItems()];
        foreach (var preset in _periodPresets) {
            _periodComboBox.Items.Add(preset.Label);
        }
    }

    private void SetSelectedPeriodPreset() {
        var index = ChartPeriodPresetStore.FindMatchingPresetIndex(_config.PeriodPresetUid, _periodPresets);
        if (index >= 0) {
            var selectedPreset = _periodPresets[index];
            _config.PeriodPresetUid = selectedPreset.Uid;
            _config.Period = selectedPreset.Period;
            _config.CustomPeriodDuration = selectedPreset.Period == ChartPeriod.Custom
                ? selectedPreset.Duration
                : null;
            _config.CustomAggregationInterval = selectedPreset.AggregationInterval;
            _periodComboBox.SelectedIndex = index;
            return;
        }

        var fallbackPreset = ChartPeriodPresetStore.GetFallbackPreset(_periodPresets);
        _config.PeriodPresetUid = fallbackPreset.Uid;
        _config.Period = fallbackPreset.Period;
        _config.CustomPeriodDuration = fallbackPreset.Period == ChartPeriod.Custom
            ? fallbackPreset.Duration
            : null;
        _config.CustomAggregationInterval = fallbackPreset.AggregationInterval;

        if (_periodComboBox.Items.Count > 0) {
            _periodComboBox.SelectedIndex = 0;
        }
    }

    private static Font CreateSafeFont(string familyName, float emSize, FontStyle style = FontStyle.Regular) {
        try {
            var font = new Font(familyName, emSize, style);
            _ = font.GetHeight(); // verify GDI+ handle is actually valid
            return font;
        } catch {
            try {
                return new Font("Segoe UI", emSize, style);
            } catch {
                return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
            }
        }
    }

    #endregion Private Methods

}
