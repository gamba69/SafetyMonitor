using MaterialSkin;
using SafetyMonitorView.Controls;
using SafetyMonitorView.Models;
using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

public class ChartPeriodsEditorForm : ThemedCaptionForm {
    private readonly List<ChartPeriodPresetDefinition> _presets;
    private readonly int _autoAggregationTargetPointCount;
    private readonly int _rawDataPointIntervalSeconds;

    private DataGridView _grid = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Button _addButton = null!;
    private Button _removeButton = null!;
    private Button _calculateButton = null!;
    private Label _headerLabel = null!;
    private Label _targetPointsLabel = null!;

    private readonly List<ChartPeriodUnit> _units = [.. Enum.GetValues<ChartPeriodUnit>()];
    private static readonly (string Bucket, TimeSpan Interval)[] BucketDefinitions = [
        ("raw", TimeSpan.Zero),
        ("10s", TimeSpan.FromSeconds(10)),
        ("30s", TimeSpan.FromSeconds(30)),
        ("1m", TimeSpan.FromMinutes(1)),
        ("5m", TimeSpan.FromMinutes(5)),
        ("15m", TimeSpan.FromMinutes(15)),
        ("1h", TimeSpan.FromHours(1)),
        ("4h", TimeSpan.FromHours(4)),
        ("12h", TimeSpan.FromHours(12)),
        ("1d", TimeSpan.FromDays(1)),
        ("3d", TimeSpan.FromDays(3)),
        ("1w", TimeSpan.FromDays(7))
    ];
    private static readonly string[] Buckets = [.. BucketDefinitions.Select(x => x.Bucket)];

