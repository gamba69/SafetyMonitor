using MaterialSkin;
using SafetyMonitorView.Services;
using SafetyMonitorView.Controls;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class ChartPeriodsEditorForm : Form {

    #region Private Fields

    private readonly List<ChartPeriodPresetDefinition> _presets;
    private Button _addButton = null!;
    private Button _cancelButton = null!;
    private DataGridView _presetGrid = null!;
    private Button _moveDownButton = null!;
    private Button _moveUpButton = null!;
    private Button _removeButton = null!;
    private Button _calculateButton = null!;
    private Button _saveButton = null!;
    private readonly List<ChartPeriodUnit> _units = [.. Enum.GetValues<ChartPeriodUnit>()];
    private readonly List<string> _aggregationUnits = ["Seconds", "Minutes", "Hours"];
    private readonly int _autoAggregationTargetPointCount;

    #endregion Private Fields

    #region Public Constructors

    public ChartPeriodsEditorForm(IEnumerable<ChartPeriodPresetDefinition> presets, int autoAggregationTargetPointCount) {
        _presets = [.. presets.Select(p => new ChartPeriodPresetDefinition {
            Uid = p.Uid,
            Name = p.Name,
            Value = p.Value,
            Unit = p.Unit,
            AggregationInterval = p.AggregationInterval
        })];

        _autoAggregationTargetPointCount = Math.Max(2, autoAggregationTargetPointCount);

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewChartPeriods);
        ApplyTheme();
        LoadPresets();
    }

    #endregion Public Constructors

    #region Public Properties

    public List<ChartPeriodPresetDefinition> Presets { get; private set; } = [];

    #endregion Public Properties

    #region Private Methods

    private void AddPresetButton_Click(object? sender, EventArgs e) {
        _presetGrid.Rows.Add(Guid.NewGuid().ToString("N"), "Custom", 1, ChartPeriodUnit.Hours, 1, "Minutes");
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        _presetGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _presetGrid.DefaultCellStyle.BackColor = _presetGrid.BackgroundColor;
        _presetGrid.DefaultCellStyle.ForeColor = ForeColor;
        _presetGrid.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(225, 245, 254) : Color.FromArgb(56, 78, 84);
        _presetGrid.DefaultCellStyle.SelectionForeColor = ForeColor;
        _presetGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(238, 238, 238) : Color.FromArgb(55, 71, 79);
        _presetGrid.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        _presetGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _presetGrid.ColumnHeadersDefaultCellStyle.BackColor;
        _presetGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _presetGrid.ColumnHeadersDefaultCellStyle.ForeColor;
        foreach (var comboColumnName in new[] { "Unit", "AggregationUnit" }) {
            if (_presetGrid.Columns[comboColumnName] is not DataGridViewComboBoxColumn comboColumn) {
                continue;
            }

            comboColumn.DefaultCellStyle.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
            comboColumn.DefaultCellStyle.ForeColor = isLight ? Color.Black : Color.White;
            comboColumn.DefaultCellStyle.SelectionBackColor = _presetGrid.DefaultCellStyle.SelectionBackColor;
            comboColumn.DefaultCellStyle.SelectionForeColor = _presetGrid.DefaultCellStyle.SelectionForeColor;
            comboColumn.FlatStyle = FlatStyle.Popup;
        }
        _presetGrid.EnableHeadersVisualStyles = false;
        _presetGrid.GridColor = isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(60, 75, 80);

        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);
            switch (control) {
                case Label lbl:
                    lbl.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case Button btn:
                    ThemedButtonStyler.Apply(btn, isLight);
                    if (ReferenceEquals(btn, _calculateButton)) {
                        btn.Image = MaterialIcons.GetIcon(MaterialIcons.CommonCalculate, btn.ForeColor, 18);
                        btn.TextImageRelation = TextImageRelation.ImageBeforeText;
                        btn.ImageAlign = ContentAlignment.MiddleLeft;
                    }
                    break;
                case TextBox txt:
                    txt.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    txt.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case ThemedComboBox tcmb:
                    tcmb.ApplyTheme();
                    break;
                case ComboBox cmb:
                    ThemedComboBoxStyler.Apply(cmb, isLight);
                    break;
            }

            ApplyThemeRecursive(control, isLight);
        }
    }

    private void InitializeComponent() {
        Text = "Chart Periods";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Padding = new Padding(15);

        var titleFont = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold);
        var normalFont = CreateSafeFont("Segoe UI", 9.5f);

        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = false
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: 2x2 hints
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 2: Grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Buttons row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Save/Cancel

        var headerLabel = new Label {
            Text = "Configure chart period presets used by chart tiles for quick period switching and static aggregation.",
            Font = titleFont,
            AutoSize = true,
            MaximumSize = new Size(980, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        mainLayout.Controls.Add(headerLabel, 0, 0);

        var valueHintsTable = new TableLayoutPanel {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 10)
        };
        valueHintsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        valueHintsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        valueHintsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        valueHintsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        valueHintsTable.Controls.Add(new Label {
            Text = "• Period value: numeric period length for the preset.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 2)
        }, 0, 0);
        valueHintsTable.Controls.Add(new Label {
            Text = "• Aggregation value: aggregation bucket size for the preset.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        }, 1, 0);
        valueHintsTable.Controls.Add(new Label {
            Text = "• Period unit: time unit for period length.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        }, 0, 1);
        valueHintsTable.Controls.Add(new Label {
            Text = "• Aggregation unit: time unit for aggregation bucket.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0)
        }, 1, 1);

        mainLayout.Controls.Add(valueHintsTable, 0, 1);

        _presetGrid = new DataGridView {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 5)
        };

        _presetGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Uid",
            Visible = false
        });
        _presetGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Name",
            HeaderText = "Preset Name",
            FillWeight = 30,
            MinimumWidth = 220
        });
        _presetGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Value",
            HeaderText = "Period Value",
            FillWeight = 15,
            MinimumWidth = 130
        });
        _presetGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "Unit",
            HeaderText = "Period Unit",
            FillWeight = 16,
            MinimumWidth = 150,
            DataSource = _units
        });
        _presetGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "AggregationValue",
            HeaderText = "Aggregation Value",
            FillWeight = 19,
            MinimumWidth = 180
        });
        _presetGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "AggregationUnit",
            HeaderText = "Aggregation Unit",
            FillWeight = 20,
            MinimumWidth = 180,
            DataSource = _aggregationUnits
        });

        foreach (DataGridViewColumn column in _presetGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
        }
        _presetGrid.DataError += (_, e) => { e.ThrowException = false; };
        _presetGrid.EditingControlShowing += PresetGrid_EditingControlShowing;

        mainLayout.Controls.Add(_presetGrid, 0, 2);

        var editButtonsPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0, 5, 0, 10),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _addButton = new Button { Text = "Add", Width = 120, Height = 32, Font = normalFont, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left }; _addButton.Click += AddPresetButton_Click;
        editButtonsPanel.Controls.Add(_addButton, 0, 0);

        _removeButton = new Button { Text = "Delete", Width = 120, Height = 32, Font = normalFont, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left };
        _removeButton.Click += RemovePresetButton_Click;
        editButtonsPanel.Controls.Add(_removeButton, 1, 0);

        _moveUpButton = new Button { Text = "Up", Width = 120, Height = 32, Font = normalFont, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left };
        _moveUpButton.Click += (s, e) => MoveSelectedRow(-1);
        editButtonsPanel.Controls.Add(_moveUpButton, 2, 0);

        _moveDownButton = new Button { Text = "Down", Width = 120, Height = 32, Font = normalFont, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left };
        _moveDownButton.Click += (s, e) => MoveSelectedRow(1);
        editButtonsPanel.Controls.Add(_moveDownButton, 3, 0);

        _calculateButton = new Button { Text = "Calculate", Width = 120, Height = 32, Font = normalFont, Anchor = AnchorStyles.Left };
        _calculateButton.Click += CalculateButton_Click;
        editButtonsPanel.Controls.Add(_calculateButton, 4, 0);

        mainLayout.Controls.Add(editButtonsPanel, 0, 3);

        var buttonPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };

        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Right };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton, 1, 0);

        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont, Margin = new Padding(0), Anchor = AnchorStyles.Right };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton, 2, 0);

        mainLayout.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(mainLayout);
        ClientSize = new Size(980, 560);
    }

    private void LoadPresets() {
        _presetGrid.Rows.Clear();
        foreach (var preset in _presets) {
            var unit = _units.Contains(preset.Unit) ? preset.Unit : ChartPeriodUnit.Hours;
            _presetGrid.Rows.Add(preset.Uid, preset.Name, preset.Value, unit, FormatAggregationValue(preset.AggregationInterval), GetAggregationUnitName(preset.AggregationInterval));
        }
    }

    private void MoveSelectedRow(int direction) {
        if (_presetGrid.SelectedRows.Count == 0) {
            return;
        }

        var row = _presetGrid.SelectedRows[0];
        var index = row.Index;
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= _presetGrid.Rows.Count) {
            return;
        }

        _presetGrid.Rows.RemoveAt(index);
        _presetGrid.Rows.Insert(newIndex, row);
        _presetGrid.ClearSelection();
        row.Selected = true;
    }

    private void PresetGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        var columnName = _presetGrid.CurrentCell?.OwningColumn?.Name;
        if (columnName is not ("Unit" or "AggregationUnit") || e.Control is not ComboBox comboBox) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ThemedComboBoxStyler.Apply(comboBox, isLight);
    }


    private void CalculateButton_Click(object? sender, EventArgs e) {
        var result = ThemedMessageBox.Show(
            this,
            "Aggregation periods will be automatically recalculated for all presets according to auto-calculation settings (target points: "
            + _autoAggregationTargetPointCount
            + "). Continue?",
            "Recalculate Aggregation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) {
            return;
        }

        RecalculateAggregationForAllPresets();
    }

    private void RecalculateAggregationForAllPresets() {
        foreach (DataGridViewRow row in _presetGrid.Rows) {
            if (row.IsNewRow) {
                continue;
            }

            if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var periodValue) || periodValue <= 0) {
                continue;
            }

            var unit = ParseChartPeriodUnit(row.Cells["Unit"].Value);
            var duration = ChartAggregationHelper.BuildPeriodDuration(periodValue, unit);
            if (duration <= TimeSpan.Zero) {
                continue;
            }

            var aggregationInterval = CalculateAutomaticAggregationInterval(duration, row.Index);
            row.Cells["AggregationValue"].Value = FormatAggregationValue(aggregationInterval);
            row.Cells["AggregationUnit"].Value = GetAggregationUnitName(aggregationInterval);
        }
    }

    private TimeSpan CalculateAutomaticAggregationInterval(TimeSpan range, int currentRowIndex) {
        var candidates = BuildAggregationCandidates(currentRowIndex);
        return ChartAggregationHelper.CalculateAutomaticAggregationInterval(
            range,
            0,
            _autoAggregationTargetPointCount,
            candidates,
            applyPeriodMatching: false);
    }

    private IEnumerable<(TimeSpan Duration, TimeSpan AggregationInterval)> BuildAggregationCandidates(int skipRowIndex) {
        foreach (DataGridViewRow row in _presetGrid.Rows) {
            if (row.IsNewRow || row.Index == skipRowIndex) {
                continue;
            }

            if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var periodValue) || periodValue <= 0) {
                continue;
            }

            var unit = ParseChartPeriodUnit(row.Cells["Unit"].Value);
            var duration = ChartAggregationHelper.BuildPeriodDuration(periodValue, unit);
            if (duration <= TimeSpan.Zero) {
                continue;
            }

            if (!double.TryParse(row.Cells["AggregationValue"].Value?.ToString(), out var aggregationValue) || aggregationValue <= 0) {
                continue;
            }

            var aggregationUnit = row.Cells["AggregationUnit"].Value?.ToString() ?? "Minutes";
            var aggregationInterval = BuildAggregationInterval(aggregationValue, aggregationUnit);
            if (aggregationInterval <= TimeSpan.Zero) {
                continue;
            }

            yield return (duration, aggregationInterval);
        }
    }

    private void RemovePresetButton_Click(object? sender, EventArgs e) {
        foreach (DataGridViewRow row in _presetGrid.SelectedRows) {
            if (!row.IsNewRow) {
                _presetGrid.Rows.Remove(row);
            }
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        if (_presetGrid.IsCurrentCellInEditMode) {
            _presetGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            _presetGrid.EndEdit();
        }
        Validate();

        var newPresets = new List<ChartPeriodPresetDefinition>();
        foreach (DataGridViewRow row in _presetGrid.Rows) {
            var uid = row.Cells["Uid"].Value?.ToString() ?? "";
            var name = row.Cells["Name"].Value?.ToString() ?? "";
            var valueText = row.Cells["Value"].Value?.ToString() ?? "";
            var unit = ParseChartPeriodUnit(row.Cells["Unit"].Value);
            var aggregationValueText = row.Cells["AggregationValue"].Value?.ToString() ?? "";
            var aggregationUnit = row.Cells["AggregationUnit"].Value?.ToString() ?? "Minutes";

            if (string.IsNullOrWhiteSpace(name)) {
                ThemedMessageBox.Show(this, "Each preset must have a name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(valueText, out var value) || value <= 0) {
                ThemedMessageBox.Show(this, "Each preset must have a positive numeric value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(aggregationValueText, out var aggregationValue) || aggregationValue <= 0) {
                ThemedMessageBox.Show(this, "Aggregation value must be a positive number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var aggregationInterval = BuildAggregationInterval(aggregationValue, aggregationUnit);
            if (aggregationInterval <= TimeSpan.Zero) {
                ThemedMessageBox.Show(this, "Aggregation interval must be greater than 00:00:00.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            newPresets.Add(new ChartPeriodPresetDefinition {
                Uid = string.IsNullOrWhiteSpace(uid) ? Guid.NewGuid().ToString("N") : uid,
                Name = name.Trim(),
                Value = value,
                Unit = unit,
                AggregationInterval = aggregationInterval
            });
        }

        if (newPresets.Count == 0) {
            ThemedMessageBox.Show(this, "Add at least one preset.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Presets = newPresets;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static ChartPeriodUnit ParseChartPeriodUnit(object? value) {
        if (value is ChartPeriodUnit unitValue) {
            return unitValue;
        }

        if (value is string text && Enum.TryParse<ChartPeriodUnit>(text, true, out var parsedFromText)) {
            return parsedFromText;
        }

        if (value is int intValue && Enum.IsDefined(typeof(ChartPeriodUnit), intValue)) {
            return (ChartPeriodUnit)intValue;
        }

        return ChartPeriodUnit.Hours;
    }


    private static TimeSpan BuildAggregationInterval(double value, string unit) {
        return unit switch {
            "Seconds" => TimeSpan.FromSeconds(value),
            "Minutes" => TimeSpan.FromMinutes(value),
            "Hours" => TimeSpan.FromHours(value),
            _ => TimeSpan.FromMinutes(value)
        };
    }

    private static string FormatAggregationValue(TimeSpan interval) {
        if (interval.TotalHours >= 1 && Math.Abs(interval.TotalHours - Math.Round(interval.TotalHours)) < 0.0001) {
            return ((int)Math.Round(interval.TotalHours)).ToString();
        }

        if (interval.TotalMinutes >= 1 && Math.Abs(interval.TotalMinutes - Math.Round(interval.TotalMinutes)) < 0.0001) {
            return ((int)Math.Round(interval.TotalMinutes)).ToString();
        }

        return Math.Max(1, (int)Math.Round(interval.TotalSeconds)).ToString();
    }

    private static string GetAggregationUnitName(TimeSpan interval) {
        if (interval.TotalHours >= 1 && Math.Abs(interval.TotalHours - Math.Round(interval.TotalHours)) < 0.0001) {
            return "Hours";
        }

        if (interval.TotalMinutes >= 1 && Math.Abs(interval.TotalMinutes - Math.Round(interval.TotalMinutes)) < 0.0001) {
            return "Minutes";
        }

        return "Seconds";
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
