using MaterialSkin;
using SafetyMonitorView.Services;
using SafetyMonitorView.Models;

namespace SafetyMonitorView.Forms;

public class DashboardManagementForm : Form {

    #region Private Fields

    private readonly List<DashboardListItem> _items;
    private readonly Guid? _currentDashboardId;
    private Button _cancelButton = null!;
    private DataGridView _grid = null!;
    private Button _okButton = null!;
    private Button _moveDownButton = null!;
    private Button _moveUpButton = null!;
    private Button _deleteButton = null!;
    private Label _descriptionLabel = null!;
    private bool _isBindingGrid;

    #endregion Private Fields

    #region Public Constructors

    public DashboardManagementForm(IEnumerable<Dashboard> dashboards, Guid? currentDashboardId) {
        _currentDashboardId = currentDashboardId;
        _items = dashboards
            .OrderByDescending(d => d.IsQuickAccess)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new DashboardListItem {
                Id = d.Id,
                Name = d.Name,
                IsQuickAccess = d.IsQuickAccess
            })
            .ToList();

        InitializeComponent();
        FormIconHelper.Apply(this, MaterialIcons.DashboardManage);
        ApplyTheme();
        BindGrid();
    }

    #endregion Public Constructors

    #region Public Properties

    public IReadOnlyList<Guid> DeletedDashboardIds { get; private set; } = [];

    public IReadOnlyList<DashboardOrderUpdate> Updates {
        get {
            var result = new List<DashboardOrderUpdate>(_items.Count);
            int quickIndex = 0;
            int regularIndex = 0;
            foreach (var item in _items) {
                result.Add(new DashboardOrderUpdate(
                    item.Id,
                    item.Name,
                    item.IsQuickAccess,
                    item.IsQuickAccess ? quickIndex++ : regularIndex++));
            }

            return result;
        }
    }

    #endregion Public Properties

    #region Private Methods

    private void ApplyTheme() {
        var skinManager = MaterialSkinManager.Instance;
        bool isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;

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

        foreach (var button in new[] { _moveUpButton, _moveDownButton, _deleteButton, _cancelButton, _okButton }) {
            ThemedButtonStyler.Apply(button, isLight);
        }

        NormalizeActionButtonWidths();
    }

    private void BindGrid() {
        _isBindingGrid = true;
        try {
            _grid.Rows.Clear();
            foreach (var item in _items) {
                int rowIndex = _grid.Rows.Add(item.Name, item.IsQuickAccess);
                _grid.Rows[rowIndex].Tag = item.Id;
                if (_currentDashboardId.HasValue && _currentDashboardId.Value == item.Id) {
                    _grid.Rows[rowIndex].DefaultCellStyle.Font = new Font(_grid.Font, FontStyle.Bold);
                }
            }

            if (_grid.Rows.Count > 0) {
                _grid.CurrentCell ??= _grid.Rows[0].Cells[0];
            }
        }
        finally {
            _isBindingGrid = false;
        }
    }

    private void RebindGridAndSelect(Guid itemId) {
        BeginInvoke(() => {
            BindGrid();
            int newIndex = _items.FindIndex(i => i.Id == itemId);
            if (newIndex >= 0 && _grid.Rows.Count > newIndex) {
                _grid.CurrentCell = _grid.Rows[newIndex].Cells[0];
            }
        });
    }

    private static bool CanMoveItem(IReadOnlyList<DashboardListItem> items, int index, int direction) {
        int newIndex = index + direction;
        if (index < 0 || index >= items.Count || newIndex < 0 || newIndex >= items.Count) {
            return false;
        }

        return items[index].IsQuickAccess == items[newIndex].IsQuickAccess;
    }

    private void InitializeComponent() {
        var normalFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        var emphasizedFont = new Font("Segoe UI", 10f, FontStyle.Bold);

        Text = "Manage Dashboards";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        Font = normalFont;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(700, 480);
        Padding = new Padding(16);

        var root = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _descriptionLabel = new Label {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 12),
            MaximumSize = new Size(640, 0),
            Text = "Use this form to rename dashboards, mark favorites for quick access (up to 7), reorder items within each group, and remove dashboards you no longer need (at least one dashboard must remain)."
        };

        _grid = new DataGridView {
            Dock = DockStyle.Fill,
            Font = normalFont,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn {
            Name = "Name",
            HeaderText = "Dashboard",
            FillWeight = 75
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn {
            Name = "IsQuickAccess",
            HeaderText = "Favorite",
            FillWeight = 25
        });
        _grid.CellValueChanged += OnGridCellValueChanged;
        _grid.CellValidating += OnGridCellValidating;
        _grid.CurrentCellDirtyStateChanged += (s, e) => {
            if (_grid.IsCurrentCellDirty) {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        var controlsPanel = new TableLayoutPanel {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Dock = DockStyle.Top,
            Margin = new Padding(12, 0, 0, 0)
        };
        controlsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        controlsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _moveUpButton = new Button { Text = "Up", Width = 100, Height = 34 };
        _moveDownButton = new Button { Text = "Down", Width = 100, Height = 34 };
        _deleteButton = new Button { Text = "Delete", Width = 100, Height = 34 };

        _moveUpButton.Click += (s, e) => MoveSelectedItem(-1);
        _moveDownButton.Click += (s, e) => MoveSelectedItem(1);
        _deleteButton.Click += (s, e) => DeleteSelectedItem();

        NormalizeActionButtonWidths();

        controlsPanel.Controls.Add(_moveUpButton, 0, 0);
        controlsPanel.Controls.Add(_moveDownButton, 0, 1);
        controlsPanel.Controls.Add(_deleteButton, 0, 2);

        var bottomPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 0)
        };

        _cancelButton = new Button { Text = "Cancel", Width = 110, Height = 36, Font = normalFont, Margin = new Padding(0) };
        _okButton = new Button { Text = "Save", Width = 110, Height = 36, Font = emphasizedFont, Margin = new Padding(0, 0, 10, 0) };

        _cancelButton.Click += (s, e) => {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        _okButton.Click += (s, e) => {
            DialogResult = DialogResult.OK;
            Close();
        };

        bottomPanel.Controls.Add(_cancelButton);
        bottomPanel.Controls.Add(_okButton);

        root.Controls.Add(_descriptionLabel, 0, 0);
        root.SetColumnSpan(_descriptionLabel, 2);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(controlsPanel, 1, 1);
        root.Controls.Add(bottomPanel, 0, 2);
        root.SetColumnSpan(bottomPanel, 2);

        Controls.Add(root);
    }

    private void MoveSelectedItem(int direction) {
        if (_grid.CurrentCell == null) {
            return;
        }

        int index = _grid.CurrentCell.RowIndex;
        if (!CanMoveItem(_items, index, direction)) {
            return;
        }

        var item = _items[index];
        _items.RemoveAt(index);
        _items.Insert(index + direction, item);
        BindGrid();
        _grid.CurrentCell = _grid.Rows[index + direction].Cells[0];
    }

    private void NormalizeActionButtonWidths() {
        var actionButtons = new[] { _moveUpButton, _moveDownButton, _deleteButton };
        var maxWidth = actionButtons.Max(button => button.Width);

        foreach (var button in actionButtons) {
            button.Width = maxWidth;
            button.MinimumSize = new Size(maxWidth, button.Height);
        }
    }

    private void DeleteSelectedItem() {
        if (_grid.CurrentCell == null) {
            return;
        }

        if (_items.Count <= 1) {
            ThemedMessageBox.Show(this, "Cannot delete the last dashboard.", "Delete dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int index = _grid.CurrentCell.RowIndex;
        var item = _items[index];
        var result = ThemedMessageBox.Show(this, $"Delete dashboard '{item.Name}'?", "Delete dashboard", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) {
            return;
        }

        DeletedDashboardIds = [.. DeletedDashboardIds, item.Id];
        _items.RemoveAt(index);
        BindGrid();
    }

    private void OnGridCellValueChanged(object? sender, DataGridViewCellEventArgs e) {
        if (_isBindingGrid || e.RowIndex < 0 || e.ColumnIndex < 0) {
            return;
        }

        var columnName = _grid.Columns[e.ColumnIndex].Name;
        if (columnName == "Name") {
            var updatedName = Convert.ToString(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value)?.Trim();
            if (!string.IsNullOrWhiteSpace(updatedName)) {
                _items[e.RowIndex].Name = updatedName;
            }
            return;
        }

        if (columnName != "IsQuickAccess") {
            return;
        }

        bool isChecked = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
        int selectedQuickCount = _items.Count(i => i.IsQuickAccess);
        var target = _items[e.RowIndex];

        if (isChecked && !target.IsQuickAccess && selectedQuickCount >= 7) {
            RebindGridAndSelect(target.Id);
            ThemedMessageBox.Show(this, "You can select up to 7 favorite dashboards.", "Favorite dashboards", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        target.IsQuickAccess = isChecked;

        var ordered = _items.Where(i => i.IsQuickAccess)
            .Concat(_items.Where(i => !i.IsQuickAccess))
            .ToList();

        _items.Clear();
        _items.AddRange(ordered);
        RebindGridAndSelect(target.Id);
    }

    private void OnGridCellValidating(object? sender, DataGridViewCellValidatingEventArgs e) {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "Name") {
            return;
        }

        var proposedName = Convert.ToString(e.FormattedValue)?.Trim();
        if (!string.IsNullOrWhiteSpace(proposedName)) {
            return;
        }

        e.Cancel = true;
        ThemedMessageBox.Show(this, "Dashboard name cannot be empty.", "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    #endregion Private Methods

    #region Private Classes

    private sealed class DashboardListItem {
        public required Guid Id { get; init; }
        public required string Name { get; set; }
        public required bool IsQuickAccess { get; set; }
    }

    #endregion Private Classes
}

public readonly record struct DashboardOrderUpdate(Guid DashboardId, string Name, bool IsQuickAccess, int SortOrder);
