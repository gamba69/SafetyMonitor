using SafetyMonitorView.Controls;

namespace SafetyMonitorView.Forms;

internal static class InteractiveCursorStyler {

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
