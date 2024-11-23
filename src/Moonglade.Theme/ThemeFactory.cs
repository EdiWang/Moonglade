using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public static class ThemeFactory
{
    public static IEnumerable<BlogThemeEntity> GetSystemThemes()
    {
        return new List<BlogThemeEntity>
        {
            CreateTheme(100, "Word Blue (Default)", "#2a579a", "#1a365f", "#3e6db5"),
            CreateTheme(101, "Excel Green", "#165331", "#0E351F", "#0E703A"),
            CreateTheme(102, "PowerPoint Orange", "#983B22", "#622616", "#C43E1C"),
            CreateTheme(103, "OneNote Purple", "#663276", "#52285E", "#7719AA"),
            CreateTheme(104, "Outlook Blue", "#035AA6", "#032B51", "#006CBF"),
            CreateTheme(105, "Metal Blue", "#4E5967", "#333942", "#6e7c8e"),
            CreateTheme(106, "Mars Green", "#008C8C", "#005e5e", "#17b5b5"),
            CreateTheme(107, "Prussian Blue", "#003153", "#002036", "#0061a5")
        };
    }

    private static BlogThemeEntity CreateTheme(int id, string themeName, string color1, string color2, string color3)
    {
        return new()
        {
            Id = id,
            ThemeName = themeName,
            CssRules = $"{{\"--accent-color1\": \"{color1}\",\"--accent-color2\": \"{color2}\",\"--accent-color3\": \"{color3}\"}}",
            ThemeType = 0
        };
    }
}
