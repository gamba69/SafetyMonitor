using DataStorage.Models;
using MaterialSkin;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class ChartTileEditorForm : Form {

    #region Private Fields

    private readonly ChartTileConfig _config;

    private ComboBox _aggregationUnitComboBox = null!;
    private NumericUpDown _aggregationValueNumeric = null!;
    private Button _cancelButton = null!;
    private NumericUpDown _columnSpanNumeric = null!;
    private DateTimePicker _endTimePicker = null!;
    private DataGridView _metricsGrid = null!;
    private ComboBox _periodComboBox = null!;
    private NumericUpDown _rowSpanNumeric = null!;
    private Button _saveButton = null!;
    private CheckBox _showGridCheckBox = null!;
    private CheckBox _showLegendCheckBox = null!;
    private TextBox _titleTextBox = null!;
    private CheckBox _useCustomEndTimeCheckBox = null!;

    #endregion Private Fields

    #region Public Constructors

    public ChartTileEditorForm(ChartTileConfig config) {
        _config = config;

        InitializeComponent();
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
        var newRow = _metricsGrid.Rows.Add(MetricType.Temperature, AggregationFunction.Average, "Metric", null!, 2.0f, false);
        _metricsGrid.Rows[newRow].Tag = Color.Blue;
        _metricsGrid.Rows[newRow].Cells["Color"].Style.BackColor = Color.Blue;
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        // DataGridView special handling
        _metricsGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        _metricsGrid.DefaultCellStyle.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _metricsGrid.DefaultCellStyle.ForeColor = isLight ? Color.Black : Color.White;
        _metricsGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(53, 70, 76);
        _metricsGrid.ColumnHeadersDefaultCellStyle.ForeColor = isLight ? Color.Black : Color.White;
        _metricsGrid.EnableHeadersVisualStyles = false;

        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            switch (control) {
                case Label lbl:
                    lbl.ForeColor = isLight ? Color.Black : Color.White;
                    break;

                case Button btn:
                    if (btn == _cancelButton) {
                        btn.BackColor = Color.Gray;
                    } else if (btn.BackColor == Color.IndianRed) {
                        btn.BackColor = Color.IndianRed;
                    } else {
                        btn.BackColor = Color.FromArgb(0, 121, 107);
                    }

                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    break;

                case TextBox txt:
                    txt.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    txt.ForeColor = isLight ? Color.Black : Color.White;
                    break;

                case ComboBox cmb:
                    cmb.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    cmb.ForeColor = isLight ? Color.Black : Color.White;
                    break;

                case NumericUpDown num:
                    num.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    num.ForeColor = isLight ? Color.Black : Color.White;
                    break;

                case CheckBox chk:
                    chk.ForeColor = isLight ? Color.Black : Color.White;
                    break;

                case DateTimePicker dtp:
                    dtp.CalendarForeColor = isLight ? Color.Black : Color.White;
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

        var titleFont = new Font("Roboto", 9.5f, FontStyle.Bold);
        var normalFont = new Font("Roboto", 9.5f);

        // Main layout
        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Metrics label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180)); // 2: Metrics grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Add/Remove buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Period row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: Aggregation row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 6: Size row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 7: Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 8: Buttons

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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 5)
        };

        _metricsGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "Metric",
            HeaderText = "Metric",
            Width = 140,
            DataSource = Enum.GetValues<MetricType>().Select(m => new { Value = m, Display = m.GetDisplayName() }).ToList(),
            DisplayMember = "Display",
            ValueMember = "Value"
        });

        _metricsGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "Function",
            HeaderText = "Aggregation",
            Width = 100,
            DataSource = Enum.GetValues<AggregationFunction>().ToList()
        });

        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label", Width = 100 });
        _metricsGrid.Columns.Add(new DataGridViewButtonColumn { Name = "Color", HeaderText = "Color", Width = 70, Text = "Choose", UseColumnTextForButtonValue = true });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineWidth", HeaderText = "Width", Width = 55 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "ShowMarkers", HeaderText = "Markers", Width = 65 });

        _metricsGrid.CellClick += MetricsGrid_CellClick;
        mainLayout.Controls.Add(_metricsGrid, 0, 2);

        // Row 3: Add/Remove buttons
        var gridButtonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 10) };
        var addMetricButton = new Button { Text = "Add Metric", Width = 110, Height = 30, Font = normalFont, Margin = new Padding(0, 0, 10, 0) };
        addMetricButton.Click += AddMetricButton_Click;
        gridButtonPanel.Controls.Add(addMetricButton);
        var removeMetricButton = new Button { Text = "Remove", Width = 90, Height = 30, Font = normalFont, BackColor = Color.IndianRed };
        removeMetricButton.Click += RemoveMetricButton_Click;
        gridButtonPanel.Controls.Add(removeMetricButton);
        mainLayout.Controls.Add(gridButtonPanel, 0, 3);

        // Row 4: Period + Custom End Time
        var periodPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 5) };
        periodPanel.Controls.Add(new Label { Text = "Period:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _periodComboBox = new ComboBox { Width = 120, Font = normalFont, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 15, 0) };
        _periodComboBox.Items.AddRange(["15 Minutes", "1 Hour", "6 Hours", "24 Hours", "7 Days", "30 Days"]);
        periodPanel.Controls.Add(_periodComboBox);
        _useCustomEndTimeCheckBox = new CheckBox { Text = "Custom End Time:", Font = normalFont, AutoSize = true, Margin = new Padding(10, 4, 5, 0) };
        _useCustomEndTimeCheckBox.CheckedChanged += (s, e) => _endTimePicker.Enabled = _useCustomEndTimeCheckBox.Checked;
        periodPanel.Controls.Add(_useCustomEndTimeCheckBox);
        _endTimePicker = new DateTimePicker { Width = 160, Font = normalFont, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Enabled = false };
        periodPanel.Controls.Add(_endTimePicker);
        mainLayout.Controls.Add(periodPanel, 0, 4);

        // Row 5: Aggregation + Options
        var aggPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 5) };
        aggPanel.Controls.Add(new Label { Text = "Aggregation:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _aggregationValueNumeric = new NumericUpDown { Width = 70, Minimum = 1, Maximum = 1440, Value = 5, Font = normalFont, Margin = new Padding(0, 0, 5, 0) };
        aggPanel.Controls.Add(_aggregationValueNumeric);
        _aggregationUnitComboBox = new ComboBox { Width = 90, Font = normalFont, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 15, 0) };
        _aggregationUnitComboBox.Items.AddRange(["Seconds", "Minutes", "Hours"]);
        _aggregationUnitComboBox.SelectedIndex = 1;
        aggPanel.Controls.Add(_aggregationUnitComboBox);
        _showLegendCheckBox = new CheckBox { Text = "Legend", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(10, 4, 10, 0) };
        aggPanel.Controls.Add(_showLegendCheckBox);
        _showGridCheckBox = new CheckBox { Text = "Grid", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 4, 0, 0) };
        aggPanel.Controls.Add(_showGridCheckBox);
        mainLayout.Controls.Add(aggPanel, 0, 5);

        // Row 6: Size
        var sizePanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 10) };
        sizePanel.Controls.Add(new Label { Text = "Size:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 10, 0) });
        sizePanel.Controls.Add(new Label { Text = "Rows:", Font = normalFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _rowSpanNumeric = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 2, Font = normalFont, Margin = new Padding(0, 0, 15, 0) };
        sizePanel.Controls.Add(_rowSpanNumeric);
        sizePanel.Controls.Add(new Label { Text = "Columns:", Font = normalFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _columnSpanNumeric = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 2, Font = normalFont };
        sizePanel.Controls.Add(_columnSpanNumeric);
        mainLayout.Controls.Add(sizePanel, 0, 6);

        // Row 7: Spacer (empty)
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 7);

        // Row 8: Buttons
        var buttonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 10, 0, 0) };
        _cancelButton = new Button { Text = "Cancel", Width = 90, Height = 35, Font = normalFont, Margin = new Padding(0) };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);
        _saveButton = new Button { Text = "Save", Width = 90, Height = 35, Font = new Font("Roboto", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);
        mainLayout.Controls.Add(buttonPanel, 0, 8);

        Controls.Add(mainLayout);

        // Set form size
        ClientSize = new Size(580, 550);
    }
    private void LoadConfig() {
        _titleTextBox.Text = _config.Title;
        _periodComboBox.SelectedIndex = (int)_config.Period;

        if (_config.CustomEndTime.HasValue) {
            _useCustomEndTimeCheckBox.Checked = true;
            _endTimePicker.Value = _config.CustomEndTime.Value;
        }

        if (_config.CustomAggregationInterval.HasValue) {
            var interval = _config.CustomAggregationInterval.Value;
            if (interval.TotalHours >= 1) {
                _aggregationValueNumeric.Value = (decimal)interval.TotalHours;
                _aggregationUnitComboBox.SelectedIndex = 2;
            } else if (interval.TotalMinutes >= 1) {
                _aggregationValueNumeric.Value = (decimal)interval.TotalMinutes;
                _aggregationUnitComboBox.SelectedIndex = 1;
            } else {
                _aggregationValueNumeric.Value = (decimal)interval.TotalSeconds;
                _aggregationUnitComboBox.SelectedIndex = 0;
            }
        }

        _showLegendCheckBox.Checked = _config.ShowLegend;
        _showGridCheckBox.Checked = _config.ShowGrid;
        _rowSpanNumeric.Value = _config.RowSpan;
        _columnSpanNumeric.Value = _config.ColumnSpan;

        foreach (var agg in _config.MetricAggregations) {
            var rowIndex = _metricsGrid.Rows.Add(agg.Metric, agg.Function, agg.Label, null!, agg.LineWidth, agg.ShowMarkers);
            _metricsGrid.Rows[rowIndex].Tag = agg.Color;
            _metricsGrid.Rows[rowIndex].Cells["Color"].Style.BackColor = agg.Color;
        }
    }
    private void MetricsGrid_CellClick(object? sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex >= 0 && e.ColumnIndex == 3) // Color column
        {
            var currentColor = _metricsGrid.Rows[e.RowIndex].Tag as Color? ?? Color.Blue;
            if (ThemedColorPicker.ShowPicker(currentColor, out var pickedColor) == DialogResult.OK) {
                _metricsGrid.Rows[e.RowIndex].Tag = pickedColor;
                _metricsGrid.Rows[e.RowIndex].Cells["Color"].Style.BackColor = pickedColor;
            }
        }
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
        _config.Title = _titleTextBox.Text;
        _config.Period = (ChartPeriod)_periodComboBox.SelectedIndex;

        _config.CustomEndTime = _useCustomEndTimeCheckBox.Checked ? _endTimePicker.Value : null;

        var value = (int)_aggregationValueNumeric.Value;
        _config.CustomAggregationInterval = _aggregationUnitComboBox.SelectedIndex switch {
            0 => TimeSpan.FromSeconds(value),
            1 => TimeSpan.FromMinutes(value),
            2 => TimeSpan.FromHours(value),
            _ => TimeSpan.FromMinutes(value)
        };

        _config.ShowLegend = _showLegendCheckBox.Checked;
        _config.ShowGrid = _showGridCheckBox.Checked;
        _config.RowSpan = (int)_rowSpanNumeric.Value;
        _config.ColumnSpan = (int)_columnSpanNumeric.Value;

        _config.MetricAggregations.Clear();
        foreach (DataGridViewRow row in _metricsGrid.Rows) {
            if (row.Cells["Metric"].Value == null) {
                continue;
            }

            var agg = new MetricAggregation {
                Metric = (MetricType)row.Cells["Metric"].Value!,
                Function = (AggregationFunction)row.Cells["Function"].Value!,
                Label = row.Cells["Label"].Value?.ToString() ?? "",
                Color = (Color)(row.Tag ?? Color.Blue),
                LineWidth = float.Parse(row.Cells["LineWidth"].Value?.ToString() ?? "2"),
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

    #endregion Private Methods

}
