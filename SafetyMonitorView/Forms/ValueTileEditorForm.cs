using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

public class ValueTileEditorForm : Form {
    #region Private Fields

    private readonly ColorSchemeService _colorSchemeService;
    private readonly ValueTileConfig _config;
    private Button _cancelButton = null!;
    private ComboBox _colorSchemeComboBox = null!;
    private NumericUpDown _decimalPlacesNumeric = null!;
    private ComboBox _iconColorSchemeComboBox = null!;
    private ComboBox _metricComboBox = null!;
    private Button _editSchemesButton = null!;
    private Button _saveButton = null!;
    private CheckBox _showIconCheckBox = null!;
    private TextBox _titleTextBox = null!;

    #endregion Private Fields

    #region Public Constructors

    public ValueTileEditorForm(ValueTileConfig config, Dashboard dashboard) {
        _config = config;
        _colorSchemeService = new ColorSchemeService();

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.WindowTileValue);
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
        UpdateSchemeEditorButtonIcon(isLight);
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
        RefreshIconColorSchemeCombo();
    }


    private void UpdateSchemeEditorButtonIcon(bool isLight) {
        if (_editSchemesButton is null) {
            return;
        }

        var iconColor = isLight ? Color.FromArgb(48, 48, 48) : Color.White;
        var icon = MaterialIcons.GetIcon(MaterialIcons.MenuViewColorSchemes, iconColor, 18);
        var oldImage = _editSchemesButton.Image;
        _editSchemesButton.Image = icon;
        oldImage?.Dispose();
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
            RowCount = 7,
            AutoSize = true
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Description
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Metric
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Schemes
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Decimal + Icon
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 5: Spacer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 6: Buttons

        var descriptionLabel = new Label {
            Text = "Use this form to configure value tile content: title, metric, display options, and color scheme.\n" +
                   "Tile size is edited in the Dashboard Editor by dragging tile borders on the grid.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Margin = new Padding(0, 0, 0, 10)
        };
        mainLayout.Controls.Add(descriptionLabel, 0, 0);

        // Row 1: Title
        var titlePanel = CreateLabeledControl("Title:", _titleTextBox = new TextBox { Font = normalFont, Dock = DockStyle.Fill }, titleFont);
        mainLayout.Controls.Add(titlePanel, 0, 1);

        // Row 1: Metric
        _metricComboBox = new ComboBox { Font = normalFont, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (MetricType metric in Enum.GetValues<MetricType>()) {
            _metricComboBox.Items.Add(metric.GetDisplayName());
        }

        var metricPanel = CreateLabeledControl("Metric:", _metricComboBox, titleFont);
        mainLayout.Controls.Add(metricPanel, 0, 2);

        // Row 3: Value scheme + Icon scheme + Scheme editor
        _colorSchemeComboBox = new ComboBox { Font = normalFont, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        _iconColorSchemeComboBox = new ComboBox { Font = normalFont, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        RefreshColorSchemeCombo();
        RefreshIconColorSchemeCombo();

        var valueSchemePanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
        var valueSchemeLabel = new Label { Text = "Value scheme:", Font = titleFont, AutoSize = true, Location = new Point(0, 0) };
        _colorSchemeComboBox.Location = new Point(0, valueSchemeLabel.Bottom + 5);
        valueSchemePanel.Controls.Add(valueSchemeLabel);
        valueSchemePanel.Controls.Add(_colorSchemeComboBox);

        var iconSchemePanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
        var iconSchemeLabel = new Label { Text = "Icon scheme:", Font = titleFont, AutoSize = true, Location = new Point(0, 0) };
        _iconColorSchemeComboBox.Location = new Point(0, iconSchemeLabel.Bottom + 5);
        iconSchemePanel.Controls.Add(iconSchemeLabel);
        iconSchemePanel.Controls.Add(_iconColorSchemeComboBox);

        _editSchemesButton = new Button { Text = "Scheme editor...", Width = 160, Height = 30, Font = normalFont, Margin = new Padding(0, 24, 0, 0), TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleLeft };
        _editSchemesButton.Click += EditSchemesButton_Click;

        var schemesPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, WrapContents = false, Margin = new Padding(0, 5, 0, 5) };
        schemesPanel.Controls.Add(valueSchemePanel);
        schemesPanel.Controls.Add(iconSchemePanel);
        schemesPanel.Controls.Add(_editSchemesButton);
        mainLayout.Controls.Add(schemesPanel, 0, 3);

        // Row 3: Decimal places + Show icon
        var decimalPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 10, 0, 5) };
        decimalPanel.Controls.Add(new Label { Text = "Decimal Places:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 5, 5, 0) });
        _decimalPlacesNumeric = new NumericUpDown { Width = 70, Minimum = 0, Maximum = 5, Value = 1, Font = normalFont, Margin = new Padding(0, 0, 20, 0) };
        decimalPanel.Controls.Add(_decimalPlacesNumeric);
        _showIconCheckBox = new CheckBox { Text = "Show Icon", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(10, 3, 0, 0) };
        decimalPanel.Controls.Add(_showIconCheckBox);
        mainLayout.Controls.Add(decimalPanel, 0, 4);

        // Row 5: Spacer (empty)
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 5);

        // Row 6: Buttons
        var buttonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 10, 0, 0) };
        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont, Margin = new Padding(0) };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);
        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);
        mainLayout.Controls.Add(buttonPanel, 0, 6);

        Controls.Add(mainLayout);

        // Set form size after layout
        MinimumSize = new Size(560, 500);
        ClientSize = new Size(560, 500);
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

        if (string.IsNullOrEmpty(_config.IconColorSchemeName)) {
            _iconColorSchemeComboBox.SelectedIndex = 0; // "(Theme)"
        } else {
            var iconSchemeIndex = _iconColorSchemeComboBox.Items.IndexOf(_config.IconColorSchemeName);
            _iconColorSchemeComboBox.SelectedIndex = iconSchemeIndex >= 0 ? iconSchemeIndex : 0;
        }
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

    private void RefreshIconColorSchemeCombo() {
        var currentSelection = _iconColorSchemeComboBox.SelectedItem?.ToString();
        _iconColorSchemeComboBox.Items.Clear();
        _iconColorSchemeComboBox.Items.Add("(Theme)");
        foreach (var scheme in _colorSchemeService.LoadSchemes()) {
            _iconColorSchemeComboBox.Items.Add(scheme.Name);
        }

        if (currentSelection != null) {
            var idx = _iconColorSchemeComboBox.Items.IndexOf(currentSelection);
            _iconColorSchemeComboBox.SelectedIndex = idx >= 0 ? idx : 0;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        _config.Title = _titleTextBox.Text;
        _config.Metric = (MetricType)_metricComboBox.SelectedIndex;
        var selectedScheme = _colorSchemeComboBox.SelectedItem?.ToString();
        _config.ColorSchemeName = selectedScheme == "(None)" ? "" : (selectedScheme ?? "");
        var selectedIconScheme = _iconColorSchemeComboBox.SelectedItem?.ToString();
        _config.IconColorSchemeName = selectedIconScheme == "(Theme)" ? "" : (selectedIconScheme ?? "");
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
