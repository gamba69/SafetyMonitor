using MaterialSkin;
using SafetyMonitor.Controls;
using SafetyMonitor.Models;
using SafetyMonitor.Services;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents chart periods editor form and encapsulates its related behavior and state.
/// </summary>
public class ChartPeriodsEditorForm : ThemedCaptionForm {
    private readonly List<ChartPeriodPresetDefinition> _presets;
    private readonly int _autoAggregationTargetPointCount;
    private readonly int _rawDataPointIntervalSeconds;

    private DataGridView _grid = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Button _addButton = null!;
    private Button _removeButton = null!;
    private Button _moveUpButton = null!;
    private Button _moveDownButton = null!;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPeriodsEditorForm"/> class.
    /// </summary>
    /// <param name="presets">Collection of presets items used by the operation.</param>
    /// <param name="autoAggregationTargetPointCount">Input value for auto aggregation target point count.</param>
    /// <param name="rawDataPointIntervalSeconds">Input value for raw data point interval seconds.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public ChartPeriodsEditorForm(IEnumerable<ChartPeriodPresetDefinition> presets, int autoAggregationTargetPointCount, int rawDataPointIntervalSeconds) {
        _presets = [.. presets.Select(p => new ChartPeriodPresetDefinition {
            Uid = p.Uid,
            Name = p.Name,
            ShortName = p.ShortName,
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

    /// <summary>
    /// Gets or sets the presets for chart periods editor form. Contains a collection of values that drive configuration, rendering, or data processing.
    /// </summary>
    public List<ChartPeriodPresetDefinition> Presets { get; private set; } = [];

    /// <summary>
    /// Initializes chart periods editor form state and required resources.
    /// </summary>
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

        const int visualDetailsColumnCount = 2;
        const int detailsColumnCount = visualDetailsColumnCount * 2;
        var details = new[] {
            "Preset: display name shown in the chart period selector.",
            "Short: editable short period name stored with the preset.",
            "Value + Unit: duration of the period (for example 6 Hours).",
            "Aggregation: bucket size used for chart data grouping.",
            "Points: expected number of points for the selected aggregation."
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
                Font = normal,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            }, bulletColumn, row);
            bulletPanel.Controls.Add(new Label {
                Text = details[index],
                Font = normal,
                AutoSize = true,
                MaximumSize = new Size(detailColumnWidth, 0),
                Margin = visualColumn == 0 ? new Padding(0, 0, 12, 2) : new Padding(0, 0, 0, 2)
            }, textColumn, row);
        }

        headerPanel.Controls.Add(bulletPanel, 0, 2);
        headerPanel.SetColumnSpan(bulletPanel, 2);

        var detailsExpanded = false;
        void UpdateDetailsToggle() {
            bulletPanel.Visible = detailsExpanded;
            detailsToggle.Image?.Dispose();
            detailsToggle.Image = MaterialIcons.GetIcon(detailsExpanded ? "keyboard_double_arrow_up" : "keyboard_double_arrow_down", _headerLabel.ForeColor, 20, IconRenderPreset.DarkOutlined);
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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Preset", FillWeight = 28 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ShortName", HeaderText = "Short", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Unit", HeaderText = "Unit", FillWeight = 14, DataSource = _units });
        _grid.Columns.Add(new DataGridViewComboBoxColumn { Name = "Aggregation", HeaderText = "Aggregation", FillWeight = 24, DataSource = Buckets });
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
        _moveUpButton = new Button { Text = "Up", Width = 100, Height = 32 };
        _moveDownButton = new Button { Text = "Down", Width = 100, Height = 32 };
        _calculateButton = new Button { Text = "Auto…", Width = 120, Height = 32 };
        _addButton.Click += (_, _) => {
            var rowIndex = _grid.Rows.Add(Guid.NewGuid().ToString("N"), "Custom", "custom", 1, ChartPeriodUnit.Hours, "1m", string.Empty);
            if (rowIndex >= 0 && rowIndex < _grid.Rows.Count) {
                UpdateRowAggregationOptions(_grid.Rows[rowIndex], showWarningIfAdjusted: false);
            }
        };
        _removeButton.Click += (_, _) => {
            foreach (DataGridViewRow row in _grid.SelectedRows) {
                if (!row.IsNewRow) _grid.Rows.Remove(row);
            }
        };
        _moveUpButton.Click += (_, _) => MoveSelectedRow(-1);
        _moveDownButton.Click += (_, _) => MoveSelectedRow(1);
        _calculateButton.Click += (_, _) => RecalculateAllWithConfirmation();
        buttons.Controls.AddRange([_addButton, _removeButton, _moveUpButton, _moveDownButton, _calculateButton]);
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

    /// <summary>
    /// Applies the theme for chart periods editor form.
    /// </summary>
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

    /// <summary>
    /// Applies the combo column theme for chart periods editor form.
    /// </summary>
    /// <param name="columnName">Input value for column name.</param>
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

    /// <summary>
    /// Executes grid editing control showing as part of chart periods editor form processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e) {
        if (_grid.CurrentCell?.OwningColumn is not DataGridViewComboBoxColumn || e.Control is not ComboBox comboBox) {
            return;
        }

        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Font = CreateSafeFont(comboBox.Font.FontFamily.Name, comboBox.Font.Size, comboBox.Font.Style);
        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        ThemedComboBoxStyler.Apply(comboBox, isLight);
    }

    /// <summary>
    /// Applies the theme recursive for chart periods editor form.
    /// </summary>
    /// <param name="parent">Input value for parent.</param>
    /// <param name="isLight">Input value for is light.</param>
    private static void ApplyThemeRecursive(Control parent, bool isLight) {
        foreach (Control control in parent.Controls) {
            InteractiveCursorStyler.Apply(control);
            if (control is Button button) {
                ThemedButtonStyler.Apply(button, isLight);
            }

            ApplyThemeRecursive(control, isLight);
        }
    }

    /// <summary>
    /// Loads the presets for chart periods editor form.
    /// </summary>
    private void LoadPresets() {
        _grid.Rows.Clear();
        foreach (var p in _presets) {
            var rowIndex = _grid.Rows.Add(p.Uid, p.Name, p.ShortName, p.Value, p.Unit, BucketFromInterval(p.AggregationInterval), string.Empty);
            if (rowIndex >= 0 && rowIndex < _grid.Rows.Count) {
                UpdateRowAggregationOptions(_grid.Rows[rowIndex], showWarningIfAdjusted: false);
            }
        }
    }

    /// <summary>
    /// Executes recalculate all as part of chart periods editor form processing.
    /// </summary>
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

    /// <summary>
    /// Executes move selected row as part of chart periods editor form processing.
    /// </summary>
    /// <param name="direction">Input value for direction.</param>
    private void MoveSelectedRow(int direction) {
        if (direction == 0 || _grid.CurrentRow is null || _grid.CurrentRow.IsNewRow) {
            return;
        }

        _grid.EndEdit();

        var sourceIndex = _grid.CurrentRow.Index;
        var targetIndex = sourceIndex + direction;
        if (targetIndex < 0 || targetIndex >= _grid.Rows.Count) {
            return;
        }

        SwapRowValues(_grid.Rows[sourceIndex], _grid.Rows[targetIndex]);
        UpdateRowAggregationOptions(_grid.Rows[sourceIndex], showWarningIfAdjusted: false);
        UpdateRowAggregationOptions(_grid.Rows[targetIndex], showWarningIfAdjusted: false);
        UpdateRowPoints(_grid.Rows[sourceIndex]);
        UpdateRowPoints(_grid.Rows[targetIndex]);

        _grid.ClearSelection();
        _grid.Rows[targetIndex].Selected = true;
        _grid.CurrentCell = _grid.Rows[targetIndex].Cells["Name"];
    }

    /// <summary>
    /// Executes swap row values as part of chart periods editor form processing.
    /// </summary>
    /// <param name="firstRow">Input value for first row.</param>
    /// <param name="secondRow">Input value for second row.</param>
    private static void SwapRowValues(DataGridViewRow firstRow, DataGridViewRow secondRow) {
        var firstValues = firstRow.Cells.Cast<DataGridViewCell>().Select(cell => cell.Value).ToArray();
        var secondValues = secondRow.Cells.Cast<DataGridViewCell>().Select(cell => cell.Value).ToArray();

        for (var columnIndex = 0; columnIndex < firstRow.Cells.Count; columnIndex++) {
            firstRow.Cells[columnIndex].Value = secondValues[columnIndex];
            secondRow.Cells[columnIndex].Value = firstValues[columnIndex];
        }
    }

    /// <summary>
    /// Executes recalculate all with confirmation as part of chart periods editor form processing.
    /// </summary>
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

    /// <summary>
    /// Saves the button click for chart periods editor form.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
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

            var shortName = row.Cells["ShortName"].Value?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(shortName)) {
                shortName = name;
                row.Cells["ShortName"].Value = shortName;
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
                ShortName = shortName,
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

    /// <summary>
    /// Executes grid current cell dirty state changed as part of chart periods editor form processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
    private void Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e) {
        if (_grid.IsCurrentCellDirty) {
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    /// <summary>
    /// Executes grid cell value changed as part of chart periods editor form processing.
    /// </summary>
    /// <param name="sender">Input value for sender.</param>
    /// <param name="e">Input value for e.</param>
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
        if (string.Equals(columnName, "Unit", StringComparison.OrdinalIgnoreCase)) {
            NormalizeUnitCellValue(row);
        }

        UpdateRowAggregationOptions(row, showWarningIfAdjusted: columnName == "Aggregation");
        UpdateRowPoints(row);
    }

    /// <summary>
    /// Normalizes the unit cell value for chart periods editor form.
    /// </summary>
    /// <param name="row">Input value for row.</param>
    private static void NormalizeUnitCellValue(DataGridViewRow row) {
        if (row.Cells["Unit"] is not DataGridViewComboBoxCell unitCell) {
            return;
        }

        if (TryParseUnit(unitCell.Value, out var unit)) {
            unitCell.Value = unit;
            return;
        }

        if (TryParseUnit(unitCell.FormattedValue, out unit)) {
            unitCell.Value = unit;
        }
    }

    /// <summary>
    /// Updates the row aggregation options for chart periods editor form.
    /// </summary>
    /// <param name="row">Input value for row.</param>
    /// <param name="showWarningIfAdjusted">Input value for show warning if adjusted.</param>
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

    /// <summary>
    /// Updates the row points for chart periods editor form.
    /// </summary>
    /// <param name="row">Input value for row.</param>
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

    /// <summary>
    /// Attempts to build duration for chart periods editor form.
    /// </summary>
    /// <param name="row">Input value for row.</param>
    /// <param name="duration">Input value for duration.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private bool TryBuildDuration(DataGridViewRow row, out TimeSpan duration) {
        duration = TimeSpan.Zero;
        if (!double.TryParse(row.Cells["Value"].Value?.ToString(), out var value) || value <= 0) {
            return false;
        }

        var unit = GetUnitValue(row);
        duration = ChartAggregationHelper.BuildPeriodDuration(value, unit);
        return duration > TimeSpan.Zero;
    }

