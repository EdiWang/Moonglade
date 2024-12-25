using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public static class ThemeFactory
{
    public static IEnumerable<BlogThemeEntity> GetSystemThemes()
    {
        return new List<BlogThemeEntity>
        {
            CreateTheme(100, "Word Blue (Default)", "#2A579A", "#5478AE"),
            CreateTheme(101, "Excel Green", "#165331",  "#44755A"),
            CreateTheme(102, "PowerPoint Orange", "#983B22",  "#AC624E"),
            CreateTheme(103, "OneNote Purple", "#663276",  "#845B91"),
            CreateTheme(104, "Outlook Blue", "#035AA6",  "#357BB7"),
            CreateTheme(105, "Metal Blue", "#4E5967",  "#717A85"),
            CreateTheme(106, "Mars Green", "#008C8C", "#33A3A3"),
            CreateTheme(107, "Prussian Blue", "#003153",  "#335A75")
        };
    }

    private static BlogThemeEntity CreateTheme(int id, string themeName, string color1, string color2)
    {
        return new()
        {
            Id = id,
            ThemeName = themeName,
            CssRules = $"{{\"--accent-color1\": \"{color1}\",\"--accent-color2\": \"{color2}\"}}",
            ThemeType = 0
        };
    }
}
