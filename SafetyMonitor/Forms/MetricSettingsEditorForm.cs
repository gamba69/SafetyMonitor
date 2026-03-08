using MaterialSkin;
using SafetyMonitor.Models;
using SafetyMonitor.Services;
using System.Linq;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents metric settings editor form and encapsulates its related behavior and state.
/// </summary>
public class MetricSettingsEditorForm : ThemedCaptionForm {

    private Button _cancelButton = null!;
    private DataGridView _metricsGrid = null!;
    private Button _saveButton = null!;
    private readonly List<MetricDisplaySetting> _settings;
    private readonly List<string> _trayValueSchemeNames;
    private bool _isReorderingGrid;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricSettingsEditorForm"/> class.
    /// </summary>
    /// <param name="settings">Collection of settings items used by the operation.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public MetricSettingsEditorForm(IEnumerable<MetricDisplaySetting> settings) {
        _settings = [.. settings.Select(s => new MetricDisplaySetting {
            Metric = s.Metric,
            Decimals = s.Decimals,
            HideZeroes = s.HideZeroes,
            InvertY = s.InvertY,
            LogY = s.LogY,
            TrayName = s.TrayName,
            TrayValueSchemeName = s.TrayValueSchemeName
        })];
        _trayValueSchemeNames = ["(None)", .. new ValueSchemeService().LoadSchemes().Select(s => s.Name).Distinct().OrderBy(n => n)];

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewMetricSettings);
        ApplyTheme();
        LoadSettings();
    }

    /// <summary>
    /// Gets or sets the settings for metric settings editor form. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<MetricDisplaySetting> Settings { get; private set; } = [];

    /// <summary>
    /// Initializes metric settings editor form state and required resources.
    /// </summary>
    private void InitializeComponent() {
        Text = "Metric Settings";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Padding = new Padding(16);
        ClientSize = new Size(920, 560);

        var normalFont = CreateSafeFont("Segoe UI", 10f);
        var helpFontSize = HelpTextFontService.GetAdjustedSize();
        var helpFont = CreateSafeFont("Segoe UI", helpFontSize);
        Font = normalFont;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.Cursor = Cursors.Hand;

        var headerLabel = new Label {
            AutoSize = true,
            Margin = new Padding(0),
            MaximumSize = new Size(860, 0),
            Text = "Configure per-metric display settings. Metric list is fixed and cannot be changed.",
            Font = CreateSafeFont("Segoe UI", helpFontSize, FontStyle.Bold)
        };
        headerPanel.Controls.Add(headerLabel, 0, 0);

        var detailsToggle = new PictureBox {
            Size = new Size(22, 22),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Cursor = Cursors.Hand,
            Margin = new Padding(0),
            Dock = DockStyle.Top
        };
        headerPanel.Controls.Add(detailsToggle, 1, 0);

        const int visualDetailsColumnCount = 2;
        const int detailsColumnCount = visualDetailsColumnCount * 2;
        var details = new[] {
            "Short: predefined metric abbreviation used on compact chart axes.",
            "Decimals: number of digits after decimal point (0..10).",
            "Hide zeroes: if enabled, zero values are shown as empty text instead of 0/0.0.",
            "Invert Y: flips chart Y-axis direction for this metric.",
            "Log Y: enables logarithmic chart Y-axis for this metric.",
            "Tray name: short alias shown in tray/compact displays.",
            "Tray scheme: optional value-scheme text mapping used in tray tooltip."
        };
        var bulletPanel = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = detailsColumnCount,
            RowCount = (details.Length + visualDetailsColumnCount - 1) / visualDetailsColumnCount,
            AutoSize = true,
            Margin = new Padding(0)
        };
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14F));
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14F));
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var detailColumnWidth = Math.Max(120, 810 / visualDetailsColumnCount - 24);
        for (var index = 0; index < details.Length; index++) {
            var row = index / visualDetailsColumnCount;
            var visualColumn = index % visualDetailsColumnCount;
            var bulletColumn = visualColumn * 2;
            var textColumn = bulletColumn + 1;
            if (row >= bulletPanel.RowStyles.Count) {
                bulletPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            bulletPanel.Controls.Add(new Label {
                Text = "•",
                Font = helpFont,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            }, bulletColumn, row);
            bulletPanel.Controls.Add(new Label {
                Text = details[index],
                Font = helpFont,
                AutoSize = true,
                MaximumSize = new Size(detailColumnWidth, 0),
                Margin = visualColumn == 0 ? new Padding(0, 0, 12, 2) : new Padding(0, 0, 0, 2)
            }, textColumn, row);
        }

        headerPanel.Controls.Add(bulletPanel, 0, 1);
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

        detailsToggle.Click += (_, _) => ToggleDetails();
        headerLabel.Click += (_, _) => ToggleDetails();
        headerPanel.MouseClick += (_, e) => {
            if (e.Y <= headerLabel.Bottom) {
                ToggleDetails();
            }
        };
        headerLabel.ForeColorChanged += (_, _) => UpdateDetailsToggle();
        UpdateDetailsToggle();

        root.Controls.Add(headerPanel, 0, 0);

        _metricsGrid = new DataGridView {
            Margin = new Padding(0, 0, 0, 12),
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            Font = normalFont,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };

        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Metric", HeaderText = "Metric", FillWeight = 29, ReadOnly = true, MinimumWidth = 140 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MetricShort", HeaderText = "Short", FillWeight = 10, ReadOnly = true, MinimumWidth = 75, ToolTipText = "Predefined short metric name" });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Decimals", HeaderText = "Decimals", FillWeight = 12, MinimumWidth = 70 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "HideZeroes", HeaderText = "Hide zeroes", FillWeight = 16, MinimumWidth = 95 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "InvertY", HeaderText = "Inv Y", FillWeight = 11, MinimumWidth = 70, ToolTipText = "Invert Y-axis" });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "LogY", HeaderText = "Log Y", FillWeight = 11, MinimumWidth = 70, ToolTipText = "Logarithmic Y-axis" });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrayName", HeaderText = "Tray name", FillWeight = 19, MinimumWidth = 110 });

        var traySchemeColumn = new DataGridViewComboBoxColumn {
            Name = "TrayValueScheme",
            HeaderText = "Tray scheme",
            FillWeight = 16,
            MinimumWidth = 130,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Popup
        };
        traySchemeColumn.Items.AddRange([.. _trayValueSchemeNames.Cast<object>()]);
        _metricsGrid.Columns.Add(traySchemeColumn);

        foreach (DataGridViewColumn column in _metricsGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _metricsGrid.CellValidating += MetricsGrid_CellValidating;
        _metricsGrid.CellEndEdit += MetricsGrid_CellEndEdit;
        _metricsGrid.EditingControlShowing += MetricsGrid_EditingControlShowing;
        root.Controls.Add(_metricsGrid, 0, 1);

        var buttonPanel = new TableLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Right,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont, Margin = new Padding(0) };
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = CreateSafeFont("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_saveButton, 0, 0);
        buttonPanel.Controls.Add(_cancelButton, 1, 0);
        root.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(root);
    }

    /// <summary>
    /// Loads the settings for metric settings editor form.
    /// </summary>
    private void LoadSettings() {
            _metricsGrid.Rows.Clear();
        var map = _settings.ToDictionary(s => s.Metric, s => s);
        foreach (var metric in GetOrderedMetricsForGrid(map)) {
            var s = map.TryGetValue(metric, out var found) ? found : MetricDisplaySettingsStore.GetDefaultSetting(metric);
            _metricsGrid.Rows.Add(metric.GetDisplayName(), metric.GetShortName(), Math.Max(0, s.Decimals), s.HideZeroes, s.InvertY, s.LogY, s.TrayName, NormalizeTrayValueScheme(s.TrayValueSchemeName));
        }
    }

    /// <summary>
    /// Gets the ordered metrics for grid for metric settings editor form.
    /// </summary>
    /// <param name="settingsByMetric">Input value for settings by metric.</param>
    /// <returns>The result of the operation.</returns>
    private static IEnumerable<MetricType> GetOrderedMetricsForGrid(Dictionary<MetricType, MetricDisplaySetting> settingsByMetric) {
        return Enum
            .GetValues<MetricType>()
            .OrderByDescending(metric => {
                var setting = settingsByMetric.TryGetValue(metric, out var existing)
                    ? existing
                    : MetricDisplaySettingsStore.GetDefaultSetting(metric);
                return !string.IsNullOrWhiteSpace(setting.TrayName);
            })
            .ThenBy(metric => (int)metric);
    }

    /// <summary>
    /// Executes metrics grid cell validating as part of metric settings editor form processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void MetricsGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e) {
        if (_metricsGrid.Columns[e.ColumnIndex].Name != "Decimals") {
            return;
        }

        var text = e.FormattedValue?.ToString() ?? string.Empty;
        if (!int.TryParse(text, out var value) || value < 0 || value > 10) {
            e.Cancel = true;
            _metricsGrid.Rows[e.RowIndex].ErrorText = "Decimals must be integer between 0 and 10.";
        } else {
            _metricsGrid.Rows[e.RowIndex].ErrorText = string.Empty;
        }
    }


    /// <summary>
    /// Executes metrics grid cell end edit as part of metric settings editor form processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void MetricsGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0 || _metricsGrid.Columns[e.ColumnIndex].Name != "TrayName") {
            return;
        }

        if (_isReorderingGrid) {
            return;
        }

        var selectedMetric = _metricsGrid.Rows[e.RowIndex].Cells["Metric"].Value?.ToString();
        BeginInvoke(new Action(() => ReorderGridRowsByTrayName(selectedMetric)));
    }

    /// <summary>
    /// Executes reorder grid rows by tray name as part of metric settings editor form processing.
    /// </summary>
    /// <param name="selectedMetric">Input value for selected metric.</param>
    private void ReorderGridRowsByTrayName(string? selectedMetric = null) {
        if (_isReorderingGrid || IsDisposed) {
            return;
        }

        _isReorderingGrid = true;

        try {
            var metricNames = Enum.GetValues<MetricType>().ToDictionary(m => m.GetDisplayName(), m => m);

            var rowStates = _metricsGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow)
                .Select(row => new {
                    Metric = metricNames.TryGetValue(row.Cells["Metric"].Value?.ToString() ?? string.Empty, out var metric) ? metric : (MetricType?)null,
                    Decimals = row.Cells["Decimals"].Value,
                    HideZeroes = row.Cells["HideZeroes"].Value is true,
                    InvertY = row.Cells["InvertY"].Value is true,
                    LogY = row.Cells["LogY"].Value is true,
                    TrayName = row.Cells["TrayName"].Value?.ToString() ?? string.Empty,
                    TrayScheme = NormalizeTrayValueScheme(row.Cells["TrayValueScheme"].Value?.ToString())
                })
                .Where(state => state.Metric.HasValue)
                .Select(state => new {
                    Metric = state.Metric!.Value,
                    state.Decimals,
                    state.HideZeroes,
                    state.InvertY,
                    state.LogY,
                    state.TrayName,
                    state.TrayScheme
                })
                .OrderByDescending(state => !string.IsNullOrWhiteSpace(state.TrayName))
                .ThenBy(state => (int)state.Metric)
                .ToList();

            _metricsGrid.Rows.Clear();

            foreach (var rowState in rowStates) {
                _metricsGrid.Rows.Add(
                    rowState.Metric.GetDisplayName(),
                    rowState.Metric.GetShortName(),
                    rowState.Decimals!,
                    rowState.HideZeroes,
                    rowState.InvertY,
                    rowState.LogY,
                    rowState.TrayName,
                    rowState.TrayScheme);
            }

            if (selectedMetric is null) {
                return;
            }

            foreach (DataGridViewRow row in _metricsGrid.Rows) {
                if (row.IsNewRow) {
                    continue;
                }

                if (!string.Equals(row.Cells["Metric"].Value?.ToString(), selectedMetric, StringComparison.Ordinal)) {
                    continue;
                }

                row.Selected = true;
                _metricsGrid.CurrentCell = row.Cells["TrayName"];
                break;
            }
        } finally {
            _isReorderingGrid = false;
        }
    }

    /// <summary>
    /// Executes metrics grid editing control showing as part of metric settings editor form processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void MetricsGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (_metricsGrid.CurrentCell?.OwningColumn?.Name != "TrayValueScheme" || e.Control is not ComboBox comboBox) {
            return;
        }

        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ThemedComboBoxStyler.Apply(comboBox, isLight);
    }

    /// <summary>
    /// Saves the button click for metric settings editor form.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void SaveButton_Click(object? sender, EventArgs e) {
        var metricNames = Enum.GetValues<MetricType>().ToDictionary(m => m.GetDisplayName(), m => m);
        var newSettings = new List<MetricDisplaySetting>();

            foreach (DataGridViewRow row in _metricsGrid.Rows) {
            if (row.IsNewRow) {
                continue;
            }

            var metricName = row.Cells["Metric"].Value?.ToString() ?? string.Empty;
            if (!metricNames.TryGetValue(metricName, out var metric)) {
                continue;
            }

            _ = int.TryParse(row.Cells["Decimals"].Value?.ToString() ?? "2", out var decimals);
            newSettings.Add(new MetricDisplaySetting {
                Metric = metric,
                Decimals = Math.Clamp(decimals, 0, 10),
                HideZeroes = row.Cells["HideZeroes"].Value is true,
                InvertY = row.Cells["InvertY"].Value is true,
                LogY = row.Cells["LogY"].Value is true,
                TrayName = row.Cells["TrayName"].Value?.ToString() ?? string.Empty,
                TrayValueSchemeName = NormalizeTrayValueScheme(row.Cells["TrayValueScheme"].Value?.ToString())
            });
        }

        Settings = [.. newSettings.OrderBy(s => (int)s.Metric)];
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>
    /// Normalizes the tray value scheme for metric settings editor form.
    /// </summary>
    /// <param name="schemeName">Input value for scheme name.</param>
    /// <returns>The resulting string value.</returns>
    private string NormalizeTrayValueScheme(string? schemeName) {
        if (string.IsNullOrWhiteSpace(schemeName) || schemeName == "(None)") {
            return string.Empty;
        }

        return _trayValueSchemeNames.Contains(schemeName) ? schemeName : string.Empty;
    }

    /// <summary>
    /// Applies the theme for metric settings editor form.
    /// </summary>
    private void ApplyTheme() {
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;

        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        _metricsGrid.BackgroundColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _metricsGrid.DefaultCellStyle.BackColor = _metricsGrid.BackgroundColor;
        _metricsGrid.DefaultCellStyle.ForeColor = ForeColor;
        _metricsGrid.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(225, 245, 254) : Color.FromArgb(56, 78, 84);
        _metricsGrid.DefaultCellStyle.SelectionForeColor = ForeColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(238, 238, 238) : Color.FromArgb(55, 71, 79);
        _metricsGrid.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _metricsGrid.ColumnHeadersDefaultCellStyle.BackColor;
        _metricsGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _metricsGrid.ColumnHeadersDefaultCellStyle.ForeColor;
        _metricsGrid.EnableHeadersVisualStyles = false;
        _metricsGrid.GridColor = isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(60, 75, 80);

        if (_metricsGrid.Columns["TrayValueScheme"] is DataGridViewComboBoxColumn traySchemeColumn) {
            traySchemeColumn.FlatStyle = FlatStyle.Popup;
            traySchemeColumn.DefaultCellStyle.BackColor = _metricsGrid.DefaultCellStyle.BackColor;
            traySchemeColumn.DefaultCellStyle.ForeColor = _metricsGrid.DefaultCellStyle.ForeColor;
            traySchemeColumn.DefaultCellStyle.SelectionBackColor = _metricsGrid.DefaultCellStyle.SelectionBackColor;
            traySchemeColumn.DefaultCellStyle.SelectionForeColor = _metricsGrid.DefaultCellStyle.SelectionForeColor;
        }

        ApplyThemeRecursive(this, isLight);
    }

    /// <summary>
    /// Applies the theme recursive for metric settings editor form.
    /// </summary>
    /// <param name="parent">Input value for parent.</param>
    /// <param name="isLight">Input value for is light.</param>
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

    /// <summary>
    /// Creates the safe font for metric settings editor form.
    /// </summary>
    /// <param name="familyName">Input value for family name.</param>
    /// <param name="emSize">Input value for em size.</param>
    /// <param name="style">Input value for style.</param>
    /// <returns>The result of the operation.</returns>
    private static Font CreateSafeFont(string familyName, float emSize, FontStyle style = FontStyle.Regular) {
        try {
            var font = new Font(familyName, emSize, style);
            _ = font.GetHeight();
            return font;
        } catch {
            try {
                return new Font("Segoe UI", emSize, style);
            } catch {
                return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
            }
        }
    }
}
