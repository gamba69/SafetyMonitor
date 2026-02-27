using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;
using System.Linq;

namespace SafetyMonitorView.Forms;

public class MetricSettingsEditorForm : ThemedCaptionForm {

    private Button _cancelButton = null!;
    private DataGridView _metricsGrid = null!;
    private Button _saveButton = null!;
    private readonly List<MetricDisplaySetting> _settings;
    private readonly List<string> _trayValueSchemeNames;

    public MetricSettingsEditorForm(IEnumerable<MetricDisplaySetting> settings) {
        _settings = [.. settings.Select(s => new MetricDisplaySetting {
            Metric = s.Metric,
            Decimals = s.Decimals,
            HideZeroes = s.HideZeroes,
            InvertY = s.InvertY,
            TrayName = s.TrayName,
            TrayValueSchemeName = s.TrayValueSchemeName
        })];
        _trayValueSchemeNames = ["(None)", .. new ValueSchemeService().LoadSchemes().Select(s => s.Name).Distinct().OrderBy(n => n)];

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewMetricSettings);
        ApplyTheme();
        LoadSettings();
    }

    public List<MetricDisplaySetting> Settings { get; private set; } = [];

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
        Font = normalFont;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerLabel = new Label {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
            MaximumSize = new Size(860, 0),
            Text = "Configure per-metric display settings. Metric list is fixed and cannot be changed.",
            Font = CreateSafeFont("Segoe UI", 10f, FontStyle.Bold)
        };
        headerPanel.Controls.Add(headerLabel, 0, 0);
        headerPanel.SetColumnSpan(headerLabel, 2);

        const int headerDescriptionMaxWidth = 400;

        headerPanel.Controls.Add(new Label {
            Text = "• Decimals: number of digits after decimal point (0..10).",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(headerDescriptionMaxWidth, 0),
            Margin = new Padding(0, 0, 12, 2)
        }, 0, 1);
        headerPanel.Controls.Add(new Label {
            Text = "• Hide zeroes: if enabled, zero values are shown as empty text instead of 0/0.0.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(headerDescriptionMaxWidth, 0),
            Margin = new Padding(0, 0, 0, 2)
        }, 1, 1);
        headerPanel.Controls.Add(new Label {
            Text = "• Invert Y: flips chart Y-axis direction for this metric.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(headerDescriptionMaxWidth, 0),
            Margin = new Padding(0, 0, 12, 0)
        }, 0, 2);
        headerPanel.Controls.Add(new Label {
            Text = "• Tray name: short alias shown in tray/compact displays.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(headerDescriptionMaxWidth, 0),
            Margin = new Padding(0)
        }, 1, 2);
        headerPanel.Controls.Add(new Label {
            Text = "• Tray scheme: optional value-scheme text mapping used in tray tooltip.",
            Font = normalFont,
            AutoSize = true,
            MaximumSize = new Size(860, 0),
            Margin = new Padding(0, 8, 0, 0)
        }, 0, 3);
        headerPanel.SetColumnSpan(headerPanel.Controls[^1], 2);

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

        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Metric", HeaderText = "Metric", FillWeight = 34, ReadOnly = true, MinimumWidth = 180 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Decimals", HeaderText = "Decimals", FillWeight = 12, MinimumWidth = 90 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "HideZeroes", HeaderText = "Hide zeroes", FillWeight = 16, MinimumWidth = 110 });
        _metricsGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "InvertY", HeaderText = "Invert Y", FillWeight = 13, MinimumWidth = 100 });
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrayName", HeaderText = "Tray name", FillWeight = 19, MinimumWidth = 120 });

        var traySchemeColumn = new DataGridViewComboBoxColumn {
            Name = "TrayValueScheme",
            HeaderText = "Tray scheme",
            FillWeight = 16,
            MinimumWidth = 150,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Popup
        };
        traySchemeColumn.Items.AddRange(_trayValueSchemeNames.Cast<object>().ToArray());
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

    private void LoadSettings() {
        _metricsGrid.Rows.Clear();
        var map = _settings.ToDictionary(s => s.Metric, s => s);
        foreach (var metric in GetOrderedMetricsForGrid(map)) {
            var s = map.TryGetValue(metric, out var found) ? found : new MetricDisplaySetting { Metric = metric };
            _metricsGrid.Rows.Add(metric.GetDisplayName(), Math.Max(0, s.Decimals), s.HideZeroes, s.InvertY, s.TrayName, NormalizeTrayValueScheme(s.TrayValueSchemeName));
        }
    }

    private static IEnumerable<MetricType> GetOrderedMetricsForGrid(IReadOnlyDictionary<MetricType, MetricDisplaySetting> settingsByMetric) {
        return Enum
            .GetValues<MetricType>()
            .OrderByDescending(metric => settingsByMetric.TryGetValue(metric, out var setting) && !string.IsNullOrWhiteSpace(setting.TrayName))
            .ThenBy(metric => (int)metric);
    }

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


    private void MetricsGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0 || _metricsGrid.Columns[e.ColumnIndex].Name != "TrayName") {
            return;
        }

        ReorderGridRowsByTrayName();
    }

    private void ReorderGridRowsByTrayName() {
        var metricNames = Enum.GetValues<MetricType>().ToDictionary(m => m.GetDisplayName(), m => m);
        var selectedMetric = _metricsGrid.CurrentRow?.Cells["Metric"].Value?.ToString();

        var rowStates = _metricsGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => new {
                Metric = metricNames.TryGetValue(row.Cells["Metric"].Value?.ToString() ?? string.Empty, out var metric) ? metric : (MetricType?)null,
                Decimals = row.Cells["Decimals"].Value,
                HideZeroes = row.Cells["HideZeroes"].Value is true,
                InvertY = row.Cells["InvertY"].Value is true,
                TrayName = row.Cells["TrayName"].Value?.ToString() ?? string.Empty,
                TrayScheme = NormalizeTrayValueScheme(row.Cells["TrayValueScheme"].Value?.ToString())
            })
            .Where(state => state.Metric.HasValue)
            .Select(state => new {
                Metric = state.Metric!.Value,
                state.Decimals,
                state.HideZeroes,
                state.InvertY,
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
                rowState.Decimals,
                rowState.HideZeroes,
                rowState.InvertY,
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
    }

    private void MetricsGrid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (_metricsGrid.CurrentCell?.OwningColumn?.Name != "TrayValueScheme" || e.Control is not ComboBox comboBox) {
            return;
        }

        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ThemedComboBoxStyler.Apply(comboBox, isLight);
    }

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
                TrayName = row.Cells["TrayName"].Value?.ToString() ?? string.Empty,
                TrayValueSchemeName = NormalizeTrayValueScheme(row.Cells["TrayValueScheme"].Value?.ToString())
            });
        }

        Settings = [.. newSettings.OrderBy(s => (int)s.Metric)];
        DialogResult = DialogResult.OK;
        Close();
    }

    private string NormalizeTrayValueScheme(string? schemeName) {
        if (string.IsNullOrWhiteSpace(schemeName) || schemeName == "(None)") {
            return string.Empty;
        }

        return _trayValueSchemeNames.Contains(schemeName) ? schemeName : string.Empty;
    }

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
