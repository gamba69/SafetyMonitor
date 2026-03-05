using MaterialSkin;
using SafetyMonitor.Models;
using SafetyMonitor.Services;

namespace SafetyMonitor.Forms;

public class ValueTileEditorForm : ThemedCaptionForm {
    #region Private Fields

    private readonly ColorSchemeService _colorSchemeService;
    private readonly ValueSchemeService _valueSchemeService;
    private readonly ValueTileConfig _config;
    private Button _cancelButton = null!;
    private ComboBox _colorSchemeComboBox = null!;
    private ComboBox _iconColorSchemeComboBox = null!;
    private ComboBox _textColorSchemeComboBox = null!;
    private ComboBox _valueSchemeComboBox = null!;
    private ComboBox _metricComboBox = null!;
    private ComboBox _displayModeComboBox = null!;
    private Button _editSchemesButton = null!;
    private Button _editValueSchemesButton = null!;
    private Button _saveButton = null!;
    private CheckBox _showIconCheckBox = null!;
    private CheckBox _showUnitCheckBox = null!;
    private TextBox _titleTextBox = null!;
    private MetricType? _lastMetricForSchemeDefaults;

    #endregion Private Fields

    #region Public Constructors

    public ValueTileEditorForm(ValueTileConfig config) {
        _config = config;
        _colorSchemeService = new ColorSchemeService();
        _valueSchemeService = new ValueSchemeService();

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

    private static Panel CreateDescriptionSection(string title, string? subtitle, string[] details, Font titleFont, Font textFont, int maxWidth) {
        var headerPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 10),
            Cursor = Cursors.Hand
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerLabel = new Label {
            Text = title,
            Font = titleFont,
            AutoSize = true,
            MaximumSize = new Size(maxWidth, 0),
            Margin = new Padding(0)
        };
        headerPanel.Controls.Add(headerLabel, 0, 0);

        Label? subtitleLabel = null;
        if (!string.IsNullOrWhiteSpace(subtitle)) {
            subtitleLabel = new Label {
                Text = subtitle,
                Font = CreateSafeFont(textFont.FontFamily.Name, textFont.Size, FontStyle.Italic),
                AutoSize = true,
                MaximumSize = new Size(maxWidth, 0),
                Margin = new Padding(0, 2, 0, 0)
            };
            headerPanel.Controls.Add(subtitleLabel, 0, 1);
        }

        var detailsToggle = new PictureBox {
            Size = new Size(22, 22),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Cursor = Cursors.Hand,
            Margin = new Padding(0),
            Dock = DockStyle.Top
        };
        headerPanel.Controls.Add(detailsToggle, 1, 0);

        var bulletPanel = new TableLayoutPanel {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = (details.Length + 1) / 2,
            Margin = new Padding(0, 4, 0, 0)
        };
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var detailColumnWidth = Math.Max(120, maxWidth / 2 - 10);
        var detailTextWidth = Math.Max(80, detailColumnWidth - 14);
        for (var index = 0; index < details.Length; index++) {
            var row = index / 2;
            var column = index % 2;
            if (row >= bulletPanel.RowStyles.Count) {
                bulletPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var itemPanel = new TableLayoutPanel {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = column == 0 ? new Padding(0, 0, 12, 2) : new Padding(0, 0, 0, 2)
            };
            itemPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14F));
            itemPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            itemPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var bulletLabel = new Label {
                Text = "•",
                Font = textFont,
                AutoSize = true,
                Margin = new Padding(0)
            };
            var textLabel = new Label {
                Text = details[index],
                Font = textFont,
                AutoSize = true,
                MaximumSize = new Size(detailTextWidth, 0),
                Margin = new Padding(0)
            };

            itemPanel.Controls.Add(bulletLabel, 0, 0);
            itemPanel.Controls.Add(textLabel, 1, 0);
            bulletPanel.Controls.Add(itemPanel, column, row);
        }
        headerPanel.Controls.Add(bulletPanel, 0, 2);
        headerPanel.SetColumnSpan(bulletPanel, 2);

