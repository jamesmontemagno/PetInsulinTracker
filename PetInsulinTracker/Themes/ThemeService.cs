namespace PetInsulinTracker.Themes;

public enum AppTheme
{
    Warm,
    Ocean,
    Forest,
    Berry,
    Midnight
}

public record ThemePalette(
    Color Primary, Color PrimaryDark,
    Color Secondary, Color SecondaryDark,
    Color Tertiary, Color TertiaryDark,
    Color Surface, Color SurfaceDark,
    Color Background, Color BackgroundDark,
    Color CardBackground, Color CardBackgroundDark,
    Color TextPrimary, Color TextPrimaryDark,
    Color TextSecondary, Color TextSecondaryDark,
    Color ShellBackground, Color ShellBackgroundDark,
    Color Divider, Color DividerDark);

public static class ThemeService
{
    private static readonly Dictionary<AppTheme, ThemePalette> _palettes = new()
    {
        [AppTheme.Warm] = new(
            Primary: Color.FromArgb("#E8910C"), PrimaryDark: Color.FromArgb("#F5A623"),
            Secondary: Color.FromArgb("#F28B6E"), SecondaryDark: Color.FromArgb("#F2A68B"),
            Tertiary: Color.FromArgb("#5B9A6F"), TertiaryDark: Color.FromArgb("#6BAD7F"),
            Surface: Color.FromArgb("#FFF8F0"), SurfaceDark: Color.FromArgb("#2A2018"),
            Background: Color.FromArgb("#FDF5EC"), BackgroundDark: Color.FromArgb("#1A1410"),
            CardBackground: Color.FromArgb("#FFFFFF"), CardBackgroundDark: Color.FromArgb("#332820"),
            TextPrimary: Color.FromArgb("#3D2C1E"), TextPrimaryDark: Color.FromArgb("#F5E6D3"),
            TextSecondary: Color.FromArgb("#8C7B6B"), TextSecondaryDark: Color.FromArgb("#A89580"),
            ShellBackground: Color.FromArgb("#4A3728"), ShellBackgroundDark: Color.FromArgb("#151010"),
            Divider: Color.FromArgb("#E8DDD0"), DividerDark: Color.FromArgb("#3D3228")),

        [AppTheme.Ocean] = new(
            Primary: Color.FromArgb("#0288D1"), PrimaryDark: Color.FromArgb("#29B6F6"),
            Secondary: Color.FromArgb("#26C6DA"), SecondaryDark: Color.FromArgb("#4DD0E1"),
            Tertiary: Color.FromArgb("#00897B"), TertiaryDark: Color.FromArgb("#26A69A"),
            Surface: Color.FromArgb("#F0F7FA"), SurfaceDark: Color.FromArgb("#1A2530"),
            Background: Color.FromArgb("#E8F4F8"), BackgroundDark: Color.FromArgb("#121D28"),
            CardBackground: Color.FromArgb("#FFFFFF"), CardBackgroundDark: Color.FromArgb("#1E2D3D"),
            TextPrimary: Color.FromArgb("#1A3A4A"), TextPrimaryDark: Color.FromArgb("#D6EAF2"),
            TextSecondary: Color.FromArgb("#6B8A9A"), TextSecondaryDark: Color.FromArgb("#8AAAB8"),
            ShellBackground: Color.FromArgb("#01579B"), ShellBackgroundDark: Color.FromArgb("#0A1520"),
            Divider: Color.FromArgb("#D0E4ED"), DividerDark: Color.FromArgb("#253545")),

        [AppTheme.Forest] = new(
            Primary: Color.FromArgb("#2E7D32"), PrimaryDark: Color.FromArgb("#66BB6A"),
            Secondary: Color.FromArgb("#8D6E63"), SecondaryDark: Color.FromArgb("#A1887F"),
            Tertiary: Color.FromArgb("#FFA000"), TertiaryDark: Color.FromArgb("#FFB74D"),
            Surface: Color.FromArgb("#F1F8E9"), SurfaceDark: Color.FromArgb("#1A2518"),
            Background: Color.FromArgb("#E8F0E0"), BackgroundDark: Color.FromArgb("#121A10"),
            CardBackground: Color.FromArgb("#FFFFFF"), CardBackgroundDark: Color.FromArgb("#243020"),
            TextPrimary: Color.FromArgb("#1B3A1E"), TextPrimaryDark: Color.FromArgb("#D5E8D0"),
            TextSecondary: Color.FromArgb("#5A7A5D"), TextSecondaryDark: Color.FromArgb("#8AAD8D"),
            ShellBackground: Color.FromArgb("#1B5E20"), ShellBackgroundDark: Color.FromArgb("#0A1508"),
            Divider: Color.FromArgb("#C8DCC0"), DividerDark: Color.FromArgb("#2D3D28")),

        [AppTheme.Berry] = new(
            Primary: Color.FromArgb("#AD1457"), PrimaryDark: Color.FromArgb("#EC407A"),
            Secondary: Color.FromArgb("#7B1FA2"), SecondaryDark: Color.FromArgb("#AB47BC"),
            Tertiary: Color.FromArgb("#F4511E"), TertiaryDark: Color.FromArgb("#FF7043"),
            Surface: Color.FromArgb("#FFF0F5"), SurfaceDark: Color.FromArgb("#2A1820"),
            Background: Color.FromArgb("#FCE4EC"), BackgroundDark: Color.FromArgb("#1A1015"),
            CardBackground: Color.FromArgb("#FFFFFF"), CardBackgroundDark: Color.FromArgb("#352028"),
            TextPrimary: Color.FromArgb("#3D1A28"), TextPrimaryDark: Color.FromArgb("#F2D6E0"),
            TextSecondary: Color.FromArgb("#8C6B78"), TextSecondaryDark: Color.FromArgb("#B8909D"),
            ShellBackground: Color.FromArgb("#880E4F"), ShellBackgroundDark: Color.FromArgb("#150810"),
            Divider: Color.FromArgb("#E8D0D8"), DividerDark: Color.FromArgb("#3D2830")),

        [AppTheme.Midnight] = new(
            Primary: Color.FromArgb("#5C6BC0"), PrimaryDark: Color.FromArgb("#7986CB"),
            Secondary: Color.FromArgb("#26C6DA"), SecondaryDark: Color.FromArgb("#4DD0E1"),
            Tertiary: Color.FromArgb("#FFCA28"), TertiaryDark: Color.FromArgb("#FFD54F"),
            Surface: Color.FromArgb("#ECEFF1"), SurfaceDark: Color.FromArgb("#1E2228"),
            Background: Color.FromArgb("#E8EAF6"), BackgroundDark: Color.FromArgb("#10131A"),
            CardBackground: Color.FromArgb("#FFFFFF"), CardBackgroundDark: Color.FromArgb("#1A2030"),
            TextPrimary: Color.FromArgb("#1A1F3D"), TextPrimaryDark: Color.FromArgb("#D6DAF0"),
            TextSecondary: Color.FromArgb("#6B708C"), TextSecondaryDark: Color.FromArgb("#9095B0"),
            ShellBackground: Color.FromArgb("#283593"), ShellBackgroundDark: Color.FromArgb("#080A18"),
            Divider: Color.FromArgb("#D0D4E0"), DividerDark: Color.FromArgb("#252A38"))
    };

