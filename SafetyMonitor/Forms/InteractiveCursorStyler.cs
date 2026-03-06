using SafetyMonitor.Controls;

namespace SafetyMonitor.Forms;

/// <summary>
/// Represents interactive cursor styler and encapsulates its related behavior and state.
/// </summary>
internal static class InteractiveCursorStyler {

    /// <summary>
    /// Applies the state for interactive cursor styler.
    /// </summary>
    /// <param name="control">Input value for control.</param>
    public static void Apply(Control control) {
        if (!control.Enabled) {
            if (ShouldUseHandCursor(control)) {
                control.Cursor = Cursors.Default;
            }

            return;
        }

        if (ShouldUseHandCursor(control)) {
            control.Cursor = Cursors.Hand;
        }
    }

    /// <summary>
    /// Applies the state for interactive cursor styler.
    /// </summary>
    /// <param name="items">Input value for items.</param>
    public static void Apply(ToolStripItemCollection items) {
        foreach (ToolStripItem item in items) {
            if (item is ToolStripSeparator) {
                continue;
            }

            if (item is ToolStripDropDownItem dropDownItem) {
                dropDownItem.DropDown.Cursor = Cursors.Hand;
                if (dropDownItem.HasDropDownItems) {
                    Apply(dropDownItem.DropDownItems);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether should use hand cursor for interactive cursor styler.
    /// </summary>
    /// <param name="control">Input value for control.</param>
    /// <returns><see langword="true"/> when the condition is satisfied; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Use the boolean result to branch success and fallback logic.
    /// </remarks>
    private static bool ShouldUseHandCursor(Control control) => control switch {
        ButtonBase => true,
        ComboBox => true,
        ListBox => true,
        TabControl => true,
        UpDownBase => true,
        TrackBar => true,
        ThemedComboBox => true,
        ThemedDateTimePicker => true,
        _ => false
    };
}