    /// <summary>
    /// Gets the unit value for chart periods editor form.
    /// </summary>
    /// <param name="row">Input value for row.</param>
    /// <returns>The result of the operation.</returns>
    private static ChartPeriodUnit GetUnitValue(DataGridViewRow row) {
        if (TryParseUnit(row.Cells["Unit"].Value, out var unit)) {
            return unit;
        }

        if (TryParseUnit(row.Cells["Unit"].FormattedValue, out unit)) {
            return unit;
        }

        return ChartPeriodUnit.Hours;
    }

    /// <summary>
    /// Attempts to parse unit for chart periods editor form.
    /// </summary>
    /// <param name="rawValue">Input value for raw value.</param>
    /// <param name="unit">Input value for unit.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool TryParseUnit(object? rawValue, out ChartPeriodUnit unit) {
        if (rawValue is ChartPeriodUnit enumValue) {
            unit = enumValue;
            return true;
        }

        if (rawValue is string text
            && Enum.TryParse<ChartPeriodUnit>(text, ignoreCase: true, out var parsedFromText)
            && Enum.IsDefined(parsedFromText)) {
            unit = parsedFromText;
            return true;
        }

        if (rawValue is int intValue && Enum.IsDefined(typeof(ChartPeriodUnit), intValue)) {
            unit = (ChartPeriodUnit)intValue;
            return true;
        }

