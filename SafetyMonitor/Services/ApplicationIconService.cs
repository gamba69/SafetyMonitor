using MaterialSkin;
using SafetyMonitor.Properties;

namespace SafetyMonitor.Services;

internal static class ApplicationIconService {

    #region Public Methods

    public static Icon GetThemeIcon(MaterialSkinManager.Themes theme) {
        var key = $"app_icon::{theme}";
        var cached = HeavyRenderCache.GetBitmap(key);
        if (cached is not null) {
            using var iconFromCache = Icon.FromHandle(cached.GetHicon());
            return (Icon)iconFromCache.Clone();
        }

        var sourceIcon = theme == MaterialSkinManager.Themes.DARK
            ? Resources.AppIconDark
            : Resources.AppIconLight;

        using var iconClone = (Icon)sourceIcon.Clone();
        using var bitmap = iconClone.ToBitmap();
        HeavyRenderCache.PutBitmap(key, bitmap);

        return (Icon)iconClone.Clone();
    }

    #endregion Public Methods
}
