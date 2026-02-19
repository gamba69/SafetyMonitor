using MaterialSkin;
using SafetyMonitorView.Services;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class AxisRulesEditorForm : Form {

    #region Private Fields

    private Button _addButton = null!;
    private Button _cancelButton = null!;
    private DataGridView _rulesGrid = null!;
    private Button _removeButton = null!;
    private Button _saveButton = null!;
    private Label _headerLabel = null!;
    private readonly List<MetricAxisRuleSetting> _rules;

    #endregion Private Fields

    #region Public Constructors

    public AxisRulesEditorForm(IEnumerable<MetricAxisRuleSetting> rules) {
        _rules = [.. rules.Select(r => new MetricAxisRuleSetting {
            Metric = r.Metric,
            Enabled = r.Enabled,
            MinBoundary = r.MinBoundary,
            MaxBoundary = r.MaxBoundary,
            MinSpan = r.MinSpan,
            MaxSpan = r.MaxSpan
        })];

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewAxisRules);
        ApplyTheme();
        LoadRules();
    }

    #endregion Public Constructors

    #region Public Properties

    public List<MetricAxisRuleSetting> Rules { get; private set; } = [];

    #endregion Public Properties

    #region Private Methods

    private void InitializeComponent() {
        Text = "Axis Rules";
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
            RowCount = 4,
            AutoSize = false
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // 0: Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 1: Grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // 2: Add/Remove
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // 3: Save/Cancel

        // Row 0: Header
        var headerPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
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

        _headerLabel = new Label {
            Text = "Configure Y-axis limits applied during chart zoom/pan. Leave numeric cells empty to disable a specific limit.",
            Font = titleFont,
            AutoSize = true,
            MaximumSize = new Size(810, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        headerPanel.Controls.Add(_headerLabel, 0, 0);
        headerPanel.SetColumnSpan(_headerLabel, 2);

        var minBoundaryLabel = new Label {
            Text = "• Min Boundary: chart cannot go below this Y value.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 2)
        };
        var minSpanLabel = new Label {
            Text = "• Min Span: prevents too much zoom-in (minimum Y range).",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        var maxBoundaryLabel = new Label {
            Text = "• Max Boundary: chart cannot go above this Y value.",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        };
        var maxSpanLabel = new Label {
            Text = "• Max Span: prevents too much zoom-out (maximum Y range).",
            Font = normalFont,
            AutoSize = true,
            Margin = new Padding(0)
        };

        headerPanel.Controls.Add(minBoundaryLabel, 0, 1);
        headerPanel.Controls.Add(minSpanLabel, 1, 1);
        headerPanel.Controls.Add(maxBoundaryLabel, 0, 2);
        headerPanel.Controls.Add(maxSpanLabel, 1, 2);

        mainLayout.Controls.Add(headerPanel, 0, 0);

        // Row 1: DataGridView
        _rulesGrid = new DataGridView {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            Font = normalFont,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };

        // Metric column (ComboBox)
        var metricColumn = new DataGridViewComboBoxColumn {
            Name = "Metric",
            HeaderText = "Metric",
            FillWeight = 30,
            MinimumWidth = 180
        };
        foreach (MetricType mt in Enum.GetValues<MetricType>()) {
            metricColumn.Items.Add(mt.GetDisplayName());
        }
        _rulesGrid.Columns.Add(metricColumn);

        // Enabled column (CheckBox)
        _rulesGrid.Columns.Add(new DataGridViewCheckBoxColumn {
            Name = "Enabled",
            HeaderText = "Enabled",
            FillWeight = 14,
            MinimumWidth = 90
        });

        // MinBoundary
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "MinBoundary",
            HeaderText = "Min Boundary",
            FillWeight = 17,
            MinimumWidth = 120
        });

        // MaxBoundary
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "MaxBoundary",
            HeaderText = "Max Boundary",
            FillWeight = 17,
            MinimumWidth = 120
        });

        // MinSpan
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "MinSpan",
            HeaderText = "Min Span",
            FillWeight = 14,
            MinimumWidth = 100
        });

        // MaxSpan
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "MaxSpan",
            HeaderText = "Max Span",
            FillWeight = 14,
            MinimumWidth = 100
        });

        foreach (DataGridViewColumn column in _rulesGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
        }

        _rulesGrid.CellValidating += RulesGrid_CellValidating;
        _rulesGrid.EditingControlShowing += RulesGrid_EditingControlShowing;
        mainLayout.Controls.Add(_rulesGrid, 0, 1);

        // Row 2: Add / Remove buttons
        var actionPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 6, 0, 6)
        };

        _addButton = new Button {
            Text = "Add",
            Width = 100,
            Height = 30,
            Font = normalFont,
            Margin = new Padding(0, 0, 8, 0)
        };
        _addButton.Click += AddButton_Click;
        actionPanel.Controls.Add(_addButton);

        _removeButton = new Button {
            Text = "Delete",
            Width = 100,
            Height = 30,
            Font = normalFont,
            Margin = new Padding(0)
        };
        _removeButton.Click += RemoveButton_Click;
        actionPanel.Controls.Add(_removeButton);

        mainLayout.Controls.Add(actionPanel, 0, 2);

        // Row 3: Save / Cancel
        var buttonPanel = new FlowLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 4, 0, 0)
        };

        _cancelButton = new Button {
            Text = "Cancel",
            Width = 110,
            Height = 35,
            Font = normalFont,
            Margin = new Padding(0)
        };
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);

        _saveButton = new Button {
            Text = "Save",
            Width = 110,
            Height = 35,
            Font = CreateSafeFont("Segoe UI", 10f, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0)
        };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);

        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);
        ClientSize = new Size(860, 500);
    }

    private void LoadRules() {
        _rulesGrid.Rows.Clear();
        foreach (var rule in _rules) {
            _rulesGrid.Rows.Add(
                rule.Metric.GetDisplayName(),
                rule.Enabled,
                rule.MinBoundary.HasValue ? rule.MinBoundary.Value.ToString() : "",
                rule.MaxBoundary.HasValue ? rule.MaxBoundary.Value.ToString() : "",
                rule.MinSpan.HasValue ? rule.MinSpan.Value.ToString() : "",
                rule.MaxSpan.HasValue ? rule.MaxSpan.Value.ToString() : ""
            );
        }
    }

    private void AddButton_Click(object? sender, EventArgs e) {
        _rulesGrid.Rows.Add(MetricType.Temperature.GetDisplayName(), true, "", "", "", "");
    }

    private void RemoveButton_Click(object? sender, EventArgs e) {
        foreach (DataGridViewRow row in _rulesGrid.SelectedRows) {
            if (!row.IsNewRow) {
                _rulesGrid.Rows.Remove(row);
            }
        }
    }

    private void RulesGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e) {
        var columnName = _rulesGrid.Columns[e.ColumnIndex].Name;
        if (columnName is not ("MinBoundary" or "MaxBoundary" or "MinSpan" or "MaxSpan")) {
            return;
        }

        var text = e.FormattedValue?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(text)) {
            return; // empty is allowed (means no constraint)
        }

        if (!double.TryParse(text, out _)) {
            e.Cancel = true;
            _rulesGrid.Rows[e.RowIndex].ErrorText = "Must be a number or empty.";
        } else {
            _rulesGrid.Rows[e.RowIndex].ErrorText = "";
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        var newRules = new List<MetricAxisRuleSetting>();
        var metricNames = Enum.GetValues<MetricType>().ToDictionary(m => m.GetDisplayName(), m => m);

        foreach (DataGridViewRow row in _rulesGrid.Rows) {
            if (row.IsNewRow) {
                continue;
            }

            var metricName = row.Cells["Metric"].Value?.ToString() ?? "";
            if (!metricNames.TryGetValue(metricName, out var metric)) {
                ThemedMessageBox.Show(this, $"Unknown metric: {metricName}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var enabled = row.Cells["Enabled"].Value is true;

            var rule = new MetricAxisRuleSetting {
                Metric = metric,
                Enabled = enabled,
                MinBoundary = ParseNullableDouble(row.Cells["MinBoundary"].Value),
                MaxBoundary = ParseNullableDouble(row.Cells["MaxBoundary"].Value),
                MinSpan = ParseNullableDouble(row.Cells["MinSpan"].Value),
                MaxSpan = ParseNullableDouble(row.Cells["MaxSpan"].Value)
            };

            newRules.Add(rule);
        }

        Rules = newRules;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static double? ParseNullableDouble(object? value) {
        var text = value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        return double.TryParse(text, out var result) ? result : null;
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        _rulesGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _rulesGrid.DefaultCellStyle.BackColor = _rulesGrid.BackgroundColor;
        _rulesGrid.DefaultCellStyle.ForeColor = ForeColor;
        _rulesGrid.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(225, 245, 254) : Color.FromArgb(56, 78, 84);
        _rulesGrid.DefaultCellStyle.SelectionForeColor = ForeColor;
        _rulesGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(238, 238, 238) : Color.FromArgb(55, 71, 79);
        _rulesGrid.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        _rulesGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _rulesGrid.ColumnHeadersDefaultCellStyle.BackColor;
        _rulesGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _rulesGrid.ColumnHeadersDefaultCellStyle.ForeColor;

        _rulesGrid.EnableHeadersVisualStyles = false;

        _rulesGrid.GridColor = isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(60, 75, 80);

        if (_rulesGrid.Columns["Metric"] is DataGridViewComboBoxColumn metricCol) {
            metricCol.DefaultCellStyle.BackColor = _rulesGrid.DefaultCellStyle.BackColor;
            metricCol.DefaultCellStyle.ForeColor = _rulesGrid.DefaultCellStyle.ForeColor;
            metricCol.DefaultCellStyle.SelectionBackColor = _rulesGrid.DefaultCellStyle.SelectionBackColor;
            metricCol.DefaultCellStyle.SelectionForeColor = _rulesGrid.DefaultCellStyle.SelectionForeColor;
            metricCol.FlatStyle = FlatStyle.Popup;
        }

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
            }
            ApplyThemeRecursive(control, isLight);
        }
    }

    private void RulesGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (_rulesGrid.CurrentCell?.OwningColumn?.Name != "Metric" || e.Control is not ComboBox comboBox) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        ThemedComboBoxStyler.Apply(comboBox, isLight);
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
