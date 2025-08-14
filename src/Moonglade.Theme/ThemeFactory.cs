using Moonglade.Data.Entities;

namespace Moonglade.Theme;

public static class ThemeFactory
{
    public static IEnumerable<BlogThemeEntity> GetSystemThemes() => [
        CreateTheme(100, "Word Blue (Default)", "#2A579A"),
        CreateTheme(101, "Excel Green", "#165331"),
        CreateTheme(102, "PowerPoint Orange", "#983B22"),
        CreateTheme(103, "OneNote Purple", "#663276"),
        CreateTheme(104, "Outlook Blue", "#035AA6"),
        CreateTheme(105, "Metal Blue", "#4E5967"),
        CreateTheme(106, "Mars Green", "#008C8C"),
        CreateTheme(107, "Prussian Blue", "#003153"),
        CreateTheme(108, "Hermes Orange", "#E85827"),
        CreateTheme(109, "Burgundy Red", "#800020"),
    ];

    private static BlogThemeEntity CreateTheme(int id, string themeName, string color1)
    {
        var color2 = LightenColor(color1, 0.2);

        return new()
        {
            Id = id,
            ThemeName = themeName,
            CssRules = $"{{\"--accent-color1\": \"{color1}\",\"--accent-color2\": \"{color2}\"}}",
            ThemeType = 0
        };
    }

    public static string LightenColor(string hexColor, double percentage)
    {
        // Remove '#' and parse the color into RGB components
        hexColor = hexColor.TrimStart('#');
        int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
        int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
        int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);

        // Calculate the new RGB values
        // - Use the formula:
        //   \[
        //   C_{\text{ new} } = C_{\text{ original} }
        //      +(255 - C_{\text{ original} }) \times \text{ percentage}
        //   \]
        // - This moves each color channel closer to 255(white) by the specified percentage.
        int rNew = (int)(r + (255 - r) * percentage);
        int gNew = (int)(g + (255 - g) * percentage);
        int bNew = (int)(b + (255 - b) * percentage);

        // Ensure the values are within the valid range (0-255)
        rNew = Math.Clamp(rNew, 0, 255);
        gNew = Math.Clamp(gNew, 0, 255);
        bNew = Math.Clamp(bNew, 0, 255);

        // Convert back to HEX format and return
        return $"#{rNew:X2}{gNew:X2}{bNew:X2}";
    }
}
