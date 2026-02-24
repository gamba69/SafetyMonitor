using MaterialSkin;
using SafetyMonitorView.Properties;

namespace SafetyMonitorView.Services;

internal static class ApplicationIconService {

    #region Public Methods

    public static Icon GetThemeIcon(MaterialSkinManager.Themes theme) {
        var sourceIcon = theme == MaterialSkinManager.Themes.DARK
            ? Resources.AppIconDark
            : Resources.AppIconLight;

        return (Icon)sourceIcon.Clone();
    }

    #endregion Public Methods
}
