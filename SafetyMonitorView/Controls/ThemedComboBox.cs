using MaterialSkin;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Text;

namespace SafetyMonitorView.Controls;

/// <summary>
/// Fully custom ComboBox (pull-down) with complete dark/light theme support.
/// Replaces standard ComboBox which cannot be fully themed.
/// Owner-draws the text area, dropdown button, and items popup.
/// Visual style matches ThemedDateTimePicker.
/// </summary>
public class ThemedComboBox : UserControl {

    #region Private Fields

    private readonly Panel _textPanel;
    private readonly Panel _buttonPanel;

    private int _selectedIndex = -1;
    private readonly ItemCollection _items;
    private bool _isHovering;
    private bool _isButtonHovering;
    private bool _isDroppedDown;
    private DropdownPopup? _popup;
    private bool _enforcingSafeFont;
    private string _displayMember = "";
    private int _maxDropDownItems = 8;
    private int _dropDownWidth;

    // Theme colors
    private Color _backColor;
    private Color _foreColor;
    private Color _borderColor;
    private Color? _borderColorOverride;
    private Color _buttonColor;
    private Color _buttonHoverColor;
    private Color _disabledBackColor;
    private Color _disabledForeColor;

    #endregion Private Fields

    #region Public Constructors

    public ThemedComboBox() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _items = new ItemCollection(this);
        Height = 28;
        Font = CreateSafeFont();

        _textPanel = new Panel { Dock = DockStyle.Fill, Cursor = Cursors.Hand };
        _textPanel.Paint += TextPanel_Paint;
        _textPanel.MouseEnter += (_, _) => { _isHovering = true; Invalidate(true); };
        _textPanel.MouseLeave += (_, _) => { _isHovering = false; Invalidate(true); };
        _textPanel.Click += (_, _) => TogglePopup();

        _buttonPanel = new Panel { Dock = DockStyle.Right, Width = 24, Cursor = Cursors.Hand };
        _buttonPanel.Paint += ButtonPanel_Paint;
        _buttonPanel.MouseEnter += (_, _) => { _isButtonHovering = true; _buttonPanel.Invalidate(); };
        _buttonPanel.MouseLeave += (_, _) => { _isButtonHovering = false; _buttonPanel.Invalidate(); };
        _buttonPanel.Click += (_, _) => TogglePopup();

        Controls.Add(_textPanel);
        Controls.Add(_buttonPanel);