    public ChartPeriodsEditorForm(IEnumerable<ChartPeriodPresetDefinition> presets, int autoAggregationTargetPointCount, int rawDataPointIntervalSeconds) {
        _presets = [.. presets.Select(p => new ChartPeriodPresetDefinition {
            Uid = p.Uid,
            Name = p.Name,
            Value = p.Value,
            Unit = p.Unit,
            AggregationInterval = p.AggregationInterval
        })];
        _autoAggregationTargetPointCount = Math.Max(2, autoAggregationTargetPointCount);
        _rawDataPointIntervalSeconds = Math.Max(1, rawDataPointIntervalSeconds);

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.MenuViewChartPeriods);
        ApplyTheme();
        LoadPresets();
    }

    public List<ChartPeriodPresetDefinition> Presets { get; private set; } = [];

    private void InitializeComponent() {
        Text = "Chart Period Presets";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;

        var titleFont = CreateSafeFont("Segoe UI", 9.5f, FontStyle.Bold);
        var normal = CreateSafeFont("Segoe UI", 9f, FontStyle.Regular);

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 4,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerPanel.Cursor = Cursors.Hand;

        _headerLabel = new Label {
            Text = "Configure chart period presets used by chart tiles. Each row defines a named period and the aggregation bucket that should be applied.",
            Font = titleFont,
            AutoSize = true,
            MaximumSize = new Size(810, 0),
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        headerPanel.Controls.Add(_headerLabel, 0, 0);

        var detailsToggle = new PictureBox {
            Size = new Size(22, 22),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Cursor = Cursors.Hand,
            Margin = new Padding(0),
            Dock = DockStyle.Top
        };
        headerPanel.Controls.Add(detailsToggle, 1, 0);

        _targetPointsLabel = new Label {
            Dock = DockStyle.Fill,
            AutoSize = true,
            MaximumSize = new Size(810, 0),
            Font = normal,
            Margin = new Padding(0, 0, 0, 8),
            Text = $"Auto aggregation target: {_autoAggregationTargetPointCount} point(s)."
        };
        headerPanel.Controls.Add(_targetPointsLabel, 0, 1);
        headerPanel.SetColumnSpan(_targetPointsLabel, 2);

        var bulletPanel = new TableLayoutPanel {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0)
        };
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        bulletPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        bulletPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bulletPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        bulletPanel.Controls.Add(new Label {
            Text = "• Preset: display name shown in the chart period selector.",
            Font = normal,
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            Margin = new Padding(0, 0, 12, 2)
        }, 0, 0);
        bulletPanel.Controls.Add(new Label {
            Text = "• Value + Unit: duration of the period (for example 6 Hours).",
            Font = normal,
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            Margin = new Padding(0, 0, 0, 2)
        }, 1, 0);
        bulletPanel.Controls.Add(new Label {
            Text = "• Aggregation: bucket size used for chart data grouping.",
            Font = normal,
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            Margin = new Padding(0, 0, 12, 0)
        }, 0, 1);
        bulletPanel.Controls.Add(new Label {
            Text = "• Points: expected number of points for the selected aggregation.",
            Font = normal,
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            Margin = new Padding(0)
        }, 1, 1);

        headerPanel.Controls.Add(bulletPanel, 0, 2);
        headerPanel.SetColumnSpan(bulletPanel, 2);

        var detailsExpanded = false;
        void UpdateDetailsToggle() {
            bulletPanel.Visible = detailsExpanded;
            detailsToggle.Image?.Dispose();
            detailsToggle.Image = MaterialIcons.GetIcon(detailsExpanded ? "keyboard_double_arrow_up" : "keyboard_double_arrow_down", _headerLabel.ForeColor, 20);
        }

        void ToggleDetails() {
            detailsExpanded = !detailsExpanded;
            UpdateDetailsToggle();
        }

        detailsToggle.Click += (_, _) => ToggleDetails();
        _headerLabel.Click += (_, _) => ToggleDetails();
        headerPanel.MouseClick += (_, e) => {
            if (e.Y <= _headerLabel.Bottom) {
                ToggleDetails();
            }
        };
        _headerLabel.ForeColorChanged += (_, _) => UpdateDetailsToggle();
        UpdateDetailsToggle();

        layout.Controls.Add(headerPanel, 0, 0);

        _grid = new DataGridView {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = normal
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Uid", Visible = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Preset", FillWeight = 34 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Unit", HeaderText = "Unit", FillWeight = 16, DataSource = _units });
        _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Aggregation", HeaderText = "Aggregation", FillWeight = 28, DataSource = Buckets });
        _grid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Points",
            HeaderText = "Points",
            FillWeight = 16,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle {
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });
        _grid.EditingControlShowing += Grid_EditingControlShowing;
        _grid.CellValueChanged += Grid_CellValueChanged;
        _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
        _grid.RowValidating += (_, _) => _grid.EndEdit();
        _grid.DataError += (_, e) => e.ThrowException = false;

        layout.Controls.Add(_grid, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        _addButton = new Button { Text = "Add", Width = 100, Height = 32 };
        _removeButton = new Button { Text = "Delete", Width = 100, Height = 32 };
        _calculateButton = new Button { Text = "Auto...", Width = 120, Height = 32 };
        _addButton.Click += (_, _) => {
            var rowIndex = _grid.Rows.Add(Guid.NewGuid().ToString("N"), "Custom", 1, ChartPeriodUnit.Hours, "1m", string.Empty);
            if (rowIndex >= 0 && rowIndex < _grid.Rows.Count) {
                UpdateRowAggregationOptions(_grid.Rows[rowIndex], showWarningIfAdjusted: false);
            }
        };
        _removeButton.Click += (_, _) => {
            foreach (DataGridViewRow row in _grid.SelectedRows) {
                if (!row.IsNewRow) _grid.Rows.Remove(row);
            }
        };
        _calculateButton.Click += (_, _) => RecalculateAllWithConfirmation();
        buttons.Controls.AddRange([_addButton, _removeButton, _calculateButton]);
        layout.Controls.Add(buttons, 0, 2);

        var action = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        _saveButton = new Button { Text = "Save", Width = 110, Height = 35 };
        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 35 };
        _saveButton.Click += SaveButton_Click;
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        action.Controls.AddRange([_cancelButton, _saveButton]);
        layout.Controls.Add(action, 0, 3);

        Controls.Add(layout);
        ClientSize = new Size(860, 520);
    }

    private void ApplyTheme() {
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        BackColor = isLight ? Color.FromArgb(250, 250, 250) : Color.FromArgb(38, 52, 57);
        ForeColor = isLight ? Color.Black : Color.White;

        _grid.BackgroundColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _grid.DefaultCellStyle.BackColor = _grid.BackgroundColor;
        _grid.DefaultCellStyle.ForeColor = ForeColor;
        _grid.DefaultCellStyle.SelectionBackColor = isLight ? Color.FromArgb(225, 245, 254) : Color.FromArgb(56, 78, 84);
        _grid.DefaultCellStyle.SelectionForeColor = ForeColor;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = isLight ? Color.FromArgb(238, 238, 238) : Color.FromArgb(55, 71, 79);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = ForeColor;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _grid.ColumnHeadersDefaultCellStyle.BackColor;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _grid.ColumnHeadersDefaultCellStyle.ForeColor;
        _grid.EnableHeadersVisualStyles = false;
        _grid.GridColor = isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(60, 75, 80);

        ApplyComboColumnTheme("Unit");
        ApplyComboColumnTheme("Aggregation");

        ApplyThemeRecursive(this, isLight);
    }

    private void ApplyComboColumnTheme(string columnName) {
        if (_grid.Columns[columnName] is not DataGridViewComboBoxColumn comboColumn) {
            return;
        }

        comboColumn.FlatStyle = FlatStyle.Popup;
        comboColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
        comboColumn.DefaultCellStyle.BackColor = _grid.DefaultCellStyle.BackColor;
        comboColumn.DefaultCellStyle.ForeColor = _grid.DefaultCellStyle.ForeColor;
        comboColumn.DefaultCellStyle.SelectionBackColor = _grid.DefaultCellStyle.SelectionBackColor;
        comboColumn.DefaultCellStyle.SelectionForeColor = _grid.DefaultCellStyle.SelectionForeColor;
    }

    private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (_grid.CurrentCell?.OwningColumn is not DataGridViewComboBoxColumn || e.Control is not ComboBox comboBox) {
            return;
        }

        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ThemedComboBoxStyler.Apply(comboBox, isLight);
    }

    private static void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);
            if (control is Button button) {
                ThemedButtonStyler.Apply(button, isLight);
            }

            ApplyThemeRecursive(control, isLight);
        }
    }

    private void LoadPresets() {
        _grid.Rows.Clear();
        foreach (var p in _presets) {
            var rowIndex = _grid.Rows.Add(p.Uid, p.Name, p.Value, p.Unit, BucketFromInterval(p.AggregationInterval), string.Empty);
            if (rowIndex >= 0 && rowIndex < _grid.Rows.Count) {
                UpdateRowAggregationOptions(_grid.Rows[rowIndex], showWarningIfAdjusted: false);
            }
        }
    }

    private void RecalculateAll() {
        foreach (DataGridViewRow row in _grid.Rows) {
            if (row.IsNewRow) continue;
            if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var value) || value <= 0) continue;
            var unit = GetUnitValue(row);
            var duration = ChartAggregationHelper.BuildPeriodDuration(value, unit);
            row.Cells["Aggregation"].Value = GetOptimalBucket(duration);
            UpdateRowAggregationOptions(row, showWarningIfAdjusted: false);
        }
    }

    private void RecalculateAllWithConfirmation() {
        var decision = ThemedMessageBox.Show(
            this,
            "All aggregations will be recalculated.",
            "Recalculate aggregations",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);

        if (decision != DialogResult.OK) {
            return;
        }

        RecalculateAll();
    }

    private void SaveButton_Click(object? sender, EventArgs e) {
        var output = new List<ChartPeriodPresetDefinition>();
        var adjustedPresetNames = new List<string>();
        foreach (DataGridViewRow row in _grid.Rows) {
            if (row.IsNewRow) continue;
            var uid = row.Cells["Uid"].Value?.ToString();
            var name = row.Cells["Name"].Value?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) {
                ThemedMessageBox.Show(this, "Preset name is required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var value) || value <= 0) {
                ThemedMessageBox.Show(this, "Preset value must be positive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var unit = GetUnitValue(row);
            var duration = ChartAggregationHelper.BuildPeriodDuration(value, unit);
            var bucket = row.Cells["Aggregation"].Value?.ToString() ?? "1m";
            if (!IsBucketWithinAllowedRange(bucket, duration)) {
                bucket = GetOptimalBucket(duration);
                row.Cells["Aggregation"].Value = bucket;
                adjustedPresetNames.Add(name);
            }

            UpdateRowAggregationOptions(row, showWarningIfAdjusted: false);

            output.Add(new ChartPeriodPresetDefinition {
                Uid = string.IsNullOrWhiteSpace(uid) ? Guid.NewGuid().ToString("N") : uid,
                Name = name,
                Value = value,
                Unit = unit,
                AggregationInterval = IntervalFromBucket(bucket)
            });
        }

        if (adjustedPresetNames.Count > 0) {
            var affected = string.Join(", ", adjustedPresetNames.Distinct(StringComparer.OrdinalIgnoreCase));
            ThemedMessageBox.Show(
                this,
                $"Aggregation was changed to the optimal value for preset(s): {affected}.\nAllowed range is from optimal -1 to optimal +2 bucket(s).",
                "Aggregation adjusted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        Presets = output;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e) {
        if (_grid.IsCurrentCellDirty) {
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e) {
        if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) {
            return;
        }

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (!string.Equals(columnName, "Value", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(columnName, "Unit", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(columnName, "Aggregation", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        var row = _grid.Rows[e.RowIndex];
        UpdateRowAggregationOptions(row, showWarningIfAdjusted: columnName == "Aggregation");
        UpdateRowPoints(row);
    }

    private void UpdateRowAggregationOptions(DataGridViewRow row, bool showWarningIfAdjusted) {
        if (row.IsNewRow || row.Cells["Aggregation"] is not DataGridViewComboBoxCell aggregationCell) {
            return;
        }

        if (!TryBuildDuration(row, out var duration)) {
            aggregationCell.DataSource = Buckets;
            UpdateRowPoints(row);
            return;
        }

        var allowedBuckets = GetAllowedBuckets(duration);
        aggregationCell.DataSource = allowedBuckets;

        var currentBucket = row.Cells["Aggregation"].Value?.ToString() ?? string.Empty;
        if (allowedBuckets.Contains(currentBucket, StringComparer.OrdinalIgnoreCase)) {
            UpdateRowPoints(row);
            return;
        }

        var optimalBucket = GetOptimalBucket(duration);
        row.Cells["Aggregation"].Value = optimalBucket;

        if (showWarningIfAdjusted) {
            ThemedMessageBox.Show(
                this,
                "Selected aggregation is outside the allowed range (optimal -1 to optimal +2). Value was reset to the optimal aggregation.",
                "Aggregation adjusted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        UpdateRowPoints(row);
    }

    private void UpdateRowPoints(DataGridViewRow row) {
        if (row.IsNewRow || row.Cells["Points"] is not DataGridViewTextBoxCell pointsCell) {
            return;
        }

        if (!TryBuildDuration(row, out var duration)) {
            pointsCell.Value = string.Empty;
            return;
        }

        var bucket = row.Cells["Aggregation"].Value?.ToString() ?? string.Empty;
        var interval = IntervalFromBucket(bucket);
        var effectiveInterval = interval > TimeSpan.Zero ? interval : TimeSpan.FromSeconds(_rawDataPointIntervalSeconds);
        var points = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds / effectiveInterval.TotalSeconds));
        pointsCell.Value = points;
    }

    private bool TryBuildDuration(DataGridViewRow row, out TimeSpan duration) {
        duration = TimeSpan.Zero;
        if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var value) || value <= 0) {
            return false;
        }

        var unit = GetUnitValue(row);
        duration = ChartAggregationHelper.BuildPeriodDuration(value, unit);
        return duration > TimeSpan.Zero;
    }

    private static ChartPeriodUnit GetUnitValue(DataGridViewRow row) {
        var rawValue = row.Cells["Unit"].Value;
        if (rawValue is ChartPeriodUnit unit) {
            return unit;
        }

        if (rawValue is string unitText
            && Enum.TryParse<ChartPeriodUnit>(unitText, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)) {
            return parsed;
        }

        return ChartPeriodUnit.Hours;
    }

    private bool IsBucketWithinAllowedRange(string bucket, TimeSpan duration) {
        if (string.IsNullOrWhiteSpace(bucket)) {
            return false;
        }

        return GetAllowedBuckets(duration).Contains(bucket, StringComparer.OrdinalIgnoreCase);
    }

    private string[] GetAllowedBuckets(TimeSpan duration) {
        var optimalIndex = GetOptimalBucketIndex(duration);
        var minIndex = Math.Max(0, optimalIndex - 1);
        var maxIndex = Math.Min(BucketDefinitions.Length - 1, optimalIndex + 2);
        return BucketDefinitions[minIndex..(maxIndex + 1)].Select(x => x.Bucket).ToArray();
    }

    private string GetOptimalBucket(TimeSpan duration) => BucketDefinitions[GetOptimalBucketIndex(duration)].Bucket;

    private int GetOptimalBucketIndex(TimeSpan duration) {
        var target = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds / _autoAggregationTargetPointCount));
        var targetInterval = TimeSpan.FromSeconds(target);
        var optimalBucket = BucketFromInterval(targetInterval);
        var foundIndex = Array.FindIndex(BucketDefinitions, x => string.Equals(x.Bucket, optimalBucket, StringComparison.OrdinalIgnoreCase));
        return foundIndex >= 0 ? foundIndex : 0;
    }

    private static Font CreateSafeFont(string familyName, float emSize, FontStyle style = FontStyle.Regular) {
        try {
            return new Font(familyName, emSize, style);
        } catch {
            return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
        }
    }

    private string BucketFromInterval(TimeSpan interval) {
        if (interval <= TimeSpan.FromSeconds(_rawDataPointIntervalSeconds)) return "raw";
        if (interval <= TimeSpan.FromSeconds(10)) return "10s";
        if (interval <= TimeSpan.FromSeconds(30)) return "30s";
        if (interval <= TimeSpan.FromMinutes(1)) return "1m";
        if (interval <= TimeSpan.FromMinutes(5)) return "5m";
        if (interval <= TimeSpan.FromMinutes(15)) return "15m";
        if (interval <= TimeSpan.FromHours(1)) return "1h";
        if (interval <= TimeSpan.FromHours(4)) return "4h";
        if (interval <= TimeSpan.FromHours(12)) return "12h";
        if (interval <= TimeSpan.FromDays(1)) return "1d";
        if (interval <= TimeSpan.FromDays(3)) return "3d";
        return "1w";
    }

    private static TimeSpan IntervalFromBucket(string bucket) => bucket.ToLowerInvariant() switch {
        "raw" => TimeSpan.Zero,
        "10s" => TimeSpan.FromSeconds(10),
        "30s" => TimeSpan.FromSeconds(30),
        "1m" => TimeSpan.FromMinutes(1),
        "5m" => TimeSpan.FromMinutes(5),
        "15m" => TimeSpan.FromMinutes(15),
        "1h" => TimeSpan.FromHours(1),
        "4h" => TimeSpan.FromHours(4),
        "12h" => TimeSpan.FromHours(12),
        "1d" => TimeSpan.FromDays(1),
        "3d" => TimeSpan.FromDays(3),
        "1w" => TimeSpan.FromDays(7),
        _ => TimeSpan.FromMinutes(1)
    };
}