        var detailsExpanded = false;
        void UpdateDetailsToggle() {
            bulletPanel.Visible = detailsExpanded;
            detailsToggle.Image?.Dispose();
            detailsToggle.Image = MaterialIcons.GetIcon(detailsExpanded ? "keyboard_double_arrow_up" : "keyboard_double_arrow_down", headerLabel.ForeColor, 20, IconRenderPreset.DarkOutlined);
        }

        void ToggleDetails() {
            detailsExpanded = !detailsExpanded;
            UpdateDetailsToggle();
        }

        void WireToggleClick(Control control) {
            control.Click += (_, _) => ToggleDetails();
            foreach (Control child in control.Controls) {
                WireToggleClick(child);
            }
        }

        detailsToggle.Click += (_, _) => ToggleDetails();
        headerLabel.Click += (_, _) => ToggleDetails();
        if (subtitleLabel != null) {
            subtitleLabel.Click += (_, _) => ToggleDetails();
        }
        WireToggleClick(bulletPanel);
        headerPanel.MouseClick += (_, e) => {
            if (e.Y <= headerLabel.Bottom) {
                ToggleDetails();
            }
        };
        headerLabel.ForeColorChanged += (_, _) => UpdateDetailsToggle();
        if (subtitleLabel != null) {
            subtitleLabel.ForeColorChanged += (_, _) => UpdateDetailsToggle();
        }
        UpdateDetailsToggle();