        ApplyTheme();
    }

    #endregion Public Constructors

    #region Public Events

    public event EventHandler? SelectedIndexChanged;

    #endregion Public Events

    #region Public Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex {
        get => _selectedIndex;
        set {
            if (value < -1) {
                value = -1;
            }

            if (value >= _items.Count) {
                value = _items.Count - 1;
            }

            if (_selectedIndex != value) {
                _selectedIndex = value;
                Invalidate(true);
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? SelectedItem {
        get => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
        set {
            if (value == null) {
                SelectedIndex = -1;
                return;
            }
            var idx = _items.IndexOf(value);
            if (idx >= 0) {
                SelectedIndex = idx;
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ItemCollection Items => _items;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DisplayMember {
        get => _displayMember;
        set { _displayMember = value; Invalidate(true); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int MaxDropDownItems {
        get => _maxDropDownItems;
        set { _maxDropDownItems = Math.Max(1, value); }
    }

    /// <summary>
    /// Custom width for the dropdown popup. 0 = use control width.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DropDownWidth {
        get => _dropDownWidth;
        set { _dropDownWidth = Math.Max(0, value); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? BorderColorOverride {
        get => _borderColorOverride;
        set {
            if (_borderColorOverride == value) {
                return;
            }

            _borderColorOverride = value;
            Invalidate(true);
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>
    /// Re-reads theme from MaterialSkinManager and repaints.
    /// Call after theme switch or from ApplyThemeRecursive.
    /// </summary>
    public void ApplyTheme() {
        var sm = MaterialSkinManager.Instance;
        var isLight = sm.Theme == MaterialSkinManager.Themes.LIGHT;

        _backColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        _foreColor = isLight ? Color.Black : Color.White;
        _borderColor = isLight ? Color.FromArgb(200, 200, 200) : Color.FromArgb(60, 75, 80);
        _buttonColor = isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(53, 70, 76);
        _buttonHoverColor = isLight ? Color.FromArgb(220, 220, 220) : Color.FromArgb(63, 80, 86);
        _disabledBackColor = isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(38, 52, 57);
        _disabledForeColor = isLight ? Color.Gray : Color.FromArgb(120, 130, 135);

        Invalidate(true);
    }

    /// <summary>
    /// Gets the display text for an item, using DisplayMember if set.
    /// </summary>
    public string GetItemText(object? item) {
        if (item == null) {
            return "";
        }

        if (!string.IsNullOrEmpty(_displayMember)) {
            var prop = item.GetType().GetProperty(_displayMember);
            if (prop != null) {
                return prop.GetValue(item)?.ToString() ?? "";
            }
        }

        return item.ToString() ?? "";
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        var bg = Enabled ? _backColor : _disabledBackColor;
        using var bgBrush = new SolidBrush(bg);
        g.FillRectangle(bgBrush, ClientRectangle);

        var borderCol = GetBorderColor();
        using var borderPen = new Pen(borderCol);
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnEnabledChanged(EventArgs e) {
        base.OnEnabledChanged(e);
        _textPanel.Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        _buttonPanel.Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate(true);
    }

    protected override void OnGotFocus(EventArgs e) {
        base.OnGotFocus(e);
        Invalidate(true);
    }

    protected override void OnLostFocus(EventArgs e) {
        base.OnLostFocus(e);
        Invalidate(true);
    }

    protected override void OnFontChanged(EventArgs e) {
        base.OnFontChanged(e);

        if (_enforcingSafeFont) {
            return;
        }

        if (!IsInstalledFont(Font?.FontFamily?.Name)) {
            try {
                _enforcingSafeFont = true;
                Font = CreateSafeFont();
            } finally {
                _enforcingSafeFont = false;
            }
        }
    }

    #endregion Protected Methods

    #region Private Methods

    private static Font CreateSafeFont() {
        return new Font("Segoe UI", 9f, FontStyle.Regular);
    }

    private static bool IsInstalledFont(string? familyName) {
        if (string.IsNullOrWhiteSpace(familyName)) {
            return false;
        }

        var installedFonts = new InstalledFontCollection();
        return installedFonts.Families.Any(f =>
            string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));
    }

    private string GetDisplayText() {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count) {
            return "";
        }

        return GetItemText(_items[_selectedIndex]);
    }

    private void TextPanel_Paint(object? sender, PaintEventArgs e) {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var bg = Enabled ? _backColor : _disabledBackColor;
        using var bgBrush = new SolidBrush(bg);
        g.FillRectangle(bgBrush, _textPanel.ClientRectangle);

        var fg = Enabled ? _foreColor : _disabledForeColor;
        var text = GetDisplayText();
        var textRect = new Rectangle(4, 0, _textPanel.Width - 8, _textPanel.Height);
        TextRenderer.DrawText(g, text, Font, textRect, fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        using var borderPen = new Pen(GetBorderColor());
        g.DrawLine(borderPen, 0, 0, _textPanel.Width - 1, 0);
        g.DrawLine(borderPen, 0, _textPanel.Height - 1, _textPanel.Width - 1, _textPanel.Height - 1);
        g.DrawLine(borderPen, 0, 0, 0, _textPanel.Height - 1);
    }

    private void ButtonPanel_Paint(object? sender, PaintEventArgs e) {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var bg = !Enabled ? _disabledBackColor
            : _isButtonHovering ? _buttonHoverColor
            : _buttonColor;
        using var bgBrush = new SolidBrush(bg);
        g.FillRectangle(bgBrush, _buttonPanel.ClientRectangle);

        var fg = Enabled ? _foreColor : _disabledForeColor;
        using var arrowPen = new Pen(fg, 1.5f);
        var cx = _buttonPanel.Width / 2;
        var cy = _buttonPanel.Height / 2;
        g.DrawLine(arrowPen, cx - 4, cy - 2, cx, cy + 2);
        g.DrawLine(arrowPen, cx, cy + 2, cx + 4, cy - 2);

        using var borderPen = new Pen(GetBorderColor());
        g.DrawLine(borderPen, 0, 0, _buttonPanel.Width - 1, 0);
        g.DrawLine(borderPen, _buttonPanel.Width - 1, 0, _buttonPanel.Width - 1, _buttonPanel.Height - 1);
        g.DrawLine(borderPen, 0, _buttonPanel.Height - 1, _buttonPanel.Width - 1, _buttonPanel.Height - 1);
    }

    private Color GetBorderColor() {
        return _isHovering && Enabled
            ? Color.FromArgb(0, 121, 107)
            : _borderColorOverride ?? _borderColor;
    }

    private void TogglePopup() {
        if (!Enabled) {
            return;
        }

        if (_isDroppedDown && _popup != null) {
            _popup.Close();
            return;
        }

        ShowPopup();
    }

    private void ShowPopup() {
        if (_items.Count == 0) {
            return;
        }

        _popup = new DropdownPopup(this);
        _popup.ItemSelected += (_, idx) => { SelectedIndex = idx; };
        _popup.FormClosed += (_, _) => { _isDroppedDown = false; _popup = null; Invalidate(true); };

        var popupWidth = _dropDownWidth > 0 ? _dropDownWidth : Width;
        _popup.SetBounds(0, 0, popupWidth, _popup.Height);

        var screenPos = PointToScreen(new Point(0, Height));
        var screen = Screen.FromControl(this);
        var popupSize = _popup.Size;

        if (screenPos.Y + popupSize.Height > screen.WorkingArea.Bottom) {
            screenPos.Y = PointToScreen(Point.Empty).Y - popupSize.Height;
        }
        if (screenPos.X + popupSize.Width > screen.WorkingArea.Right) {
            screenPos.X = screen.WorkingArea.Right - popupSize.Width;
        }

        _popup.Location = screenPos;
        _isDroppedDown = true;
        _popup.Show(FindForm()!);
    }

    /// <summary>
    /// Called by ItemCollection when items change.
    /// </summary>
    internal void OnItemsChanged() {
        if (_selectedIndex >= _items.Count) {
            _selectedIndex = _items.Count - 1;
        }
        Invalidate(true);
    }

    #endregion Private Methods

    #region ItemCollection

    /// <summary>
    /// Collection of items with API compatible with ComboBox.ObjectCollection.
    /// Supports Add, AddRange, Clear, Remove, Insert, IndexOf, Contains, Count, indexer.
    /// </summary>
    public sealed class ItemCollection : IEnumerable<object> {
        private readonly List<object> _list = [];
        private readonly ThemedComboBox _owner;

        internal ItemCollection(ThemedComboBox owner) {
            _owner = owner;
        }

        public int Count => _list.Count;

        public object this[int index] {
            get => _list[index];
            set { _list[index] = value; _owner.OnItemsChanged(); }
        }

        public int Add(object item) {
            _list.Add(item);
            _owner.OnItemsChanged();
            return _list.Count - 1;
        }

        public void AddRange(object[] items) {
            _list.AddRange(items);
            _owner.OnItemsChanged();
        }

        public void Insert(int index, object item) {
            _list.Insert(index, item);
            _owner.OnItemsChanged();
        }

        public void Remove(object item) {
            _list.Remove(item);
            _owner.OnItemsChanged();
        }

        public void RemoveAt(int index) {
            _list.RemoveAt(index);
            _owner.OnItemsChanged();
        }

        public void Clear() {
            _list.Clear();
            _owner.OnItemsChanged();
        }

        public int IndexOf(object item) => _list.IndexOf(item);

        public bool Contains(object item) => _list.Contains(item);

        public IEnumerator<object> GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }

    #endregion ItemCollection

    #region Helpers

    /// <summary>
    /// Panel with DoubleBuffered enabled to prevent flicker on repaint.
    /// </summary>
    private sealed class BufferedPanel : Panel {
        public BufferedPanel() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }
    }

    #endregion Helpers

    #region DropdownPopup

    /// <summary>
    /// Themed dropdown list popup. Owner-drawn, double-buffered,
    /// with hover highlight and scroll support.
    /// Visual style matches ThemedDateTimePicker's CalendarPopup.
    /// </summary>
    private sealed class DropdownPopup : Form {

        private readonly ThemedComboBox _owner;
        private readonly int _itemHeight;
        private readonly int _visibleCount;
        private int _scrollOffset;
        private int _hoverIndex = -1;

        // Theme
        private readonly bool _isLight;
        private readonly Color _bg;
        private readonly Color _fg;
        private readonly Color _borderColor;
        private readonly Color _selectedBg;
        private readonly Color _hoverBg;
        private readonly Color _scrollBarBg;
        private readonly Color _scrollBarThumb;

        private readonly BufferedPanel _listPanel;
        private readonly bool _needsScrollBar;
        private const int ScrollBarWidth = 10;

        // Cached font
        private readonly Font _itemFont;

        // Animation
        private System.Windows.Forms.Timer? _fadeTimer;
        private bool _isClosing;
        private const int FadeIntervalMs = 15;
        private const double FadeInStep = 0.15;  // ~7 ticks = ~105ms
        private const double FadeOutStep = 0.2;   // ~5 ticks = ~75ms

        public event EventHandler<int>? ItemSelected;

        public DropdownPopup(ThemedComboBox owner) {
            _owner = owner;

            var sm = MaterialSkinManager.Instance;
            _isLight = sm.Theme == MaterialSkinManager.Themes.LIGHT;

            _bg = _isLight ? Color.FromArgb(255, 255, 255) : Color.FromArgb(38, 52, 57);
            _fg = _isLight ? Color.Black : Color.White;
            _borderColor = _isLight ? Color.FromArgb(200, 200, 200) : Color.FromArgb(55, 70, 75);
            _selectedBg = Color.FromArgb(0, 121, 107);
            _hoverBg = _isLight ? Color.FromArgb(232, 240, 238) : Color.FromArgb(48, 65, 70);
            _scrollBarBg = _isLight ? Color.FromArgb(240, 240, 240) : Color.FromArgb(30, 42, 47);
            _scrollBarThumb = _isLight ? Color.FromArgb(190, 190, 190) : Color.FromArgb(70, 85, 90);

            _itemFont = owner.Font ?? new Font("Segoe UI", 9f);

            // Calculate item height based on font
            using var tempG = Graphics.FromHwnd(IntPtr.Zero);
            var textSize = TextRenderer.MeasureText(tempG, "Xg", _itemFont);
            _itemHeight = Math.Max(24, textSize.Height + 8);

            _visibleCount = Math.Min(owner._items.Count, owner._maxDropDownItems);
            _needsScrollBar = owner._items.Count > _visibleCount;

            // Scroll to show selected item
            if (owner._selectedIndex >= _visibleCount) {
                _scrollOffset = Math.Min(owner._selectedIndex, owner._items.Count - _visibleCount);
            }

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = _bg;
            DoubleBuffered = true;

            var totalHeight = _visibleCount * _itemHeight + 2; // +2 for border
            ClientSize = new Size(owner.Width, totalHeight);

            _listPanel = new BufferedPanel {
                Dock = DockStyle.Fill,
                BackColor = _bg,
                Cursor = Cursors.Hand
            };
            _listPanel.Paint += ListPanel_Paint;
            _listPanel.MouseMove += ListPanel_MouseMove;
            _listPanel.MouseLeave += ListPanel_MouseLeave;
            _listPanel.MouseClick += ListPanel_MouseClick;
            _listPanel.MouseWheel += ListPanel_MouseWheel;

            Controls.Add(_listPanel);

            Opacity = 0;
            Deactivate += (_, _) => AnimateClose();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using var borderPen = new Pen(_borderColor);
            e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();
            base.OnFormClosed(e);
            _itemFont.Dispose();
        }

        protected override bool ShowWithoutActivation => false;

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            StartFadeIn();
        }

        #region Animation

        private void StartFadeIn() {
            _fadeTimer?.Dispose();
            _fadeTimer = new System.Windows.Forms.Timer { Interval = FadeIntervalMs };
            _fadeTimer.Tick += (_, _) => {
                var next = Opacity + FadeInStep;
                if (next >= 1.0) {
                    Opacity = 1.0;
                    _fadeTimer?.Stop();
                    _fadeTimer?.Dispose();
                    _fadeTimer = null;
                } else {
                    Opacity = next;
                }
            };
            _fadeTimer.Start();
        }

        private void AnimateClose() {
            if (_isClosing) {
                return;
            }

            _isClosing = true;
            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();
            _fadeTimer = new System.Windows.Forms.Timer { Interval = FadeIntervalMs };
            _fadeTimer.Tick += (_, _) => {
                var next = Opacity - FadeOutStep;
                if (next <= 0.05) {
                    _fadeTimer?.Stop();
                    _fadeTimer?.Dispose();
                    _fadeTimer = null;
                    Close();
                } else {
                    Opacity = next;
                }
            };
            _fadeTimer.Start();
        }

        #endregion Animation

        #region List painting

        private void ListPanel_Paint(object? sender, PaintEventArgs e) {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var listWidth = _needsScrollBar ? _listPanel.Width - ScrollBarWidth : _listPanel.Width;

            for (var i = 0; i < _visibleCount; i++) {
                var itemIndex = i + _scrollOffset;
                if (itemIndex >= _owner._items.Count) {
                    break;
                }

                var rect = new Rectangle(1, 1 + i * _itemHeight, listWidth - 2, _itemHeight);
                Color bg;
                Color fg;

                if (itemIndex == _owner._selectedIndex) {
                    bg = _selectedBg;
                    fg = Color.White;
                } else if (itemIndex == _hoverIndex) {
                    bg = _hoverBg;
                    fg = _fg;
                } else {
                    bg = _bg;
                    fg = _fg;
                }

                using var bgBrush = new SolidBrush(bg);
                g.FillRectangle(bgBrush, rect);

                var text = _owner.GetItemText(_owner._items[itemIndex]);
                var textRect = new Rectangle(rect.X + 6, rect.Y, rect.Width - 12, rect.Height);
                TextRenderer.DrawText(g, text, _itemFont, textRect, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // Draw scrollbar
            if (_needsScrollBar) {
                PaintScrollBar(g);
            }
        }

        private void PaintScrollBar(Graphics g) {
            var totalItems = _owner._items.Count;
            var sbX = _listPanel.Width - ScrollBarWidth;
            var sbRect = new Rectangle(sbX, 1, ScrollBarWidth - 1, _listPanel.Height - 2);

            using var sbBgBrush = new SolidBrush(_scrollBarBg);
            g.FillRectangle(sbBgBrush, sbRect);

            // Thumb
            var thumbRatio = (float)_visibleCount / totalItems;
            var thumbHeight = Math.Max(20, (int)(sbRect.Height * thumbRatio));
            var thumbOffset = totalItems > _visibleCount
                ? (int)((float)_scrollOffset / (totalItems - _visibleCount) * (sbRect.Height - thumbHeight))
                : 0;

            var thumbRect = new Rectangle(sbX + 2, 1 + thumbOffset, ScrollBarWidth - 5, thumbHeight);
            using var thumbBrush = new SolidBrush(_scrollBarThumb);
            g.FillRectangle(thumbBrush, thumbRect);
        }

        #endregion

        #region Mouse handling

        private int HitTestItem(Point location) {
            if (location.Y < 1 || location.Y >= _listPanel.Height - 1) {
                return -1;
            }

            var listWidth = _needsScrollBar ? _listPanel.Width - ScrollBarWidth : _listPanel.Width;
            if (location.X < 1 || location.X >= listWidth) {
                return -1;
            }

            var row = (location.Y - 1) / _itemHeight;
            var itemIndex = row + _scrollOffset;
            return itemIndex >= 0 && itemIndex < _owner._items.Count ? itemIndex : -1;
        }

        private void ListPanel_MouseMove(object? sender, MouseEventArgs e) {
            var newHover = HitTestItem(e.Location);
            if (newHover != _hoverIndex) {
                var prevHover = _hoverIndex;
                _hoverIndex = newHover;
                InvalidateItem(prevHover);
                InvalidateItem(newHover);
            }
        }

        private void ListPanel_MouseLeave(object? sender, EventArgs e) {
            if (_hoverIndex < 0) {
                return;
            }

            var prev = _hoverIndex;
            _hoverIndex = -1;
            InvalidateItem(prev);
        }

        private void ListPanel_MouseClick(object? sender, MouseEventArgs e) {
            var idx = HitTestItem(e.Location);
            if (idx >= 0) {
                ItemSelected?.Invoke(this, idx);
                AnimateClose();
            }
        }

        private void ListPanel_MouseWheel(object? sender, MouseEventArgs e) {
            if (!_needsScrollBar) {
                return;
            }

            var maxOffset = _owner._items.Count - _visibleCount;
            var delta = e.Delta > 0 ? -1 : 1;
            var newOffset = Math.Clamp(_scrollOffset + delta, 0, maxOffset);

            if (newOffset != _scrollOffset) {
                _scrollOffset = newOffset;
                _listPanel.Invalidate();
            }
        }

        private void InvalidateItem(int itemIndex) {
            if (itemIndex < _scrollOffset || itemIndex >= _scrollOffset + _visibleCount) {
                return;
            }

            var row = itemIndex - _scrollOffset;
            _listPanel.Invalidate(new Rectangle(0, 1 + row * _itemHeight, _listPanel.Width, _itemHeight));
        }

        #endregion
    }

    #endregion DropdownPopup
}
