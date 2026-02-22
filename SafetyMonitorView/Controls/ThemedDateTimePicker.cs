using MaterialSkin;
using System.ComponentModel;
using System.Drawing.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SafetyMonitorView.Controls;

/// <summary>
/// Fully custom DateTimePicker with complete dark/light theme support.
/// Replaces standard DateTimePicker which cannot be fully themed.
/// Owner-draws the text area, dropdown button, and calendar popup.
/// </summary>
public class ThemedDateTimePicker : UserControl {

    #region Private Fields

    private readonly Panel _textPanel;
    private readonly Panel _buttonPanel;

    private DateTime _value = DateTime.Now;
    private DateTime _minDate = DateTimePicker.MinimumDateTime;
    private DateTime _maxDate = DateTimePicker.MaximumDateTime;
    private string _customFormat = "yyyy-MM-dd HH:mm";
    private DateTimePickerFormat _format = DateTimePickerFormat.Custom;
    private bool _isHovering;
    private bool _isButtonHovering;
    private bool _isDroppedDown;
    private CalendarPopup? _popup;
    private bool _enforcingSafeFont;

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

    public ThemedDateTimePicker() {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

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

    public event EventHandler? ValueChanged;

    #endregion Public Events

    #region Public Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime Value {
        get => _value;
        set {
            if (value < _minDate) {
                value = _minDate;
            }

            if (value > _maxDate) {
                value = _maxDate;
            }

            if (_value != value) {
                _value = value;
                Invalidate(true);
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime MinDate {
        get => _minDate;
        set { _minDate = value; if (_value < _minDate) {
                Value = _minDate;
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime MaxDate {
        get => _maxDate;
        set { _maxDate = value; if (_value > _maxDate) {
                Value = _maxDate;
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CustomFormat {
        get => _customFormat;
        set { _customFormat = value; Invalidate(true); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTimePickerFormat Format {
        get => _format;
        set { _format = value; Invalidate(true); }
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

    #endregion Protected Methods

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

    #region Private Methods

    private string GetFormattedText() {
        var normalizedCustomFormat = NormalizeTimeFormat(_customFormat);
        return _format switch {
            DateTimePickerFormat.Long => _value.ToString("D", CultureInfo.InvariantCulture),
            DateTimePickerFormat.Short => _value.ToString("d", CultureInfo.InvariantCulture),
            DateTimePickerFormat.Time => _value.ToString("T", CultureInfo.InvariantCulture),
            DateTimePickerFormat.Custom => _value.ToString(normalizedCustomFormat, CultureInfo.InvariantCulture),
            _ => _value.ToString(normalizedCustomFormat, CultureInfo.InvariantCulture)
        };
    }

    private static string NormalizeTimeFormat(string format) {
        if (string.IsNullOrWhiteSpace(format)) {
            return format;
        }

        var normalized = Regex.Replace(format, @"(?<!H)H(?!H)", "HH");
        normalized = Regex.Replace(normalized, @"(?<!h)h(?!h)", "hh");
        normalized = Regex.Replace(normalized, @"(?<!m)m(?!m)", "mm");
        return normalized;
    }

    private void TextPanel_Paint(object? sender, PaintEventArgs e) {
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bg = Enabled ? _backColor : _disabledBackColor;
        using var bgBrush = new SolidBrush(bg);
        g.FillRectangle(bgBrush, _textPanel.ClientRectangle);

        var fg = Enabled ? _foreColor : _disabledForeColor;
        using var fgBrush = new SolidBrush(fg);
        var text = GetFormattedText();
        var textSize = g.MeasureString(text, Font);
        var y = (_textPanel.Height - textSize.Height) / 2f;
        g.DrawString(text, Font, fgBrush, 4, y);

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
        _popup = new CalendarPopup(_value, _minDate, _maxDate, _format, _customFormat);
        _popup.DateSelected += (_, dt) => { Value = dt; };
        _popup.FormClosed += (_, _) => { _isDroppedDown = false; _popup = null; Invalidate(true); };

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

    #endregion Private Methods

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

    #region CalendarPopup

    /// <summary>
    /// Themed calendar dropdown with month navigation, day grid, and optional time editing.
    /// All custom-painted panels are double-buffered.
    /// Layout uses DPI-aware sizing. Month names always in English.
    /// Hover only invalidates the changed cell, not the whole grid.
    /// </summary>
    private sealed class CalendarPopup : Form {

        private DateTime _displayMonth;
        private DateTime _selectedDate;
        private readonly DateTime _minDate;
        private readonly DateTime _maxDate;
        private readonly bool _showTime;

        // Theme
        private readonly bool _isLight;
        private readonly Color _bg;
        private readonly Color _fg;
        private readonly Color _headerBg;
        private readonly Color _borderColor;
        private readonly Color _todayColor;
        private readonly Color _selectedBg;
        private readonly Color _hoverBg;
        private readonly Color _dimFg;
        private readonly Color _inputBg;
        private readonly Color _accentColor = Color.FromArgb(0, 121, 107);

        // Layout (DPI-scaled)
        private readonly int _cellSize;
        private readonly int _headerHeight;
        private readonly int _dayHeaderHeight;
        private const int Cols = 7;
        private const int MaxRows = 6;
        private readonly int _pad;
        private readonly int _timeRowHeight;

        private readonly BufferedPanel _calendarPanel;
        private readonly BufferedPanel _headerPanel;
        private readonly Label _monthLabel;
        private readonly Button _prevButton;
        private readonly Button _nextButton;
        private readonly Button _prevYearButton;
        private readonly Button _nextYearButton;
        private readonly BufferedPanel _dayHeaderPanel;
        private readonly string? _materialIconFontFamily;
        private readonly Panel? _timePanel;
        private NumericUpDown? _hourSpin;
        private NumericUpDown? _minuteSpin;

        private int _hoverRow = -1;
        private int _hoverCol = -1;

        // Cached fonts — created once, disposed in OnFormClosed
        private readonly Font _dayFont;
        private readonly Font _dayHeaderFont;
        private readonly Font _headerFont;

        // Animation
        private System.Windows.Forms.Timer? _fadeTimer;
        private bool _isClosing;
        private const int FadeIntervalMs = 15;
        private const double FadeInStep = 0.15;
        private const double FadeOutStep = 0.2;

        public event EventHandler<DateTime>? DateSelected;

        public CalendarPopup(DateTime value, DateTime minDate, DateTime maxDate,
                             DateTimePickerFormat format, string customFormat) {
            _selectedDate = value;
            _displayMonth = new DateTime(value.Year, value.Month, 1);
            _minDate = minDate;
            _maxDate = maxDate;
            _showTime = format == DateTimePickerFormat.Custom &&
                        (customFormat.Contains('H') || customFormat.Contains('h') ||
                         customFormat.Contains('m') || customFormat.Contains('t'));

            var sm = MaterialSkinManager.Instance;
            _isLight = sm.Theme == MaterialSkinManager.Themes.LIGHT;

            _bg = _isLight ? Color.FromArgb(255, 255, 255) : Color.FromArgb(38, 52, 57);
            _fg = _isLight ? Color.Black : Color.White;
            _headerBg = _isLight ? Color.FromArgb(245, 245, 245) : Color.FromArgb(30, 42, 47);
            _borderColor = _isLight ? Color.FromArgb(200, 200, 200) : Color.FromArgb(55, 70, 75);
            _todayColor = Color.FromArgb(0, 121, 107);
            _selectedBg = Color.FromArgb(0, 121, 107);
            _hoverBg = _isLight ? Color.FromArgb(232, 240, 238) : Color.FromArgb(48, 65, 70);
            _dimFg = _isLight ? Color.FromArgb(170, 170, 170) : Color.FromArgb(90, 105, 110);
            _inputBg = _isLight ? Color.White : Color.FromArgb(46, 61, 66);
            _materialIconFontFamily = ResolveMaterialFontFamily();

            // DPI-aware sizing
            var dpiScale = GetDpiScale();
            _cellSize = Scale(32, dpiScale);
            _headerHeight = Scale(36, dpiScale);
            _dayHeaderHeight = Scale(24, dpiScale);
            _pad = Scale(6, dpiScale);
            _timeRowHeight = Scale(42, dpiScale);

            // Cached fonts
            _dayFont = new Font("Segoe UI", 9f);
            _dayHeaderFont = new Font("Segoe UI", 7.5f);
            _headerFont = new Font("Segoe UI", 10f, FontStyle.Bold);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = _bg;
            DoubleBuffered = true;

            var totalWidth = Cols * _cellSize + _pad * 2;
            var bottomSectionHeight = _showTime ? _timeRowHeight : _pad;
            var totalHeight = _headerHeight + _dayHeaderHeight + MaxRows * _cellSize + _pad + bottomSectionHeight;
            ClientSize = new Size(totalWidth, totalHeight);

            // ── Header: [◁][◀] Month Year [▶][▷] ──
            _headerPanel = new BufferedPanel {
                Dock = DockStyle.Top,
                Height = _headerHeight,
                BackColor = _headerBg
            };

            var navBtnSize = Scale(24, dpiScale);
            _prevYearButton = CreateYearNavButton("\uEAC3", "◁", -1, navBtnSize);
            _prevYearButton.Location = new Point(_pad, (_headerHeight - navBtnSize) / 2);
            _headerPanel.Controls.Add(_prevYearButton);

            _prevButton = CreateNavButton("\uE314", "◀", -1, navBtnSize);
            _prevButton.Location = new Point(_pad + navBtnSize, (_headerHeight - navBtnSize) / 2);
            _headerPanel.Controls.Add(_prevButton);

            _nextButton = CreateNavButton("\uE315", "▶", 1, navBtnSize);
            _nextButton.Location = new Point(totalWidth - navBtnSize * 2 - _pad, (_headerHeight - navBtnSize) / 2);
            _headerPanel.Controls.Add(_nextButton);

            _nextYearButton = CreateYearNavButton("\uEAC9", "▷", 1, navBtnSize);
            _nextYearButton.Location = new Point(totalWidth - navBtnSize - _pad, (_headerHeight - navBtnSize) / 2);
            _headerPanel.Controls.Add(_nextYearButton);

            _monthLabel = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Font = _headerFont,
                ForeColor = _fg,
                BackColor = _headerBg,
                Location = new Point(navBtnSize * 2 + _pad, 0),
                Size = new Size(totalWidth - 2 * (navBtnSize * 2 + _pad), _headerHeight),
                Cursor = Cursors.Default
            };
            UpdateMonthLabel();
            _headerPanel.Controls.Add(_monthLabel);

            // ── Day-of-week headers ──
            _dayHeaderPanel = new BufferedPanel {
                Dock = DockStyle.Top,
                Height = _dayHeaderHeight,
                BackColor = _bg
            };
            _dayHeaderPanel.Paint += DayHeaderPanel_Paint;

            // ── Calendar grid ──
            _calendarPanel = new BufferedPanel {
                Location = new Point(0, _headerHeight + _dayHeaderHeight),
                Size = new Size(totalWidth, MaxRows * _cellSize + _pad),
                BackColor = _bg,
                Cursor = Cursors.Hand
            };
            _calendarPanel.Paint += CalendarPanel_Paint;
            _calendarPanel.MouseMove += CalendarPanel_MouseMove;
            _calendarPanel.MouseLeave += CalendarPanel_MouseLeave;
            _calendarPanel.MouseClick += CalendarPanel_MouseClick;

            Controls.Add(_calendarPanel);
            Controls.Add(_dayHeaderPanel);
            Controls.Add(_headerPanel);

            // ── Time row ──
            if (_showTime) {
                _timePanel = new BufferedPanel {
                    Location = new Point(0, _headerHeight + _dayHeaderHeight + MaxRows * _cellSize + _pad),
                    Size = new Size(totalWidth, _timeRowHeight),
                    BackColor = _headerBg
                };
                BuildTimeControls(dpiScale);
                Controls.Add(_timePanel);
            }

            Opacity = 0;
            Deactivate += (_, _) => AnimateClose();
        }

        protected override CreateParams CreateParams {
            get {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using var borderPen = new Pen(_borderColor);
            e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();
            _dayFont.Dispose();
            _dayHeaderFont.Dispose();
            _headerFont.Dispose();
            base.OnFormClosed(e);
        }

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

        #region DPI helpers

        private static float GetDpiScale() {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            var nativeScale = g.DpiX / 96f;
            var softenedScale = 1f + ((nativeScale - 1f) * 0.5f);
            return Math.Max(1f, softenedScale);
        }

        private static int Scale(int value, float dpiScale) =>
            (int)Math.Round(value * dpiScale);

        #endregion

        #region Header / Navigation

        private Button CreateNavButton(string materialGlyph, string fallbackText, int monthDelta, int size) {
            var btn = new Button {
                Text = GetNavigationButtonText(materialGlyph, fallbackText),
                Size = new Size(size, size),
                FlatStyle = FlatStyle.Flat,
                Font = CreateNavigationButtonFont(size),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = _fg,
                BackColor = _headerBg,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = _hoverBg;
            btn.Click += (_, _) => {
                _displayMonth = _displayMonth.AddMonths(monthDelta);
                UpdateMonthLabel();
                _calendarPanel.Invalidate();
            };
            return btn;
        }

        private Button CreateYearNavButton(string materialGlyph, string fallbackText, int yearDelta, int size) {
            var btn = new Button {
                Text = GetNavigationButtonText(materialGlyph, fallbackText),
                Size = new Size(size, size),
                FlatStyle = FlatStyle.Flat,
                Font = CreateNavigationButtonFont(size),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = _fg,
                BackColor = _headerBg,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = _hoverBg;
            btn.Click += (_, _) => {
                _displayMonth = _displayMonth.AddYears(yearDelta);
                UpdateMonthLabel();
                _calendarPanel.Invalidate();
            };
            return btn;
        }


        private string GetNavigationButtonText(string materialGlyph, string fallbackText) {
            return _materialIconFontFamily is null ? fallbackText : materialGlyph;
        }

        private Font CreateNavigationButtonFont(int buttonSize) {
            if (_materialIconFontFamily is null) {
                return new Font("Segoe UI", 10f, FontStyle.Bold);
            }

            var fontSize = Math.Max(10f, buttonSize * 0.6f);
            return new Font(_materialIconFontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        private static string? ResolveMaterialFontFamily() {
            var candidates = new[] { "Material Symbols Outlined", "Material Symbols Rounded", "Material Icons" };
            using var installedFonts = new InstalledFontCollection();
            var installed = installedFonts.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates) {
                if (installed.Contains(candidate)) {
                    return candidate;
                }
            }

            return null;
        }

        private void UpdateMonthLabel() {
            _monthLabel.Text = _displayMonth.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        }

        #endregion

        #region Day header painting

        private void DayHeaderPanel_Paint(object? sender, PaintEventArgs e) {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var dayNames = new[] { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };
            using var brush = new SolidBrush(_dimFg);
            for (int i = 0; i < 7; i++) {
                var x = _pad + i * _cellSize;
                var sz = g.MeasureString(dayNames[i], _dayHeaderFont);
                g.DrawString(dayNames[i], _dayHeaderFont, brush,
                    x + (_cellSize - sz.Width) / 2,
                    (_dayHeaderHeight - sz.Height) / 2);
            }
        }

        #endregion

        #region Calendar grid painting

        private void CalendarPanel_Paint(object? sender, PaintEventArgs e) {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var firstDay = _displayMonth;
            var startOffset = ((int)firstDay.DayOfWeek + 6) % 7; // Monday = 0
            var daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            var today = DateTime.Today;

            for (int row = 0; row < MaxRows; row++) {
                for (int col = 0; col < Cols; col++) {
                    var dayIndex = row * Cols + col - startOffset;
                    if (dayIndex < 0 || dayIndex >= daysInMonth) {
                        var otherDate = firstDay.AddDays(dayIndex);
                        DrawDayCell(g, row, col, otherDate.Day,
                            isDim: true, isToday: false, isSelected: false,
                            isHovered: false, isEnabled: false);
                        continue;
                    }

                    var day = dayIndex + 1;
                    var date = new DateTime(_displayMonth.Year, _displayMonth.Month, day);
                    DrawDayCell(g, row, col, day,
                        isDim: false,
                        isToday: date == today,
                        isSelected: date == _selectedDate.Date,
                        isHovered: row == _hoverRow && col == _hoverCol,
                        isEnabled: date >= _minDate.Date && date <= _maxDate.Date);
                }
            }
        }

        private void DrawDayCell(Graphics g, int row, int col, int day,
                                 bool isDim, bool isToday, bool isSelected,
                                 bool isHovered, bool isEnabled) {
            var x = _pad + col * _cellSize;
            var y = row * _cellSize;
            var margin = Math.Max(2, (int)(_cellSize * 0.08f));
            var innerRect = new Rectangle(x + margin, y + margin,
                _cellSize - margin * 2, _cellSize - margin * 2);

            // Background fill for cell area (clear previous content)
            using (var clearBrush = new SolidBrush(_bg)) {
                g.FillRectangle(clearBrush, x, y, _cellSize, _cellSize);
            }

            if (isSelected) {
                using var selBrush = new SolidBrush(_selectedBg);
                g.FillEllipse(selBrush, innerRect);
            } else if (isHovered && isEnabled && !isDim) {
                using var hoverBrush = new SolidBrush(_hoverBg);
                g.FillEllipse(hoverBrush, innerRect);
            }

            if (isToday && !isSelected) {
                using var todayPen = new Pen(_todayColor, 1.5f);
                g.DrawEllipse(todayPen, innerRect);
            }

            Color textColor;
            if (isSelected) {
                textColor = Color.White;
            } else if (isDim || !isEnabled) {
                textColor = _dimFg;
            } else {
                textColor = _fg;
            }

            var text = day.ToString();
            var sz = g.MeasureString(text, _dayFont);
            using var textBrush = new SolidBrush(textColor);
            g.DrawString(text, _dayFont, textBrush,
                x + (_cellSize - sz.Width) / 2,
                y + (_cellSize - sz.Height) / 2);
        }

        #endregion

        #region Mouse interaction

        private (int row, int col) HitTest(Point p) {
            var col = (p.X - _pad) / _cellSize;
            var row = p.Y / _cellSize;
            if (col < 0 || col >= Cols || row < 0 || row >= MaxRows) {
                return (-1, -1);
            }

            return (row, col);
        }

        private DateTime? GetDateFromCell(int row, int col) {
            var startOffset = ((int)_displayMonth.DayOfWeek + 6) % 7;
            var dayIndex = row * Cols + col - startOffset;
            var daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);
            if (dayIndex < 0 || dayIndex >= daysInMonth) {
                return null;
            }

            return new DateTime(_displayMonth.Year, _displayMonth.Month, dayIndex + 1);
        }

        private void CalendarPanel_MouseMove(object? sender, MouseEventArgs e) {
            var (row, col) = HitTest(e.Location);
            if (row == _hoverRow && col == _hoverCol) {
                return;
            }

            var prevRow = _hoverRow;
            var prevCol = _hoverCol;
            _hoverRow = row;
            _hoverCol = col;

            // Invalidate only the two affected cells
            InvalidateCell(prevRow, prevCol);
            InvalidateCell(row, col);
        }

        private void CalendarPanel_MouseLeave(object? sender, EventArgs e) {
            if (_hoverRow < 0 && _hoverCol < 0) {
                return;
            }

            var prevRow = _hoverRow;
            var prevCol = _hoverCol;
            _hoverRow = -1;
            _hoverCol = -1;
            InvalidateCell(prevRow, prevCol);
        }

        private void InvalidateCell(int row, int col) {
            if (row < 0 || col < 0) {
                return;
            }

            _calendarPanel.Invalidate(new Rectangle(
                _pad + col * _cellSize, row * _cellSize, _cellSize, _cellSize));
        }

        private void CalendarPanel_MouseClick(object? sender, MouseEventArgs e) {
            var (row, col) = HitTest(e.Location);
            var date = GetDateFromCell(row, col);
            if (date == null) {
                return;
            }

            if (date < _minDate.Date || date > _maxDate.Date) {
                return;
            }

            DateTime result;
            if (_showTime && _hourSpin != null && _minuteSpin != null) {
                result = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day,
                    (int)_hourSpin.Value, (int)_minuteSpin.Value, 0);
            } else {
                result = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day,
                    _selectedDate.Hour, _selectedDate.Minute, _selectedDate.Second);
            }

            _selectedDate = result;
            DateSelected?.Invoke(this, result);
            AnimateClose();
        }

        #endregion

        #region Time controls

        private void BuildTimeControls(float dpiScale) {
            if (_timePanel == null) {
                return;
            }

            var normalFont = new Font("Segoe UI", 9f);
            var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            var spinHeight = Scale(24, dpiScale);
            var btnHeight = Scale(22, dpiScale);
            var leftPadding = _pad;
            var gapAfterLabel = Scale(4, dpiScale);
            var gapAfterHour = Scale(2, dpiScale);
            var gapAfterColon = Scale(2, dpiScale);
            var gapAfterMinute = Scale(6, dpiScale);
            var minSpinWidth = Scale(36, dpiScale);
            var preferredSpinWidth = Scale(52, dpiScale);
            var minNowWidth = Scale(34, dpiScale);

            var labelWidth = TextRenderer.MeasureText("Time:", boldFont).Width;
            var colonWidth = TextRenderer.MeasureText(":", boldFont).Width;
            var availableWidth = _timePanel.Width - leftPadding;
            var fixedWidth = labelWidth + gapAfterLabel + colonWidth + gapAfterColon + gapAfterHour + gapAfterMinute + minNowWidth;
            var dynamicWidth = Math.Max(minSpinWidth * 2, availableWidth - fixedWidth);
            var spinWidth = Math.Clamp(dynamicWidth / 2, minSpinWidth, preferredSpinWidth);
            var nowBtnWidth = Math.Clamp(availableWidth - (labelWidth + gapAfterLabel + spinWidth + gapAfterHour + colonWidth + gapAfterColon + spinWidth + gapAfterMinute), minNowWidth, Scale(44, dpiScale));

            var flow = new FlowLayoutPanel {
                Dock = DockStyle.None,
                WrapContents = false,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = _headerBg,
                Padding = System.Windows.Forms.Padding.Empty
            };

            flow.Controls.Add(new Label {
                Text = "Time:",
                Font = boldFont,
                ForeColor = _fg,
                AutoSize = true,
                Margin = new System.Windows.Forms.Padding(0, 2, gapAfterLabel, 0)
            });

            _hourSpin = new NumericUpDown {
                Minimum = 0,
                Maximum = 23,
                Value = _selectedDate.Hour,
                Width = spinWidth,
                Height = spinHeight,
                Font = normalFont,
                BackColor = _inputBg,
                ForeColor = _fg,
                Margin = new System.Windows.Forms.Padding(0, 0, gapAfterHour, 0)
            };
            _hourSpin.ValueChanged += (_, _) => EnsureLeadingZeros(_hourSpin);
            flow.Controls.Add(_hourSpin);

            flow.Controls.Add(new Label {
                Text = ":",
                Font = boldFont,
                ForeColor = _fg,
                AutoSize = true,
                Margin = new System.Windows.Forms.Padding(0, 2, gapAfterColon, 0)
            });

            _minuteSpin = new NumericUpDown {
                Minimum = 0,
                Maximum = 59,
                Value = _selectedDate.Minute,
                Width = spinWidth,
                Height = spinHeight,
                Font = normalFont,
                BackColor = _inputBg,
                ForeColor = _fg,
                Margin = new System.Windows.Forms.Padding(0, 0, gapAfterMinute, 0)
            };
            _minuteSpin.ValueChanged += (_, _) => EnsureLeadingZeros(_minuteSpin);
            flow.Controls.Add(_minuteSpin);

            var nowBtn = new Button {
                Text = "Now",
                AutoSize = false,
                Size = new Size(nowBtnWidth, btnHeight),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.White,
                BackColor = _accentColor,
                Cursor = Cursors.Hand,
                Margin = new System.Windows.Forms.Padding(0)
            };
            nowBtn.FlatAppearance.BorderSize = 0;
            nowBtn.Click += (_, _) => {
                var now = DateTime.Now;
                _hourSpin.Value = now.Hour;
                _minuteSpin.Value = now.Minute;
                _selectedDate = new DateTime(
                    _selectedDate.Year, _selectedDate.Month, _selectedDate.Day,
                    now.Hour, now.Minute, 0);
                DateSelected?.Invoke(this, _selectedDate);
                AnimateClose();
            };
            flow.Controls.Add(nowBtn);

            void CenterTimeFlow() {
                var preferred = flow.PreferredSize;
                flow.Location = new Point(
                    Math.Max(0, (_timePanel.Width - preferred.Width) / 2),
                    Math.Max(0, (_timePanel.Height - preferred.Height) / 2));
            }

            _timePanel.Controls.Add(flow);
            CenterTimeFlow();
            _timePanel.Resize += (_, _) => CenterTimeFlow();

            EnsureLeadingZeros(_hourSpin);
            EnsureLeadingZeros(_minuteSpin);
        }

        private static void EnsureLeadingZeros(NumericUpDown? numericUpDown) {
            if (numericUpDown?.Controls.Count > 1 && numericUpDown.Controls[1] is TextBox textBox) {
                textBox.Text = ((int)numericUpDown.Value).ToString("00", CultureInfo.InvariantCulture);
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        #endregion
    }

    #endregion CalendarPopup
}
