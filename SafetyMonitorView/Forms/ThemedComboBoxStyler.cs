using System.Runtime.InteropServices;
using MaterialSkin;

namespace SafetyMonitorView.Forms;

internal static class ThemedComboBoxStyler {
    private static readonly Color DarkSelectedBackColor = Color.FromArgb(0, 137, 123);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    internal static void Apply(ComboBox comboBox, bool isLight) {
        comboBox.BackColor = isLight ? Color.White : Color.FromArgb(46, 61, 66);
        comboBox.ForeColor = isLight ? Color.Black : Color.White;
        comboBox.FlatStyle = FlatStyle.Popup;
        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
        ApplyNativeTheme(comboBox);
        comboBox.DrawItem -= ComboBox_DrawItem;
        comboBox.DrawItem += ComboBox_DrawItem;
    }


    internal static void ApplyNativeTheme(ComboBox comboBox) {
        comboBox.HandleCreated -= ComboBox_HandleCreated;
        comboBox.HandleCreated += ComboBox_HandleCreated;

        if (comboBox.IsHandleCreated) {
            _ = SetWindowTheme(comboBox.Handle, "", "");
        }
    }

    private static void ComboBox_HandleCreated(object? sender, EventArgs e) {
        if (sender is ComboBox comboBox) {
            _ = SetWindowTheme(comboBox.Handle, "", "");
        }
    }

    private static void ComboBox_DrawItem(object? sender, DrawItemEventArgs e) {
        if (sender is not ComboBox comboBox || e.Index < 0) {
            return;
        }

        var isLight = MaterialSkinManager.Instance.Theme == MaterialSkinManager.Themes.LIGHT;
        var bg = comboBox.BackColor;
        var fg = comboBox.ForeColor;

        if ((e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0) {
            bg = isLight ? SystemColors.Highlight : DarkSelectedBackColor;
            fg = isLight ? SystemColors.HighlightText : Color.White;
        }

        using var bgBrush = new SolidBrush(bg);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        var text = comboBox.GetItemText(comboBox.Items[e.Index]);
        TextRenderer.DrawText(e.Graphics, text, e.Font ?? comboBox.Font, e.Bounds, fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
