using MaterialSkin;
using ColorScheme = MaterialSkin.ColorScheme;

namespace SafetyMonitorView.Services;

public sealed class AppColorizationService {
    #region Private Fields

    private static readonly Dictionary<string, MaterialPaletteDefinition> MaterialPalettes = new(StringComparer.OrdinalIgnoreCase) {
        ["Teal"] = new("Teal", Primary.Teal700, Primary.Teal900, Primary.Teal500, Accent.Teal200),
        ["BlueGray"] = new("BlueGray", Primary.BlueGrey700, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200),
        ["Amber"] = new("Amber", Primary.Amber700, Primary.Amber900, Primary.Amber500, Accent.Amber200),
        ["Brown"] = new("Brown", Primary.Brown700, Primary.Brown900, Primary.Brown500, Accent.LightBlue200),
        ["DeepOrange"] = new("DeepOrange", Primary.DeepOrange700, Primary.DeepOrange900, Primary.DeepOrange500, Accent.DeepOrange200),
        ["Indigo"] = new("Indigo", Primary.Indigo700, Primary.Indigo900, Primary.Indigo500, Accent.Indigo200),
        ["Red"] = new("Red", Primary.Red700, Primary.Red900, Primary.Red500, Accent.Red200),
        ["Yellow"] = new("Yellow", Primary.Yellow700, Primary.Yellow900, Primary.Yellow500, Accent.Yellow200)
    };

    #endregion Private Fields

    #region Public Properties

    public static AppColorizationService Instance { get; } = new();

    public IReadOnlyList<string> AvailableMaterialSchemes => MaterialPalettes.Keys.OrderBy(static n => n).ToList();

    #endregion Public Properties

    #region Public Methods

    public string NormalizeMaterialSchemeName(string? schemeName) {
        if (string.IsNullOrWhiteSpace(schemeName)) {
            return "Teal";
        }

        return MaterialPalettes.ContainsKey(schemeName)
            ? schemeName
            : "Teal";
    }

    public ColorScheme GetMaterialColorScheme(string? schemeName) {
        var normalizedName = NormalizeMaterialSchemeName(schemeName);
        var palette = MaterialPalettes[normalizedName];
        return new ColorScheme(palette.Primary, palette.DarkPrimary, palette.LightPrimary, palette.Accent, TextShade.WHITE);
    }

    public Color GetPrimaryActionColor(string? schemeName) {
        return GetMaterialColorScheme(schemeName).PrimaryColor;
    }


    public Color GetPrimaryActionTextColor(string? schemeName) {
        var background = GetPrimaryActionColor(schemeName);
        return GetReadableTextColor(background);
    }

    public Color GetReadableTextColor(Color background) {
        var luminance = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
        return luminance >= 160 ? Color.Black : Color.White;
    }

    public ThemeNeutralPalette GetNeutralPalette(bool isLightTheme) {
        return isLightTheme
            ? new ThemeNeutralPalette(
                Color.FromArgb(250, 250, 250),
                Color.FromArgb(245, 245, 245),
                Color.White,
                Color.FromArgb(225, 232, 235),
                Color.FromArgb(196, 206, 211),
                Color.FromArgb(33, 33, 33),
                Color.Black)
            : new ThemeNeutralPalette(
                Color.FromArgb(38, 52, 57),
                Color.FromArgb(35, 47, 52),
                Color.FromArgb(46, 61, 66),
                Color.FromArgb(45, 58, 64),
                Color.FromArgb(70, 85, 92),
                Color.FromArgb(240, 240, 240),
                Color.White);
    }

    #endregion Public Methods

    #region Private Constructors

    private AppColorizationService() {
    }

    #endregion Private Constructors
}

public sealed record MaterialPaletteDefinition(string Name, Primary Primary, Primary DarkPrimary, Primary LightPrimary, Accent Accent);

public sealed record ThemeNeutralPalette(
    Color FormBackground,
    Color SurfaceBackground,
    Color InputBackground,
    Color SegmentBackground,
    Color Border,
    Color Text,
    Color StrongText);
