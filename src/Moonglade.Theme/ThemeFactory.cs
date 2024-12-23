using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public static class ThemeFactory
{
    public static IEnumerable<BlogThemeEntity> GetSystemThemes()
    {
        return new List<BlogThemeEntity>
        {
            CreateTheme(100, "Word Blue (Default)", "#2a579a", "#3e6db5"),
            CreateTheme(101, "Excel Green", "#165331",  "#0E703A"),
            CreateTheme(102, "PowerPoint Orange", "#983B22",  "#C43E1C"),
            CreateTheme(103, "OneNote Purple", "#663276",  "#7719AA"),
            CreateTheme(104, "Outlook Blue", "#035AA6",  "#006CBF"),
            CreateTheme(105, "Metal Blue", "#4E5967",  "#6e7c8e"),
            CreateTheme(106, "Mars Green", "#008C8C", "#17b5b5"),
            CreateTheme(107, "Prussian Blue", "#003153",  "#0061a5")
        };
    }

    private static BlogThemeEntity CreateTheme(int id, string themeName, string color1, string color3)
    {
        return new()
        {
            Id = id,
            ThemeName = themeName,
            CssRules = $"{{\"--accent-color1\": \"{color1}\",\"--accent-color3\": \"{color3}\"}}",
            ThemeType = 0
        };
    }
}
