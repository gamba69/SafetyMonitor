namespace SafetyMonitor.Services;

/// <summary>
/// Represents help text font service and encapsulates its related behavior and state.
/// </summary>
internal static class HelpTextFontService {

    #region Private Constants

    private const float DefaultHelpFontSize = 9f;
    private const float HelpFontSizeDelta = -0.5f;

    #endregion Private Constants

    #region Public Methods

    /// <summary>
    /// Gets the adjusted font size for help and description texts.
    /// </summary>
    /// <param name="baseSize">Base help font size before adjustment.</param>
    /// <returns>The result of the operation.</returns>
    public static float GetAdjustedSize(float baseSize = DefaultHelpFontSize) {
        return baseSize + HelpFontSizeDelta;
    }

    #endregion Public Methods
}
