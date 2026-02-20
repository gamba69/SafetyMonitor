using MaterialSkin;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

public class MetricSettingsEditorForm : Form {

    private Button _cancelButton = null!;
    private DataGridView _metricsGrid = null!;
    private Button _saveButton = null!;
    private readonly List<MetricDisplaySetting> _settings;

    public MetricSettingsEditorForm(IEnumerable<MetricDisplaySetting> settings) {
        _settings = [.. settings.Select(s => new MetricDisplaySetting {
            Metric = s.Metric,
            Decimals = s.Decimals,
            HideZeroes = s.HideZeroes,
            InvertY = s.InvertY,
            TrayName = s.TrayName
        })];

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewMetricEditor);
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
        ClientSize = new Size(820, 470);

        var normalFont = CreateSafeFont("Segoe UI", 10f);
        Font = normalFont;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
            MaximumSize = new Size(780, 0),
            Text = "Configure per-metric display settings. Metric list is fixed and cannot be changed."
        }, 0, 0);

        _metricsGrid = new DataGridView {
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
        _metricsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TrayName", HeaderText = "Tray name", FillWeight = 25, MinimumWidth = 120 });

        foreach (DataGridViewColumn column in _metricsGrid.Columns) {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        _metricsGrid.CellValidating += MetricsGrid_CellValidating;
        root.Controls.Add(_metricsGrid, 0, 1);

        var buttonPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 12, 0, 0) };
        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35, Font = normalFont };
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _saveButton = new Button { Text = "Save", Width = 110, Height = 35, Font = CreateSafeFont("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
        _saveButton.Click += SaveButton_Click;
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_saveButton);
        root.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(root);
    }

    private void LoadSettings() {
        _metricsGrid.Rows.Clear();
        var map = _settings.ToDictionary(s => s.Metric, s => s);
        foreach (var metric in Enum.GetValues<MetricType>()) {
            var s = map.TryGetValue(metric, out var found) ? found : new MetricDisplaySetting { Metric = metric };
            _metricsGrid.Rows.Add(metric.GetDisplayName(), Math.Max(0, s.Decimals), s.HideZeroes, s.InvertY, s.TrayName);
        }
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
                TrayName = row.Cells["TrayName"].Value?.ToString() ?? string.Empty
            });
        }

        Settings = [.. newSettings.OrderBy(s => (int)s.Metric)];
        DialogResult = DialogResult.OK;
        Close();
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