        unit = default;
        return false;
    }

    /// <summary>
    /// Determines whether is bucket within allowed range for chart periods editor form.
    /// </summary>
    /// <param name="bucket">Input value for bucket.</param>
    /// <param name="duration">Input value for duration.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private bool IsBucketWithinAllowedRange(string bucket, TimeSpan duration) {
        if (string.IsNullOrWhiteSpace(bucket)) {
            return false;
        }

        return GetAllowedBuckets(duration).Contains(bucket, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the allowed buckets for chart periods editor form.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <returns>The result of the operation.</returns>
    private string[] GetAllowedBuckets(TimeSpan duration) {
        var optimalIndex = GetOptimalBucketIndex(duration);
        var minIndex = Math.Max(0, optimalIndex - 1);
        var maxIndex = Math.Min(BucketDefinitions.Length - 1, optimalIndex + 2);
        return BucketDefinitions[minIndex..(maxIndex + 1)].Select(x => x.Bucket).ToArray();
    }

    private string GetOptimalBucket(TimeSpan duration) => BucketDefinitions[GetOptimalBucketIndex(duration)].Bucket;

    /// <summary>
    /// Gets the optimal bucket index for chart periods editor form.
    /// </summary>
    /// <param name="duration">Input value for duration.</param>
    /// <returns>The result of the operation.</returns>
    private int GetOptimalBucketIndex(TimeSpan duration) {
        var target = Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds / _autoAggregationTargetPointCount));
        var targetInterval = TimeSpan.FromSeconds(target);
        var optimalBucket = BucketFromInterval(targetInterval);
        var foundIndex = Array.FindIndex(BucketDefinitions, x => string.Equals(x.Bucket, optimalBucket, StringComparison.OrdinalIgnoreCase));
        return foundIndex >= 0 ? foundIndex : 0;
    }

    /// <summary>
    /// Creates the safe font for chart periods editor form.
    /// </summary>
    /// <param name="familyName">Input value for family name.</param>
    /// <param name="emSize">Input value for em size.</param>
    /// <param name="style">Input value for style.</param>
    /// <returns>The result of the operation.</returns>
    private static Font CreateSafeFont(string familyName, float emSize, FontStyle style = FontStyle.Regular) {
        try {
            return new Font(familyName, emSize, style);
        } catch {
            return new Font(SystemFonts.DefaultFont.FontFamily, emSize, style);
        }
    }

    /// <summary>
    /// Executes bucket from interval as part of chart periods editor form processing.
    /// </summary>
    /// <param name="interval">Input value for interval.</param>
    /// <returns>The resulting string value.</returns>
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

    /// <summary>
    /// Executes interval from bucket as part of chart periods editor form processing.
    /// </summary>
    /// <param name="bucket)">Input value for bucket.</param>
    /// <returns>The result of the operation.</returns>
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