        return headerPanel;
    }

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        ApplyThemeRecursive(this, isLight);
        UpdateSchemeEditorButtonIcons(isLight);
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
                case ComboBox cmb:
                    ThemedComboBoxStyler.Apply(cmb, isLight);
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
        RefreshTextColorSchemeCombo();
    }

    private void EditValueSchemesButton_Click(object? sender, EventArgs e) {
        using var editor = new ValueSchemeEditorForm();
        editor.ShowDialog(this);
        RefreshValueSchemeCombo();
    }


    private void UpdateSchemeEditorButtonIcons(bool isLight) {
        var iconColor = isLight ? Color.FromArgb(48, 48, 48) : Color.White;

        if (_editSchemesButton != null) {
            var colorSchemesIcon = MaterialIcons.GetIcon(MaterialIcons.MenuViewColorSchemes, iconColor, 18, IconRenderPreset.DarkOutlined);
            var oldImage = _editSchemesButton.Image;
            _editSchemesButton.Image = colorSchemesIcon;
            oldImage?.Dispose();
        }

        if (_editValueSchemesButton != null) {
            var valueSchemesIcon = MaterialIcons.GetIcon(MaterialIcons.MenuViewValueSchemes, iconColor, 18, IconRenderPreset.DarkOutlined);
            var oldImage = _editValueSchemesButton.Image;
            _editValueSchemesButton.Image = valueSchemesIcon;
            oldImage?.Dispose();
        }
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

        // Root layout with fixed footer buttons and scrollable content area
        var rootLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scrollPanel = new Panel {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = new Padding(0)
        };

        // Main layout
        var mainLayout = new TableLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 8,
            Margin = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Description
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Metric
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Display mode
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Color Schemes
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: Value Schemes
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 6: Icon + Unit
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 7: Bottom spacing

        var descriptionSection = CreateDescriptionSection(
            "Configure value tile content and appearance",
            "Resize the tile in Dashboard Editor by dragging tile borders.",
            [
                "Set the tile title and choose the displayed metric.",
                "Choose whether to show value, scheme text, or both.",
                "Pick color schemes for value, text and icon.",
                "Choose an optional text value scheme.",
                "Toggle icon and unit visibility for the tile."
            ],
            titleFont,
            normalFont,
            500);
        mainLayout.Controls.Add(descriptionSection, 0, 0);

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
        _metricComboBox.SelectedIndexChanged += MetricComboBox_SelectedIndexChanged;

        _displayModeComboBox = new ComboBox { Font = normalFont, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _displayModeComboBox.Items.AddRange([
            "Value Only",
            "Text Only",
            "Text + Value"
        ]);
        var displayModePanel = CreateLabeledControl("Display mode:", _displayModeComboBox, titleFont);
        mainLayout.Controls.Add(displayModePanel, 0, 3);

        // Row 3: Value scheme + Icon scheme + Scheme editor
        _colorSchemeComboBox = new ComboBox { Font = normalFont, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        _iconColorSchemeComboBox = new ComboBox { Font = normalFont, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        _textColorSchemeComboBox = new ComboBox { Font = normalFont, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        _valueSchemeComboBox = new ComboBox { Font = normalFont, Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        RefreshColorSchemeCombo();
        RefreshIconColorSchemeCombo();
        RefreshTextColorSchemeCombo();
        RefreshValueSchemeCombo();

        var valueSchemePanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
        var valueSchemeLabel = new Label { Text = "Value color:", Font = titleFont, AutoSize = true, Location = new Point(0, 0) };
        _colorSchemeComboBox.Location = new Point(0, valueSchemeLabel.Bottom + 5);
        valueSchemePanel.Controls.Add(valueSchemeLabel);
        valueSchemePanel.Controls.Add(_colorSchemeComboBox);

        var iconSchemePanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
        var iconSchemeLabel = new Label { Text = "Icon color:", Font = titleFont, AutoSize = true, Location = new Point(0, 0) };
        _iconColorSchemeComboBox.Location = new Point(0, iconSchemeLabel.Bottom + 5);
        iconSchemePanel.Controls.Add(iconSchemeLabel);
        iconSchemePanel.Controls.Add(_iconColorSchemeComboBox);

        var textSchemeColorPanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
        var textSchemeColorLabel = new Label { Text = "Text color:", Font = titleFont, AutoSize = true, Location = new Point(0, 0) };
        _textColorSchemeComboBox.Location = new Point(0, textSchemeColorLabel.Bottom + 5);
        textSchemeColorPanel.Controls.Add(textSchemeColorLabel);
        textSchemeColorPanel.Controls.Add(_textColorSchemeComboBox);

        _editSchemesButton = new Button { Text = "Color schemes...", Width = 160, Height = 30, Font = normalFont, Margin = new Padding(0, 24, 0, 0), TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleLeft };
        _editSchemesButton.Click += EditSchemesButton_Click;

        var schemesPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, WrapContents = false, Margin = new Padding(0, 5, 0, 5) };
        schemesPanel.Controls.Add(valueSchemePanel);
        schemesPanel.Controls.Add(iconSchemePanel);
        schemesPanel.Controls.Add(_editSchemesButton);
        mainLayout.Controls.Add(schemesPanel, 0, 4);

        // Row 4: Text value + text color + value scheme editor
        var textSchemePanel = new Panel { AutoSize = true, Margin = new Padding(0, 0, 10, 0) };
        var textSchemeLabel = new Label { Text = "Text value:", Font = titleFont, AutoSize = true, Location = new Point(0, 0) };
        _valueSchemeComboBox.Location = new Point(0, textSchemeLabel.Bottom + 5);
        textSchemePanel.Controls.Add(textSchemeLabel);
        textSchemePanel.Controls.Add(_valueSchemeComboBox);

        _editValueSchemesButton = new Button { Text = "Value schemes...", Width = 180, Height = 30, Font = normalFont, Margin = new Padding(0, 24, 0, 0), TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleLeft };
        _editValueSchemesButton.Click += EditValueSchemesButton_Click;

        var valueSchemesPanel = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, WrapContents = false, Margin = new Padding(0, 0, 0, 5) };
        valueSchemesPanel.Controls.Add(textSchemePanel);
        valueSchemesPanel.Controls.Add(textSchemeColorPanel);
        valueSchemesPanel.Controls.Add(_editValueSchemesButton);

        var iconPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 10, 0, 5) };
        _showIconCheckBox = new CheckBox { Text = "Show Icon", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 3, 12, 0) };
        _showUnitCheckBox = new CheckBox { Text = "Show Unit", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 3, 0, 0) };
        iconPanel.Controls.Add(_showIconCheckBox);
        iconPanel.Controls.Add(_showUnitCheckBox);
        mainLayout.Controls.Add(valueSchemesPanel, 0, 5);
        mainLayout.Controls.Add(iconPanel, 0, 6);

        // Row 7: Bottom spacing
        mainLayout.Controls.Add(new Panel { Height = 10 }, 0, 7);

        scrollPanel.Controls.Add(mainLayout);
        rootLayout.Controls.Add(scrollPanel, 0, 0);

        // Fixed footer buttons
        var buttonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 10, 0, 0) };
        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont, Margin = new Padding(0) };
        _cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        buttonPanel.Controls.Add(_cancelButton);
        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton);
        rootLayout.Controls.Add(buttonPanel, 0, 1);

        Controls.Add(rootLayout);

        // Set form size after layout
        MinimumSize = new Size(560, 560);
        ClientSize = new Size(560, 560);
    }

    private void LoadConfig() {
        _titleTextBox.Text = _config.Title;
        _metricComboBox.SelectedIndex = (int)_config.Metric;
        ApplyMetricDefaultSchemes(_config.Metric, force: true);

        if (string.IsNullOrEmpty(_config.ColorSchemeName)) {
            _colorSchemeComboBox.SelectedIndex = 0; // "(None)"
        } else {
            var schemeIndex = _colorSchemeComboBox.Items.IndexOf(_config.ColorSchemeName);
            _colorSchemeComboBox.SelectedIndex = schemeIndex >= 0 ? schemeIndex : 0;
        }

        _showIconCheckBox.Checked = _config.ShowIcon;
        _showUnitCheckBox.Checked = _config.ShowUnit;

        if (string.IsNullOrEmpty(_config.IconColorSchemeName)) {
            _iconColorSchemeComboBox.SelectedIndex = 0; // "(None)"
        } else {
            var iconSchemeIndex = _iconColorSchemeComboBox.Items.IndexOf(_config.IconColorSchemeName);
            _iconColorSchemeComboBox.SelectedIndex = iconSchemeIndex >= 0 ? iconSchemeIndex : 0;
        }

        if (string.IsNullOrEmpty(_config.TextColorSchemeName)) {
            _textColorSchemeComboBox.SelectedIndex = 0; // "(None)"
        } else {
            var textSchemeColorIndex = _textColorSchemeComboBox.Items.IndexOf(_config.TextColorSchemeName);
            _textColorSchemeComboBox.SelectedIndex = textSchemeColorIndex >= 0 ? textSchemeColorIndex : 0;
        }

        if (string.IsNullOrEmpty(_config.ValueSchemeName)) {
            _valueSchemeComboBox.SelectedIndex = 0; // "(None)"
        } else {
            var valueSchemeIndex = _valueSchemeComboBox.Items.IndexOf(_config.ValueSchemeName);
            _valueSchemeComboBox.SelectedIndex = valueSchemeIndex >= 0 ? valueSchemeIndex : 0;
        }

        _displayModeComboBox.SelectedIndex = _config.DisplayMode switch {
            ValueTileDisplayMode.ValueOnly => 0,
            ValueTileDisplayMode.TextAndValue => 2,
            _ => 1
        };
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
        _iconColorSchemeComboBox.Items.Add("(None)");
        foreach (var scheme in _colorSchemeService.LoadSchemes()) {
            _iconColorSchemeComboBox.Items.Add(scheme.Name);
        }

        if (currentSelection != null) {
            var idx = _iconColorSchemeComboBox.Items.IndexOf(currentSelection);
            _iconColorSchemeComboBox.SelectedIndex = idx >= 0 ? idx : 0;
        }
    }

    private void RefreshTextColorSchemeCombo() {
        var currentSelection = _textColorSchemeComboBox.SelectedItem?.ToString();
        _textColorSchemeComboBox.Items.Clear();
        _textColorSchemeComboBox.Items.Add("(None)");
        foreach (var scheme in _colorSchemeService.LoadSchemes()) {
            _textColorSchemeComboBox.Items.Add(scheme.Name);
        }

        if (currentSelection != null) {
            var idx = _textColorSchemeComboBox.Items.IndexOf(currentSelection);
            _textColorSchemeComboBox.SelectedIndex = idx >= 0 ? idx : 0;
        }
    }

    private void RefreshValueSchemeCombo() {
        var currentSelection = _valueSchemeComboBox.SelectedItem?.ToString();
        _valueSchemeComboBox.Items.Clear();
        _valueSchemeComboBox.Items.Add("(None)");
        foreach (var scheme in _valueSchemeService.LoadSchemes()) {
            _valueSchemeComboBox.Items.Add(scheme.Name);
        }

        if (currentSelection != null) {
            var idx = _valueSchemeComboBox.Items.IndexOf(currentSelection);
            _valueSchemeComboBox.SelectedIndex = idx >= 0 ? idx : 0;
        }
    }

    private void MetricComboBox_SelectedIndexChanged(object? sender, EventArgs e) {
        if (_metricComboBox.SelectedIndex < 0 || _metricComboBox.SelectedIndex >= Enum.GetValues<MetricType>().Length) {
            return;
        }

        ApplyMetricDefaultSchemes((MetricType)_metricComboBox.SelectedIndex);
    }

    private void ApplyMetricDefaultSchemes(MetricType metric, bool force = false) {
        var previousMetric = _lastMetricForSchemeDefaults;
        ApplyDefaultToCombo(_colorSchemeComboBox, ColorSchemeService.GetDefaultSchemeName(metric), previousMetric, ColorSchemeService.GetDefaultSchemeName, force);
        ApplyDefaultToCombo(_iconColorSchemeComboBox, ColorSchemeService.GetDefaultSchemeName(metric), previousMetric, ColorSchemeService.GetDefaultSchemeName, force);
        ApplyDefaultToCombo(_textColorSchemeComboBox, ColorSchemeService.GetDefaultSchemeName(metric), previousMetric, ColorSchemeService.GetDefaultSchemeName, force);
        ApplyDefaultToCombo(_valueSchemeComboBox, ValueSchemeService.GetDefaultSchemeName(metric), previousMetric, ValueSchemeService.GetDefaultSchemeName, force);
        _lastMetricForSchemeDefaults = metric;
    }

    private static void ApplyDefaultToCombo(
        ComboBox comboBox,
        string defaultName,
        MetricType? previousMetric,
        Func<MetricType, string> defaultResolver,
        bool force) {
        if (string.IsNullOrEmpty(defaultName)) {
            return;
        }

        var selected = comboBox.SelectedItem?.ToString();
        var canAutoReplace = force
            || string.IsNullOrEmpty(selected)
            || selected == "(None)"
            || (previousMetric.HasValue && string.Equals(selected, defaultResolver(previousMetric.Value), StringComparison.Ordinal));

        if (!canAutoReplace) {
            return;
        }

        var defaultIndex = comboBox.Items.IndexOf(defaultName);
        if (defaultIndex >= 0) {
            comboBox.SelectedIndex = defaultIndex;
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        _config.Title = _titleTextBox.Text;
        _config.Metric = (MetricType)_metricComboBox.SelectedIndex;
        var selectedScheme = _colorSchemeComboBox.SelectedItem?.ToString();
        _config.ColorSchemeName = selectedScheme == "(None)" ? "" : (selectedScheme ?? "");
        var selectedIconScheme = _iconColorSchemeComboBox.SelectedItem?.ToString();
        _config.IconColorSchemeName = selectedIconScheme == "(None)" ? "" : (selectedIconScheme ?? "");
        var selectedTextColorScheme = _textColorSchemeComboBox.SelectedItem?.ToString();
        _config.TextColorSchemeName = selectedTextColorScheme == "(None)" ? "" : (selectedTextColorScheme ?? "");
        var selectedValueScheme = _valueSchemeComboBox.SelectedItem?.ToString();
        _config.ValueSchemeName = selectedValueScheme == "(None)" ? "" : (selectedValueScheme ?? "");
        _config.DisplayMode = _displayModeComboBox.SelectedIndex switch {
            0 => ValueTileDisplayMode.ValueOnly,
            2 => ValueTileDisplayMode.TextAndValue,
            _ => ValueTileDisplayMode.TextOnly
        };
        _config.ShowIcon = _showIconCheckBox.Checked;
        _config.ShowUnit = _showUnitCheckBox.Checked;

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
