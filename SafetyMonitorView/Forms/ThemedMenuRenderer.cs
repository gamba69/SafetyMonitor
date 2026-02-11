using MaterialSkin;
using System.Drawing.Drawing2D;

namespace SafetyMonitorView.Forms;

/// <summary>
/// Custom MenuStrip renderer with theme support (light/dark)
/// </summary>
public class ThemedMenuRenderer : ToolStripProfessionalRenderer {
    #region Private Fields

    private static ThemedColorTable _colorTable = new(true);
    private bool _isLight;

    #endregion Private Fields

    #region Public Constructors

    public ThemedMenuRenderer() : base(_colorTable) {
        _isLight = true;
    }

    #endregion Public Constructors

    #region Public Methods

    public void UpdateTheme() {
        var skinManager = MaterialSkinManager.Instance;
        _isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        _colorTable.UpdateTheme(_isLight);
    }

    #endregion Public Methods

    #region Protected Methods

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
        e.ArrowColor = _isLight ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180);
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) {
        var rect = new Rectangle(e.ImageRectangle.X - 2, e.ImageRectangle.Y - 2,
            e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);

        var iconColor = _isLight ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180);

        using var brush = new SolidBrush(Color.FromArgb(_isLight ? 28 : 42, iconColor));
        e.Graphics.FillRectangle(brush, rect);

        // Draw checkmark
        var previousSmoothing = e.Graphics.SmoothingMode;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var pen = new Pen(iconColor, 2.2f) {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        var checkRect = e.ImageRectangle;
        var points = new Point[]
        {
            new(checkRect.X + 3, checkRect.Y + (checkRect.Height / 2) + 1),
            new(checkRect.X + (checkRect.Width / 2) - 1, checkRect.Y + checkRect.Height - 4),
            new(checkRect.X + checkRect.Width - 3, checkRect.Y + 4)
        };
        e.Graphics.DrawLines(pen, points);
        e.Graphics.SmoothingMode = previousSmoothing;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
        e.TextColor = _isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);
        base.OnRenderItemText(e);
    }



    #endregion Protected Methods

    #region Private Classes

    private class ThemedColorTable : ProfessionalColorTable {
        #region Private Fields

        private bool _isLight;

        #endregion Private Fields

        #region Public Constructors

        public ThemedColorTable(bool isLight) {
            _isLight = isLight;
            UseSystemColors = false;
        }

        #endregion Public Constructors

        #region Public Properties

        public override Color ButtonCheckedHighlight => CheckBackground;

        public override Color ButtonPressedHighlight => MenuItemPressedGradientBegin;

        // Button (for toolbar, but keeping consistent)
        public override Color ButtonSelectedHighlight => MenuItemSelected;

        public override Color ButtonSelectedHighlightBorder => MenuItemBorder;

        // Check background
        public override Color CheckBackground => _isLight
            ? Color.FromArgb(195, 225, 220)
            : Color.FromArgb(40, 90, 85);

        public override Color CheckPressedBackground => CheckSelectedBackground;

        public override Color CheckSelectedBackground => _isLight
            ? Color.FromArgb(175, 215, 210)
            : Color.FromArgb(45, 100, 95);

        // Image margin (left side of dropdown)
        public override Color ImageMarginGradientBegin => _isLight
            ? Color.FromArgb(245, 245, 245)
            : Color.FromArgb(42, 56, 61);

        public override Color ImageMarginGradientEnd => ImageMarginGradientBegin;

        public override Color ImageMarginGradientMiddle => ImageMarginGradientBegin;

        // Menu item border when selected
        public override Color MenuItemBorder => _isLight
            ? Color.FromArgb(195, 220, 215)
            : Color.FromArgb(55, 75, 80);

        // Pressed menu item
        public override Color MenuItemPressedGradientBegin => _isLight
            ? Color.FromArgb(215, 230, 228)
            : Color.FromArgb(46, 61, 66);

        public override Color MenuItemPressedGradientEnd => MenuItemPressedGradientBegin;

        public override Color MenuItemPressedGradientMiddle => MenuItemPressedGradientBegin;

        // Menu item selected (hover)
        public override Color MenuItemSelected => _isLight
            ? Color.FromArgb(225, 240, 238)
            : Color.FromArgb(53, 70, 76);

        // Selected menu item background
        public override Color MenuItemSelectedGradientBegin => _isLight
            ? Color.FromArgb(230, 242, 240)
            : Color.FromArgb(50, 66, 71);

        public override Color MenuItemSelectedGradientEnd => MenuItemSelectedGradientBegin;

        // Menu bar background
        public override Color MenuStripGradientBegin => _isLight
            ? Color.FromArgb(250, 250, 250)
            : Color.FromArgb(35, 47, 52);

        public override Color MenuStripGradientEnd => MenuStripGradientBegin;

        public override Color SeparatorDark => SeparatorLight;

        // Separator
        public override Color SeparatorLight => _isLight
            ? Color.FromArgb(230, 230, 230)
            : Color.FromArgb(53, 70, 76);

        // Dropdown menu background
        public override Color ToolStripDropDownBackground => _isLight
            ? Color.FromArgb(255, 255, 255)
            : Color.FromArgb(38, 52, 57);

        #endregion Public Properties

        #region Public Methods

        public void UpdateTheme(bool isLight) {
            _isLight = isLight;
        }

        #endregion Public Methods
    }

    #endregion Private Classes
}
