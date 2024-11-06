using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public static class ThemeFactory
{
    public static IEnumerable<BlogThemeEntity> GetSystemThemes()
    {
        return new List<BlogThemeEntity>
        {
            CreateTheme("Word Blue", "#2a579a", "#1a365f", "#3e6db5"),
            CreateTheme("Excel Green", "#165331", "#0E351F", "#0E703A"),
            CreateTheme("PowerPoint Orange", "#983B22", "#622616", "#C43E1C"),
            CreateTheme("OneNote Purple", "#663276", "#52285E", "#7719AA"),
            CreateTheme("Outlook Blue", "#035AA6", "#032B51", "#006CBF"),
            CreateTheme("Metal Blue", "#4E5967", "#333942", "#6e7c8e")
        };
    }

    private static BlogThemeEntity CreateTheme(string themeName, string color1, string color2, string color3)
    {
        return new()
        {
            ThemeName = themeName,
            CssRules = $"{{\"--accent-color1\": \"{color1}\",\"--accent-color2\": \"{color2}\",\"--accent-color3\": \"{color3}\"}}",
            ThemeType = 0
        };
    }
}
