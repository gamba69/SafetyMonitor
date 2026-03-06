using MaterialSkin;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents themed menu renderer and encapsulates its related behavior and state.
/// </summary>
public class ThemedMenuRenderer : ToolStripProfessionalRenderer {
    #region Private Fields

    private static ThemedColorTable _colorTable = new(true);
    private bool _isLight;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemedMenuRenderer"/> class.
    /// </summary>
    /// <param name="base(_colorTable">Input value for base color table.</param>
    /// <remarks>
    /// The constructor wires required dependencies and initial state.
    /// </remarks>
    public ThemedMenuRenderer() : base(_colorTable) {
        _isLight = true;
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Updates the theme for themed menu renderer.
    /// </summary>
    public void UpdateTheme() {
        var skinManager = MaterialSkinManager.Instance;
        _isLight = skinManager.Theme == MaterialSkinManager.Themes.LIGHT;
        _colorTable.UpdateTheme(_isLight);
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Executes on render arrow as part of themed menu renderer processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) {
        e.ArrowColor = _isLight ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180);
        base.OnRenderArrow(e);
    }

    /// <summary>
    /// Executes on render item text as part of themed menu renderer processing.
    /// </summary>
    /// <param name="e">Input value for e.</param>
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
        e.TextColor = _isLight ? Color.FromArgb(33, 33, 33) : Color.FromArgb(240, 240, 240);
        base.OnRenderItemText(e);
    }



    #endregion Protected Methods

    #region Private Classes

    /// <summary>
    /// Represents themed color table and encapsulates its related behavior and state.
    /// </summary>
    private class ThemedColorTable : ProfessionalColorTable {
        #region Private Fields

        private bool _isLight;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemedColorTable"/> class.
        /// </summary>
        /// <param name="isLight">Input value for is light.</param>
        /// <remarks>
        /// The constructor wires required dependencies and initial state.
        /// </remarks>
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
            ? Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.White, 0.75f)
            : Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.Black, 0.45f);

        public override Color CheckPressedBackground => CheckSelectedBackground;

        public override Color CheckSelectedBackground => _isLight
            ? Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.White, 0.68f)
            : Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.Black, 0.35f);

        // Image margin (left side of dropdown)
        public override Color ImageMarginGradientBegin => _isLight
            ? Color.FromArgb(245, 245, 245)
            : Color.FromArgb(42, 56, 61);

        public override Color ImageMarginGradientEnd => ImageMarginGradientBegin;

        public override Color ImageMarginGradientMiddle => ImageMarginGradientBegin;

        // Menu item border when selected
        public override Color MenuItemBorder => _isLight
            ? Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.White, 0.72f)
            : Color.FromArgb(55, 75, 80);

        // Pressed menu item
        public override Color MenuItemPressedGradientBegin => _isLight
            ? Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.White, 0.82f)
            : Color.FromArgb(46, 61, 66);

        public override Color MenuItemPressedGradientEnd => MenuItemPressedGradientBegin;

        public override Color MenuItemPressedGradientMiddle => MenuItemPressedGradientBegin;

        // Menu item selected (hover)
        public override Color MenuItemSelected => _isLight
            ? Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.White, 0.86f)
            : Color.FromArgb(53, 70, 76);

        // Selected menu item background
        public override Color MenuItemSelectedGradientBegin => _isLight
            ? Blend(MaterialSkinManager.Instance.ColorScheme.PrimaryColor, Color.White, 0.88f)
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

        /// <summary>
        /// Updates the theme for themed color table.
        /// </summary>
        /// <param name="isLight">Input value for is light.</param>
        public void UpdateTheme(bool isLight) {
            _isLight = isLight;
        }

        /// <summary>
        /// Executes blend as part of themed color table processing.
        /// </summary>
        /// <param name="source">Input value for source.</param>
        /// <param name="target">Input value for target.</param>
        /// <param name="ratioToTarget">Input value for ratio to target.</param>
        /// <returns>The result of the operation.</returns>
        private static Color Blend(Color source, Color target, float ratioToTarget) {
            var clamped = Math.Clamp(ratioToTarget, 0f, 1f);
            var ratioToSource = 1f - clamped;
            var r = (int)Math.Round(source.R * ratioToSource + target.R * clamped);
            var g = (int)Math.Round(source.G * ratioToSource + target.G * clamped);
            var b = (int)Math.Round(source.B * ratioToSource + target.B * clamped);
            return Color.FromArgb(r, g, b);
        }

        #endregion Public Methods
    }

    #endregion Private Classes
}