    public static AppTheme CurrentTheme
    {
        get
        {
            var saved = Preferences.Get("app_theme", nameof(AppTheme.Berry));
            return Enum.TryParse<AppTheme>(saved, out var t) ? t : AppTheme.Berry;
        }
    }

    public static ThemePalette GetPalette(AppTheme theme) => _palettes[theme];

    public static void ApplyTheme(AppTheme theme)
    {
        Preferences.Set("app_theme", theme.ToString());
        var p = _palettes[theme];
        var res = Application.Current?.Resources;
        if (res is null) return;

        // Detect current system appearance
        var isDark = Application.Current!.RequestedTheme == Microsoft.Maui.ApplicationModel.AppTheme.Dark;

        // Set both light/dark variants (for AppThemeBinding) 
        SetColor(res, "Primary", p.Primary);
        SetColor(res, "PrimaryDark", p.PrimaryDark);
        SetColor(res, "Secondary", p.Secondary);
        SetColor(res, "SecondaryDark", p.SecondaryDark);
        SetColor(res, "Tertiary", p.Tertiary);
        SetColor(res, "TertiaryDark", p.TertiaryDark);
        SetColor(res, "Surface", p.Surface);
        SetColor(res, "SurfaceDark", p.SurfaceDark);
        SetColor(res, "Background", isDark ? p.BackgroundDark : p.Background);
        SetColor(res, "BackgroundDark", p.BackgroundDark);
        SetColor(res, "CardBackground", p.CardBackground);
        SetColor(res, "CardBackgroundDark", p.CardBackgroundDark);
        SetColor(res, "TextPrimary", p.TextPrimary);
        SetColor(res, "TextPrimaryDark", p.TextPrimaryDark);
        SetColor(res, "TextSecondary", p.TextSecondary);
        SetColor(res, "TextSecondaryDark", p.TextSecondaryDark);
        SetColor(res, "ShellBackground", p.ShellBackground);
        SetColor(res, "ShellBackgroundDark", p.ShellBackgroundDark);
        SetColor(res, "Divider", p.Divider);
        SetColor(res, "DividerDark", p.DividerDark);

        // Set resolved "current" keys for DynamicResource usage
        SetColor(res, "PageBackground", isDark ? p.BackgroundDark : p.Background);
        SetColor(res, "PageText", isDark ? p.TextPrimaryDark : p.TextPrimary);
        SetColor(res, "PageTextSecondary", isDark ? p.TextSecondaryDark : p.TextSecondary);
        SetColor(res, "CurrentPrimary", isDark ? p.PrimaryDark : p.Primary);
        SetColor(res, "CurrentCardBackground", isDark ? p.CardBackgroundDark : p.CardBackground);
        SetColor(res, "CurrentSurface", isDark ? p.SurfaceDark : p.Surface);
        SetColor(res, "CurrentDivider", isDark ? p.DividerDark : p.Divider);
        SetColor(res, "CurrentShellBackground", isDark ? p.ShellBackgroundDark : p.ShellBackground);

        // Non-palette resolved keys (same across all themes, just light/dark variant)
        var danger = isDark ? Color.FromArgb("#E86565") : Color.FromArgb("#D94F4F");
        var success = isDark ? Color.FromArgb("#6BAD7F") : Color.FromArgb("#5B9A6F");
        var shellFg = isDark ? Color.FromArgb("#F5E6D3") : Color.FromArgb("#FFFFFF");
        SetColor(res, "CurrentDanger", danger);
        SetColor(res, "CurrentSuccess", success);
        SetColor(res, "CurrentSecondary", isDark ? p.SecondaryDark : p.Secondary);
        SetColor(res, "CurrentShellForeground", shellFg);

        // Also update legacy keys that derive from primary
        SetColor(res, "Magenta", p.Primary);
        SetColor(res, "Warning", p.Primary);
    }

    private static void SetColor(ResourceDictionary resources, string key, Color color)
    {
        // DynamicResource only reacts to changes on the top-level ResourceDictionary,
        // so always set there to ensure bindings update.
        resources[key] = color;
    }

    public static string GetThemeDisplayName(AppTheme theme) => theme switch
    {
        AppTheme.Warm => "Warm & Earthy",
        AppTheme.Ocean => "Ocean Breeze",
        AppTheme.Forest => "Forest Walk",
        AppTheme.Berry => "Berry Bliss",
        AppTheme.Midnight => "Midnight Indigo",
        _ => theme.ToString()
    };
}
