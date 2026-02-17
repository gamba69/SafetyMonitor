using SafetyMonitorView.Services;

namespace SafetyMonitorView.Forms;

internal static class ThemedButtonStyler {

    #region Private Fields

    private static readonly Color PrimaryButtonColor = Color.FromArgb(0, 137, 123);
    private static readonly Color CancelButtonColorLight = Color.FromArgb(189, 189, 189);
    private static readonly Color CancelButtonColorDark = Color.FromArgb(96, 105, 109);
    private static readonly Color DeleteButtonColorLight = Color.FromArgb(198, 64, 58);
    private static readonly Color DeleteButtonColorDark = Color.FromArgb(171, 74, 70);
    private static readonly Color SecondaryButtonColorLight = Color.FromArgb(220, 220, 220);
    private static readonly Color SecondaryButtonColorDark = Color.FromArgb(53, 70, 76);

    #endregion Private Fields

    #region Public Methods

    public static void Apply(Button button, bool isLight) {
        var role = ResolveRole(button.Text);
        var colors = ResolveColors(role, isLight);

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = colors.BackColor;
        button.ForeColor = colors.ForeColor;

        if (button.Font is not null) {
            var targetStyle = role is ButtonRole.Save or ButtonRole.Confirm ? FontStyle.Bold : FontStyle.Regular;
            if (button.Font.Style != targetStyle) {
                button.Font = new Font(button.Font.FontFamily, button.Font.Size, targetStyle);
            }
        }

        var iconSize = ResolveIconSize(button);
        var iconName = ResolveIcon(role);

        button.Image?.Dispose();
        button.Image = MaterialIcons.GetIcon(iconName, colors.ForeColor, iconSize);
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.TextAlign = ContentAlignment.MiddleLeft;

        var horizontalPadding = ScaleLogicalPixels(button, 10);
        button.Padding = new Padding(horizontalPadding, 0, horizontalPadding, 0);

        EnsureButtonHasEnoughSpace(button, iconSize);
    }

    #endregion Public Methods

    #region Private Methods

    private static void EnsureButtonHasEnoughSpace(Button button, int iconSize) {
        var text = button.Text ?? string.Empty;
        var textSize = TextRenderer.MeasureText(text, button.Font ?? SystemFonts.DefaultFont);
        var iconGap = ScaleLogicalPixels(button, 8);
        var minWidth = button.Padding.Left + iconSize + iconGap + textSize.Width + button.Padding.Right;
        var minHeight = Math.Max(iconSize + ScaleLogicalPixels(button, 8), textSize.Height + ScaleLogicalPixels(button, 10));

        if (button.Width < minWidth) {
            button.Width = minWidth;
        }

        if (button.Height < minHeight) {
            button.Height = minHeight;
        }
    }

    private static int ResolveIconSize(Button button) {
        // 24 logical px is visually ~2x bigger than the old compact icon set and scales with DPI.
        var scaled = ScaleLogicalPixels(button, 24);
        var maxAllowedByHeight = Math.Max(16, button.Height - ScaleLogicalPixels(button, 10));
        return Math.Clamp(scaled, 16, maxAllowedByHeight);
    }

    private static int ScaleLogicalPixels(Control control, int logicalPixels) {
        var scale = control.DeviceDpi > 0 ? control.DeviceDpi / 96f : 1f;
        return Math.Max(1, (int)Math.Round(logicalPixels * scale));
    }

    private static (Color BackColor, Color ForeColor) ResolveColors(ButtonRole role, bool isLight) => role switch {
        ButtonRole.Save => (PrimaryButtonColor, Color.White),
        ButtonRole.Confirm => (PrimaryButtonColor, Color.White),
        ButtonRole.Cancel => (isLight ? CancelButtonColorLight : CancelButtonColorDark, Color.White),
        ButtonRole.Delete => (isLight ? DeleteButtonColorLight : DeleteButtonColorDark, Color.White),
        _ => (isLight ? SecondaryButtonColorLight : SecondaryButtonColorDark, isLight ? Color.Black : Color.White),
    };

    private static string ResolveIcon(ButtonRole role) => role switch {
        ButtonRole.Save => MaterialIcons.CommonSave,
        ButtonRole.Confirm => MaterialIcons.CommonCheck,
        ButtonRole.Cancel => MaterialIcons.CommonClose,
        ButtonRole.Delete => MaterialIcons.CommonDelete,
        ButtonRole.Add => MaterialIcons.CommonAdd,
        ButtonRole.Edit => MaterialIcons.CommonEdit,
        ButtonRole.Duplicate => MaterialIcons.CommonDuplicate,
        ButtonRole.MoveUp => MaterialIcons.CommonMoveUp,
        ButtonRole.MoveDown => MaterialIcons.CommonMoveDown,
        ButtonRole.Browse => MaterialIcons.CommonBrowse,
        ButtonRole.Resize => MaterialIcons.CommonResize,
        ButtonRole.Test => MaterialIcons.CommonTest,
        _ => MaterialIcons.CommonCheck,
    };

    private static ButtonRole ResolveRole(string? text) {
        var normalized = (text ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.StartsWith("save")) {
            return ButtonRole.Save;
        }

        if (normalized == "ok" || normalized == "yes") {
            return ButtonRole.Confirm;
        }

        if (normalized == "no") {
            return ButtonRole.Cancel;
        }

        if (normalized.StartsWith("cancel")) {
            return ButtonRole.Cancel;
        }

        if (normalized.StartsWith("del")) {
            return ButtonRole.Delete;
        }

        if (normalized.StartsWith("add") || normalized.StartsWith("new")) {
            return ButtonRole.Add;
        }

        if (normalized.StartsWith("edit")) {
            return ButtonRole.Edit;
        }

        if (normalized.StartsWith("copy") || normalized.StartsWith("dup")) {
            return ButtonRole.Duplicate;
        }

        if (normalized.Contains("move up") || normalized.Contains("up")) {
            return ButtonRole.MoveUp;
        }

        if (normalized.Contains("move down") || normalized.Contains("down")) {
            return ButtonRole.MoveDown;
        }

        if (normalized.StartsWith("browse")) {
            return ButtonRole.Browse;
        }

        if (normalized.StartsWith("resize")) {
            return ButtonRole.Resize;
        }

        if (normalized.StartsWith("test")) {
            return ButtonRole.Test;
        }

        return ButtonRole.Secondary;
    }

    #endregion Private Methods

    #region Private Enums

    private enum ButtonRole {
        Secondary,
        Save,
        Confirm,
        Cancel,
        Delete,
        Add,
        Edit,
        Duplicate,
        MoveUp,
        MoveDown,
        Browse,
        Resize,
        Test,
    }

    #endregion Private Enums
}
