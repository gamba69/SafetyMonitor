using DataStorage.Models;
using MaterialSkin;
using SafetyMonitor.Services;
using SafetyMonitor.Models;
using System.Collections;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents chart tile editor form and encapsulates its related behavior and state.
/// </summary>
public class ChartTileEditorForm : ThemedCaptionForm {

    #region Private Types

    /// <summary>
    /// Represents metric row colors and encapsulates its related behavior and state.
    /// </summary>
    private sealed class MetricRowColors {

        #region Public Fields

        public Color Dark = Color.LightSkyBlue;
        public Color Light = Color.Blue;

        #endregion Public Fields
    }

    #endregion Private Types

    #region Private Fields

    private readonly ChartTileConfig _config;
    private readonly ValueSchemeService _valueSchemeService;
    private readonly Dashboard _dashboard;
    private FlowLayoutPanel _linkGroupPanel = null!;
    private Color _inputBackColor;
    private Color _inputForeColor;

    private Button _cancelButton = null!;
    private DataGridView _metricsGrid = null!;
    private ComboBox _linkGroupComboBox = null!;
    private Button _saveButton = null!;
    private CheckBox _showGridCheckBox = null!;
    private CheckBox _showInspectorCheckBox = null!;
    private CheckBox _showLegendCheckBox = null!;
    private TextBox _titleTextBox = null!;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartTileEditorForm"/> class.
    /// </summary>
    /// <param name="config">Input value for config.</param>
    /// <param name="dashboard">Input value for dashboard.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public ChartTileEditorForm(ChartTileConfig config, Dashboard dashboard) {
        _config = config;
        _dashboard = dashboard;
        _valueSchemeService = new ValueSchemeService();

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.WindowTileChart);
        ApplyTheme();
        LoadConfig();
    }

    #endregion Public Constructors

    #region Private Methods

    /// <summary>
    /// Creates the labeled control for metric row colors.
    /// </summary>
    /// <param name="labelText">Input value for label text.</param>
    /// <param name="control">Input value for control.</param>
    /// <param name="labelFont">Input value for label font.</param>
    /// <returns>The result of the operation.</returns>
    private static Panel CreateLabeledControl(string labelText, Control control, Font labelFont) {
        var panel = new Panel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 5, 0, 5) };
        var label = new Label { Text = labelText, Font = labelFont, AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 5) };
        control.Dock = DockStyle.Top;
        panel.Controls.Add(control);
        panel.Controls.Add(label);
        return panel;
    }

    /// <summary>
    /// Creates the description section for metric row colors.
    /// </summary>
    /// <param name="title">Input value for title.</param>
    /// <param name="subtitle">Input value for subtitle.</param>
    /// <param name="details">Input value for details.</param>
    /// <param name="titleFont">Input value for title font.</param>
    /// <param name="textFont">Input value for text font.</param>
    /// <param name="maxWidth">Input value for max width.</param>
    /// <returns>The result of the operation.</returns>
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
                Font = new Font(textFont, FontStyle.Italic),
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

    /// <summary>
    /// Adds the metric button click for metric row colors.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void AddMetricButton_Click(object? sender, EventArgs e) {
        var newRow = _metricsGrid.Rows.Add(MetricType.Temperature, AggregationFunction.Average, "Metric", null!, null!, 2.0f, false, 0.5f, false, "(None)");
        _metricsGrid.Rows[newRow].Tag = GenerateUniqueMetricColors();
        _metricsGrid.InvalidateRow(newRow);
    }

    /// <summary>
    /// Executes generate unique metric colors as part of metric row colors processing.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    private MetricRowColors GenerateUniqueMetricColors() {
        var usedColors = new HashSet<int>();
        foreach (DataGridViewRow row in _metricsGrid.Rows) {
            if (row.IsNewRow) {
                continue;
            }

            var colors = EnsureRowColors(row);
            usedColors.Add(colors.Light.ToArgb());
            usedColors.Add(colors.Dark.ToArgb());
        }

        const double saturation = 0.75;
        const double lightThemeValue = 0.55; // darker for light theme
        const double darkThemeValue = 0.82; // lighter for dark theme

        for (var index = 0; index < 360; index++) {
            var hue = (index * 137.508) % 360;
            var lightColor = ColorFromHsv(hue, saturation, lightThemeValue);
            var darkColor = ColorFromHsv(hue, saturation, darkThemeValue);
            if (usedColors.Contains(lightColor.ToArgb()) || usedColors.Contains(darkColor.ToArgb())) {
                continue;
            }

            return new MetricRowColors { Light = lightColor, Dark = darkColor };
        }

        var seed = Guid.NewGuid().GetHashCode();
        var random = new Random(seed);
        var fallbackHue = random.NextDouble() * 360;
        return new MetricRowColors {
            Light = ColorFromHsv(fallbackHue, saturation, lightThemeValue),
            Dark = ColorFromHsv(fallbackHue, saturation, darkThemeValue)
        };
    }

    /// <summary>
    /// Applies the theme for metric row colors.
    /// </summary>
    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        var isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        _inputBackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _inputForeColor = isLight ? Color.Black : Color.White;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = _inputForeColor;

        // DataGridView special handling
        _metricsGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _metricsGrid.DefaultCellStyle.BackColor = _metricsGrid.BackgroundColor;
        _metricsGrid.DefaultCellStyle.ForeColor = _inputForeColor;
        _metricsGrid.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(225, 245, 254) : Color.FromArgb(56, 78, 84);
        _metricsGrid.DefaultCellStyle.SelectionForeColor = _inputForeColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(238, 238, 238) : Color.FromArgb(55, 71, 79);
        _metricsGrid.ColumnHeadersDefaultCellStyle.ForeColor = _inputForeColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _metricsGrid.ColumnHeadersDefaultCellStyle.BackColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _inputForeColor;
        _metricsGrid.EnableHeadersVisualStyles = false;
        _metricsGrid.GridColor = isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(60, 75, 80);

        foreach (var comboBoxColumn in _metricsGrid.Columns.OfType<DataGridViewComboBoxColumn>()) {
            comboBoxColumn.FlatStyle = FlatStyle.Popup;
            comboBoxColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            comboBoxColumn.DefaultCellStyle.BackColor = _metricsGrid.DefaultCellStyle.BackColor;
            comboBoxColumn.DefaultCellStyle.ForeColor = _inputForeColor;
            comboBoxColumn.DefaultCellStyle.SelectionBackColor = _metricsGrid.DefaultCellStyle.SelectionBackColor;
            comboBoxColumn.DefaultCellStyle.SelectionForeColor = _metricsGrid.DefaultCellStyle.SelectionForeColor;
        }

        ApplyThemeRecursive(this, isLight);
    }

    /// <summary>
    /// Applies the theme recursive for metric row colors.
    /// </summary>
    /// <param name="parent">Input value for parent.</param>
    /// <param name="isLight">Input value for is light.</param>
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

    /// <summary>
    /// Initializes metric row colors state and required resources.
    /// </summary>
    private void InitializeComponent() {
        Text = "Chart Tile Editor";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        Padding = new Padding(15);

        var titleFont = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold);
        var normalFont = CreateSafeFont("Segoe UI", 9.5f);
        var helpFontSize = HelpTextFontService.GetAdjustedSize();
        var helpTitleFont = CreateSafeFont("Segoe UI", helpFontSize, FontStyle.Bold);
        var helpFont = CreateSafeFont("Segoe UI", helpFontSize);

        // Root layout with fixed footer buttons and scrollable content
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
            RowCount = 7,
            Margin = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Description
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Metrics label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Metrics grid container
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Add/Remove buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 5: Link group row
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 6: Options row

        var descriptionSection = CreateDescriptionSection(
            "Configure chart tile content and behavior",
            "Resize the tile in Dashboard Editor by dragging tile borders.",
            [
                "Set the tile title shown in the dashboard.",
                "Add one or more metrics and choose aggregation/line settings.",
                "Select a dashboard link group for this chart.",
                "Enable or disable legend, grid, and inspector."
            ],
            helpTitleFont,
            helpFont,
            710);
        mainLayout.Controls.Add(descriptionSection, 0, 0);

        // Row 1: Title
        var titlePanel = CreateLabeledControl("Title:", _titleTextBox = new TextBox { Font = normalFont, Dock = DockStyle.Fill }, titleFont);
        mainLayout.Controls.Add(titlePanel, 0, 1);

        // Row 1: Metrics label
        var metricsLabel = new Label { Text = "Metrics and Aggregations:", Font = titleFont, AutoSize = true, Margin = new Padding(0, 10, 0, 5) };
        mainLayout.Controls.Add(metricsLabel, 0, 2);

        // Row 2: Metrics grid
        _metricsGrid = new DataGridView {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AutoGenerateColumns = false,
            RowHeadersVisible = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = normalFont,
            Margin = new Padding(0)
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
            ToolTipText = "Aggregation",
            FillWeight = 22,
            DataSource = Enum.GetValues<AggregationFunction>()
                .Where(f => f != AggregationFunction.Sum && f != AggregationFunction.Count)
                .Select(f => new { Value = f, Display = f.ToString() })
                .ToList(),
            DisplayMember = "Display",
            ValueMember = "Value",
            ValueType = typeof(AggregationFunction)
        });

        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Label", HeaderText = "Label", FillWeight = 18 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LightColor", HeaderText = "Light", FillWeight = 8, ReadOnly = true });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DarkColor", HeaderText = "Dark", FillWeight = 8, ReadOnly = true });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineWidth", HeaderText = "W", ToolTipText = "Width", FillWeight = 7 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Smooth", HeaderText = "Smth", ToolTipText = "Smoothing", FillWeight = 8 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tension", HeaderText = "Tns", ToolTipText = "Tension", FillWeight = 8 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "ShowMarkers", HeaderText = "Mark", ToolTipText = "Markers", FillWeight = 12 });

        var valueSchemeCol = new DataGridViewComboBoxColumn {
            Name = "ValueScheme",
            HeaderText = "Val.Scheme",
            ToolTipText = "Value Scheme",
            FillWeight = 18,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        };
        valueSchemeCol.Items.Add("(None)");
        foreach (var vs in _valueSchemeService.LoadSchemes()) {
            valueSchemeCol.Items.Add(vs.Name);
        }
        _metricsGrid.Columns.Add(valueSchemeCol);

        foreach (DataGridViewColumn column in _metricsGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _metricsGrid.CellClick += MetricsGrid_CellClick;
        _metricsGrid.CellFormatting += MetricsGrid_CellFormatting;
        _metricsGrid.DataError += MetricsGrid_DataError;
        _metricsGrid.EditingControlShowing += MetricsGrid_EditingControlShowing;

        var metricsGridContainer = new Panel {
            Dock = DockStyle.Fill,
            Height = 260,
            MinimumSize = new Size(0, 220),
            Margin = new Padding(0, 0, 0, 5)
        };
        metricsGridContainer.Controls.Add(_metricsGrid);
        mainLayout.Controls.Add(metricsGridContainer, 0, 3);

        // Row 3: Add/Remove buttons
        var gridButtonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 10) };
        var addMetricButton = new Button { Text = "Add Metric", Width = 130, Height = 32, Font = normalFont, Margin = new Padding(0, 0, 10, 0) };
        addMetricButton.Click += AddMetricButton_Click;
        gridButtonPanel.Controls.Add(addMetricButton);
        var removeMetricButton = new Button { Text = "Delete", Width = 110, Height = 32, Font = normalFont, Margin = new Padding(0) };
        removeMetricButton.Click += RemoveMetricButton_Click;
        gridButtonPanel.Controls.Add(removeMetricButton);
        mainLayout.Controls.Add(gridButtonPanel, 0, 4);

        // Row 5: Link group
        _linkGroupPanel = new FlowLayoutPanel {
            AutoSize = false,
            Height = 44,
            MinimumSize = new Size(0, 44),
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 6, 0, 8),
            Padding = new Padding(0, 4, 0, 0)
        };
        var periodLabel = new Label {
            Text = "Link Group:",
            Font = titleFont,
            AutoSize = true,
            Margin = new Padding(0, 7, 8, 0)
        };
        _linkGroupComboBox = new ComboBox {
            Width = 160,
            Font = normalFont,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 2, 0, 0)
        };
        foreach (var group in _dashboard.GetAvailableLinkGroups()) {
            _linkGroupComboBox.Items.Add(group.GetDisplayName(_dashboard.GetLinkGroupPeriodShortName(group)));
        }
        _linkGroupPanel.Controls.Add(periodLabel);
        _linkGroupPanel.Controls.Add(_linkGroupComboBox);
        mainLayout.Controls.Add(_linkGroupPanel, 0, 5);

        // Row 6: Options
        var optionsPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 0, 0, 10) };
        _showLegendCheckBox = new CheckBox { Text = "Legend", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 2, 14, 0) };
        optionsPanel.Controls.Add(_showLegendCheckBox);
        _showGridCheckBox = new CheckBox { Text = "Grid", Font = normalFont, AutoSize = true, Checked = true, Margin = new Padding(0, 2, 14, 0) };
        optionsPanel.Controls.Add(_showGridCheckBox);
        _showInspectorCheckBox = new CheckBox { Text = "Inspector", Font = normalFont, AutoSize = true, Checked = false, Margin = new Padding(0, 2, 0, 0) };
        optionsPanel.Controls.Add(_showInspectorCheckBox);
        mainLayout.Controls.Add(optionsPanel, 0, 6);

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

        // Set form size
        MinimumSize = new Size(780, 620);
        ClientSize = new Size(820, 680);
    }
    /// <summary>
    /// Loads the config for metric row colors.
    /// </summary>
    private void LoadConfig() {
        _titleTextBox.Text = _config.Title;
        _config.LinkGroup = ChartLinkGroupInfo.NormalizeGroup(_config.LinkGroup, _dashboard.UsedLinkGroups);
        _linkGroupPanel.Visible = _dashboard.UsedLinkGroups > 1;

        if (_linkGroupComboBox.Items.Count > 0) {
            var groups = _dashboard.GetAvailableLinkGroups().ToList();
            var idx = groups.IndexOf(_config.LinkGroup);
            _linkGroupComboBox.SelectedIndex = Math.Max(0, idx);
        }

        _showLegendCheckBox.Checked = _config.ShowLegend;
        _showGridCheckBox.Checked = _config.ShowGrid;
        _showInspectorCheckBox.Checked = _config.ShowInspector;

        foreach (var agg in _config.MetricAggregations) {
            var rowIndex = _metricsGrid.Rows.Add(agg.Metric, agg.Function, agg.Label, null!, null!, agg.LineWidth, agg.Smooth, agg.Tension, agg.ShowMarkers,
                string.IsNullOrEmpty(agg.ValueSchemeName) ? "(None)" : agg.ValueSchemeName);
            _metricsGrid.Rows[rowIndex].Tag = new MetricRowColors {
                Light = agg.Color,
                Dark = agg.DarkThemeColor.IsEmpty ? agg.Color : agg.DarkThemeColor
            };
            _metricsGrid.InvalidateRow(rowIndex);
        }
    }
    /// <summary>
    /// Executes metrics grid cell click as part of metric row colors processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
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

    /// <summary>
    /// Executes metrics grid cell formatting as part of metric row colors processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
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

    /// <summary>
    /// Executes metrics grid data error as part of metric row colors processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
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

    /// <summary>
    /// Executes metrics grid editing control showing as part of metric row colors processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void MetricsGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (e.Control is ComboBox comboBox) {
            comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
            var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
            ThemedComboBoxStyler.Apply(comboBox, isLight);
        }
    }


    /// <summary>
    /// Parses the tension for metric row colors.
    /// </summary>
    /// <param name="rawValue">Input value for raw value.</param>
    /// <returns>The result of the operation.</returns>
    private static float ParseTension(string? rawValue) {
        if (!float.TryParse(rawValue, out var tension)) {
            return 0.5f;
        }

        return Math.Clamp(tension, 0f, 3f);
    }

    /// <summary>
    /// Executes color from hsv as part of metric row colors processing.
    /// </summary>
    /// <param name="hue">Input value for hue.</param>
    /// <param name="saturation">Input value for saturation.</param>
    /// <param name="value">Input value for value.</param>
    /// <returns>The result of the operation.</returns>
    private static Color ColorFromHsv(double hue, double saturation, double value) {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0d, 1d);
        value = Math.Clamp(value, 0d, 1d);

        var c = value * saturation;
        var x = c * (1 - Math.Abs((hue / 60d) % 2 - 1));
        var m = value - c;

        var (rPrime, gPrime, bPrime) = hue switch {
            < 60d => (c, x, 0d),
            < 120d => (x, c, 0d),
            < 180d => (0d, c, x),
            < 240d => (0d, x, c),
            < 300d => (x, 0d, c),
            _ => (c, 0d, x)
        };

        var r = (int)Math.Round((rPrime + m) * 255);
        var g = (int)Math.Round((gPrime + m) * 255);
        var b = (int)Math.Round((bPrime + m) * 255);
        return Color.FromArgb(r, g, b);
    }

    /// <summary>
    /// Ensures the row colors for metric row colors.
    /// </summary>
    /// <param name="row">Input value for row.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets the combo box fallback value for metric row colors.
    /// </summary>
    /// <param name="column">Input value for column.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Removes the metric button click for metric row colors.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void RemoveMetricButton_Click(object? sender, EventArgs e) {
        if (_metricsGrid.SelectedRows.Count > 0) {
            foreach (DataGridViewRow row in _metricsGrid.SelectedRows) {
                if (!row.IsNewRow) {
                    _metricsGrid.Rows.Remove(row);
                }
            }
        }
    }
    /// <summary>
    /// Saves the button click for metric row colors.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void SaveButton_Click(object? sender, EventArgs e) {
        _config.Title = _titleTextBox.Text;
        if (_dashboard.UsedLinkGroups > 1) {
            var groups = _dashboard.GetAvailableLinkGroups().ToList();
            if (_linkGroupComboBox.SelectedIndex < 0 || _linkGroupComboBox.SelectedIndex >= groups.Count) {
                ThemedMessageBox.Show(this, "Please select a link group.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _config.LinkGroup = groups[_linkGroupComboBox.SelectedIndex];
        } else {
            _config.LinkGroup = _dashboard.GetAvailableLinkGroups()[0];
        }

        _config.ShowLegend = _showLegendCheckBox.Checked;
        _config.ShowGrid = _showGridCheckBox.Checked;
        _config.ShowInspector = _showInspectorCheckBox.Checked;
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
                ShowMarkers = (bool)(row.Cells["ShowMarkers"].Value ?? false),
                ValueSchemeName = row.Cells["ValueScheme"].Value?.ToString() is var vs && vs != "(None)" ? vs ?? "" : ""
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

    /// <summary>
    /// Creates the safe font for metric row colors.
    /// </summary>
    /// <param name="familyName">Input value for family name.</param>
    /// <param name="emSize">Input value for em size.</param>
    /// <param name="style">Input value for style.</param>
    /// <returns>The result of the operation.</returns>
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
