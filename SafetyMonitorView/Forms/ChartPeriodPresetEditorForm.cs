using MaterialSkin;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class ChartPeriodPresetEditorForm : Form {

    #region Private Fields

    private readonly List<ChartPeriodPresetDefinition> _presets;
    private Button _addButton = null!;
    private Button _cancelButton = null!;
    private DataGridView _presetGrid = null!;
    private Button _moveDownButton = null!;
    private Button _moveUpButton = null!;
    private Button _removeButton = null!;
    private Button _saveButton = null!;
    private readonly List<ChartPeriodUnit> _units = Enum.GetValues<ChartPeriodUnit>().ToList();

    #endregion Private Fields

    #region Public Constructors

    public ChartPeriodPresetEditorForm(IEnumerable<ChartPeriodPresetDefinition> presets) {
        _presets = presets.Select(p => new ChartPeriodPresetDefinition {
            Name = p.Name,
            Value = p.Value,
            Unit = p.Unit
        }).ToList();

        InitializeComponent();
        ApplyTheme();
        LoadPresets();
    }

    #endregion Public Constructors

    #region Public Properties

    public List<ChartPeriodPresetDefinition> Presets { get; private set; } = [];

    #endregion Public Properties

    #region Private Methods

    private void AddPresetButton_Click(object? sender, EventArgs e) {
        _presetGrid.Rows.Add("Custom", 1, ChartPeriodUnit.Hours);
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        _presetGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(35, 47, 52);
        _presetGrid.DefaultCellStyle.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _presetGrid.DefaultCellStyle.ForeColor = isLight ? Color.Black : Color.White;
        _presetGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(53, 70, 76);
        _presetGrid.ColumnHeadersDefaultCellStyle.ForeColor = isLight ? Color.Black : Color.White;
        _presetGrid.EnableHeadersVisualStyles = false;

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
                    } else if (btn == _removeButton) {
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
            }

            ApplyThemeRecursive(control, isLight);
        }
    }

    private void InitializeComponent() {
        Text = "Chart Period Presets";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Padding = new Padding(15);

        var titleFont = new Font("Roboto", 9.5f, FontStyle.Bold);
        var normalFont = new Font("Roboto", 9.5f);

        var mainLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240)); // 1: Grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Buttons row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Save/Cancel

        var headerLabel = new Label {
            Text = "Edit chart period presets (name, value, unit, order).",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        mainLayout.Controls.Add(headerLabel, 0, 0);

        _presetGrid = new DataGridView {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = normalFont,
            Margin = new Padding(0, 0, 0, 5)
        };

        _presetGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Name",
            HeaderText = "Name",
            Width = 180
        });
        _presetGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Value",
            HeaderText = "Value",
            Width = 80
        });
        _presetGrid.Columns.Add(new DataGridViewComboBoxColumn {
            Name = "Unit",
            HeaderText = "Unit",
            Width = 120,
            DataSource = _units
        });
        _presetGrid.DataError += (_, e) => { e.ThrowException = false; };

        mainLayout.Controls.Add(_presetGrid, 0, 1);

        var editButtonsPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 5, 0, 10),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editButtonsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _addButton = new Button { Text = "Add", Width = 90, Height = 30, Font = normalFont, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left }; _addButton.Click += AddPresetButton_Click;
        editButtonsPanel.Controls.Add(_addButton, 0, 0);

        _removeButton = new Button { Text = "Remove", Width = 90, Height = 30, Font = normalFont, BackColor = Color.IndianRed, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left };
        _removeButton.Click += RemovePresetButton_Click;
        editButtonsPanel.Controls.Add(_removeButton, 1, 0);

        _moveUpButton = new Button { Text = "Move Up", Width = 90, Height = 30, Font = normalFont, Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Left };
        _moveUpButton.Click += (s, e) => MoveSelectedRow(-1);
        editButtonsPanel.Controls.Add(_moveUpButton, 2, 0);

        _moveDownButton = new Button { Text = "Move Down", Width = 90, Height = 30, Font = normalFont, Anchor = AnchorStyles.Left };
        _moveDownButton.Click += (s, e) => MoveSelectedRow(1);
        editButtonsPanel.Controls.Add(_moveDownButton, 3, 0);

        mainLayout.Controls.Add(editButtonsPanel, 0, 2);

        var buttonPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };

        _cancelButton = new Button { Text = "Cancel", Width = 90, Height = 35, Font = normalFont, Margin = new Padding(0), Anchor = AnchorStyles.Right };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton, 2, 0);

        _saveButton = new Button { Text = "Save", Width = 90, Height = 35, Font = new Font("Roboto", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0), Anchor = AnchorStyles.Right };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton, 1, 0);

        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);
        ClientSize = new Size(520, 420);
    }

    private void LoadPresets() {
        _presetGrid.Rows.Clear();
        foreach (var preset in _presets) {
            var unit = _units.Contains(preset.Unit) ? preset.Unit : ChartPeriodUnit.Hours;
            _presetGrid.Rows.Add(preset.Name, preset.Value, unit);
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

    private void RemovePresetButton_Click(object? sender, EventArgs e) {
        foreach (DataGridViewRow row in _presetGrid.SelectedRows) {
            if (!row.IsNewRow) {
                _presetGrid.Rows.Remove(row);
            }
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        var newPresets = new List<ChartPeriodPresetDefinition>();
        foreach (DataGridViewRow row in _presetGrid.Rows) {
            var name = row.Cells["Name"].Value?.ToString() ?? "";
            var valueText = row.Cells["Value"].Value?.ToString() ?? "";
            var unit = row.Cells["Unit"].Value is ChartPeriodUnit unitValue ? unitValue : ChartPeriodUnit.Hours;

            if (string.IsNullOrWhiteSpace(name)) {
                ThemedMessageBox.Show(this, "Each preset must have a name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(valueText, out var value) || value <= 0) {
                ThemedMessageBox.Show(this, "Each preset must have a positive numeric value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            newPresets.Add(new ChartPeriodPresetDefinition {
                Name = name.Trim(),
                Value = value,
                Unit = unit
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

    #endregion Private Methods
}
