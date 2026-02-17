using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

public class ValueTileEditorForm : Form {
    #region Private Fields

    private readonly ColorSchemeService _colorSchemeService;
    private readonly ValueTileConfig _config;
    private readonly Dashboard _dashboard;
    private Button _cancelButton = null!;
    private ComboBox _colorSchemeComboBox = null!;
    private NumericUpDown _columnSpanNumeric = null!;
    private NumericUpDown _decimalPlacesNumeric = null!;
    private ComboBox _metricComboBox = null!;
    private NumericUpDown _rowSpanNumeric = null!;
    private Button _saveButton = null!;
    private CheckBox _showIconCheckBox = null!;
    private TextBox _titleTextBox = null!;

    #endregion Private Fields

    #region Public Constructors

    public ValueTileEditorForm(ValueTileConfig config, Dashboard dashboard) {
        _config = config;
        _dashboard = dashboard;
        _colorSchemeService = new ColorSchemeService();

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

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

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
                    txt.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    txt.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case ComboBox cmb:
                    ThemedComboBoxStyler.Apply(cmb, isLight);
                    break;
                case NumericUpDown num:
                    num.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
                    num.ForeColor = isLight ? Color.Black : Color.White;
                    break;
                case CheckBox chk:
                    chk.ForeColor = isLight ? Color.Black : Color.White;
                    break;
            }
            ApplyThemeRecursive(control, isLight);
        }
    }

    private void EditSchemesButton_Click(object? sender, EventArgs e) {
        using var editor = new ColorSchemeEditorForm();
        editor.ShowDialog(this);
        // Refresh combo after editor closes
        RefreshColorSchemeCombo();
    }

    private void InitializeComponent() {
        Text = "Value Tile Editor";
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
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Metric
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Color Scheme
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Decimal + Icon
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Size label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Size controls
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

        // Row 0: Title
        var titlePanel = CreateLabeledControl("Title:", _titleTextBox = new TextBox { Font = normalFont, Dock = DockStyle.Fill }, titleFont);
        mainLayout.Controls.Add(titlePanel, 0, 0);

        // Row 1: Metric
        _metricComboBox = new ComboBox { Font = normalFont, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (MetricType metric in Enum.GetValues<MetricType>()) {
            _metricComboBox.Items.Add(metric.GetDisplayName());
        }

        var metricPanel = CreateLabeledControl("Metric:", _metricComboBox, titleFont);
        mainLayout.Controls.Add(metricPanel, 0, 1);

        // Row 2: Color Scheme with Edit button
        _colorSchemeComboBox = new ComboBox { Font = normalFont, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        RefreshColorSchemeCombo();
        var editSchemesButton = new Button { Text = "Edit Schemes...", Width = 140, Height = 30, Font = normalFont, Margin = new Padding(10, 0, 0, 0) };
        editSchemesButton.Click += EditSchemesButton_Click;
        var colorInnerPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = false, Margin = new Padding(0) };
        colorInnerPanel.Controls.Add(_colorSchemeComboBox);
        colorInnerPanel.Controls.Add(editSchemesButton);
        var colorPanel = CreateLabeledControl("Color Scheme:", colorInnerPanel, titleFont);
        mainLayout.Controls.Add(colorPanel, 0, 2);

        // Row 3: Decimal places + Show icon
        var decimalPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 10, 0, 5) };
        decimalPanel.Controls.Add(new Label { Text = "Decimal Places:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _decimalPlacesNumeric = new NumericUpDown { Width = 70, Minimum = 0, Maximum = 5, Value = 1, Font = normalFont, Margin = new Padding(0, 0, 20, 0) };
        decimalPanel.Controls.Add(_decimalPlacesNumeric);
        _showIconCheckBox = new CheckBox { Text = "Show Icon", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(10, 3, 0, 0) };
        decimalPanel.Controls.Add(_showIconCheckBox);
        mainLayout.Controls.Add(decimalPanel, 0, 3);

        // Row 4: Size label
        var sizeLabel = new Label { Text = "Size (rows × columns):", Font = titleFont, AutoSize = true, Margin = new Padding(0, 10, 0, 5) };
        mainLayout.Controls.Add(sizeLabel, 0, 4);

        // Row 5: Size controls
        var sizePanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 0, 0, 10) };
        sizePanel.Controls.Add(new Label { Text = "Rows:", Font = normalFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _rowSpanNumeric = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 1, Font = normalFont, Margin = new Padding(0, 0, 15, 0) };
        sizePanel.Controls.Add(_rowSpanNumeric);
        sizePanel.Controls.Add(new Label { Text = "Columns:", Font = normalFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _columnSpanNumeric = new NumericUpDown { Width = 60, Minimum = 1, Maximum = 5, Value = 1, Font = normalFont };
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

        // Set form size after layout
        ClientSize = new Size(450, 390);
    }

    private void LoadConfig() {
        _titleTextBox.Text = _config.Title;
        _metricComboBox.SelectedIndex = (int)_config.Metric;

        if (string.IsNullOrEmpty(_config.ColorSchemeName)) {
            _colorSchemeComboBox.SelectedIndex = 0; // "(None)"
        } else {
            var schemeIndex = _colorSchemeComboBox.Items.IndexOf(_config.ColorSchemeName);
            _colorSchemeComboBox.SelectedIndex = schemeIndex >= 0 ? schemeIndex : 0;
        }

        _decimalPlacesNumeric.Value = _config.DecimalPlaces;
        _showIconCheckBox.Checked = _config.ShowIcon;
        _rowSpanNumeric.Value = _config.RowSpan;
        _columnSpanNumeric.Value = _config.ColumnSpan;
    }

    private void RefreshColorSchemeCombo() {
        var currentSelection = _colorSchemeComboBox.SelectedItem?.ToString();
        _colorSchemeComboBox.Items.Clear();
        _colorSchemeComboBox.Items.Add("(None)");
        foreach (var scheme in _colorSchemeService.LoadSchemes()) {
            _colorSchemeComboBox.Items.Add(scheme.Name);
        }

        // Restore selection
        if (currentSelection != null) {
            var idx = _colorSchemeComboBox.Items.IndexOf(currentSelection);
            _colorSchemeComboBox.SelectedIndex = idx >= 0 ? idx : 0;
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
        _config.Metric = (MetricType)_metricComboBox.SelectedIndex;
        var selectedScheme = _colorSchemeComboBox.SelectedItem?.ToString();
        _config.ColorSchemeName = selectedScheme == "(None)" ? "" : (selectedScheme ?? "");
        _config.DecimalPlaces = (int)_decimalPlacesNumeric.Value;
        _config.ShowIcon = _showIconCheckBox.Checked;

        DialogResult = DialogResult.OK;
        Close();
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
