namespace Moonglade.Theme.Tests;

public class ThemeFactoryTests
{
    #region GetSystemThemes Tests

    [Fact]
    public void GetSystemThemes_ReturnsCorrectNumberOfThemes()
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        Assert.Equal(10, themes.Count);
    }

    [Fact]
    public void GetSystemThemes_ReturnsUniqueIds()
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        var ids = themes.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void GetSystemThemes_ReturnsThemesWithCorrectThemeType()
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        Assert.All(themes, theme => Assert.Equal(0, (int)theme.ThemeType));
    }

    [Fact]
    public void GetSystemThemes_ReturnsThemesWithValidCssRules()
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        Assert.All(themes, theme =>
        {
            Assert.NotNull(theme.CssRules);
            Assert.NotEmpty(theme.CssRules);
            Assert.Contains("--accent-color1", theme.CssRules);
            Assert.Contains("--accent-color2", theme.CssRules);
        });
    }

    [Theory]
    [InlineData(100, "Word Blue (Default)", "#2A579A")]
    [InlineData(101, "Excel Green", "#165331")]
    [InlineData(102, "PowerPoint Orange", "#983B22")]
    [InlineData(103, "OneNote Purple", "#663276")]
    [InlineData(104, "Outlook Blue", "#035AA6")]
    public void GetSystemThemes_ReturnsExpectedThemes(int expectedId, string expectedName, string expectedColor)
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        var theme = themes.FirstOrDefault(t => t.Id == expectedId);
        Assert.NotNull(theme);
        Assert.Equal(expectedName, theme.ThemeName);
        Assert.Contains(expectedColor, theme.CssRules);
    }

    [Fact]
    public void GetSystemThemes_GeneratesValidJsonCssRules()
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        Assert.All(themes, theme =>
        {
            // Verify JSON format
            Assert.StartsWith("{", theme.CssRules);
            Assert.EndsWith("}", theme.CssRules);
            Assert.Contains("\"--accent-color1\":", theme.CssRules);
            Assert.Contains("\"--accent-color2\":", theme.CssRules);
        });
    }

    #endregion

    #region LightenColor Tests

    [Theory]
    [InlineData("#000000", 0.5, "#808080")]
    [InlineData("#FFFFFF", 0.5, "#FFFFFF")]
    [InlineData("#FF0000", 0.2, "#FF3333")]
    [InlineData("#00FF00", 0.2, "#33FF33")]
    [InlineData("#0000FF", 0.2, "#3333FF")]
    public void LightenColor_WithValidHexColor_ReturnsCorrectLightenedColor(string hexColor, double percentage, string expected)
    {
        // Act
        var result = ThemeFactory.LightenColor(hexColor, percentage);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("FF0000")]
    [InlineData("00FF00")]
    public void LightenColor_WithoutHashPrefix_HandlesCorrectly(string hexColor)
    {
        // Act
        var result = ThemeFactory.LightenColor(hexColor, 0.2);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("#", result);
        Assert.Equal(7, result.Length);
    }

    [Theory]
    [InlineData("#2A579A", 0.0)]
    [InlineData("#2A579A", 1.0)]
    [InlineData("#FF0000", 0.0)]
    [InlineData("#FF0000", 1.0)]
    public void LightenColor_WithBoundaryPercentages_ReturnsValidColor(string hexColor, double percentage)
    {
        // Act
        var result = ThemeFactory.LightenColor(hexColor, percentage);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("#", result);
        Assert.Equal(7, result.Length);
    }

    [Fact]
    public void LightenColor_WithZeroPercentage_ReturnsOriginalColor()
    {
        // Arrange
        const string hexColor = "#2A579A";

        // Act
        var result = ThemeFactory.LightenColor(hexColor, 0.0);

        // Assert
        Assert.Equal(hexColor, result);
    }

    [Fact]
    public void LightenColor_WithMaxPercentage_ReturnsWhite()
    {
        // Arrange
        const string hexColor = "#123456";

        // Act
        var result = ThemeFactory.LightenColor(hexColor, 1.0);

        // Assert
        Assert.Equal("#FFFFFF", result);
    }

    [Theory]
    [InlineData("#2A579A", 0.2)]
    [InlineData("#165331", 0.3)]
    [InlineData("#983B22", 0.5)]
    public void LightenColor_ReturnsValidHexFormat(string hexColor, double percentage)
    {
        // Act
        var result = ThemeFactory.LightenColor(hexColor, percentage);

        // Assert
        Assert.Matches(@"^#[0-9A-F]{6}$", result);
    }

    [Theory]
    [InlineData("#2A579A", 0.2)]
    [InlineData("#FF0000", 0.1)]
    [InlineData("#00FF00", 0.3)]
    public void LightenColor_ProducesLighterColor(string hexColor, double percentage)
    {
        // Arrange
        var original = hexColor.TrimStart('#');
        var originalR = Convert.ToInt32(original.Substring(0, 2), 16);
        var originalG = Convert.ToInt32(original.Substring(2, 2), 16);
        var originalB = Convert.ToInt32(original.Substring(4, 2), 16);

        // Act
        var result = ThemeFactory.LightenColor(hexColor, percentage);

        // Assert
        var lightened = result.TrimStart('#');
        var lightenedR = Convert.ToInt32(lightened.Substring(0, 2), 16);
        var lightenedG = Convert.ToInt32(lightened.Substring(2, 2), 16);
        var lightenedB = Convert.ToInt32(lightened.Substring(4, 2), 16);

        if (percentage > 0)
        {
            Assert.True(lightenedR >= originalR);
            Assert.True(lightenedG >= originalG);
            Assert.True(lightenedB >= originalB);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("GGGGGG")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    public void LightenColor_WithInvalidHexColor_ThrowsException(string invalidHexColor)
    {
        // Act & Assert
        Assert.ThrowsAny<Exception>(() => ThemeFactory.LightenColor(invalidHexColor, 0.2));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    public void LightenColor_WithInvalidPercentage_StillReturnsValidColor(double invalidPercentage)
    {
        // Arrange
        const string hexColor = "#2A579A";

        // Act
        var result = ThemeFactory.LightenColor(hexColor, invalidPercentage);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("#", result);
        Assert.Equal(7, result.Length);
        Assert.Matches(@"^#[0-9A-F]{6}$", result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GetSystemThemes_UsesLightenColorCorrectly()
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        foreach (var theme in themes)
        {
            // Extract colors from CSS rules
            var cssRules = theme.CssRules;
            var color1Start = cssRules.IndexOf("\"--accent-color1\": \"") + 20;
            var color1End = cssRules.IndexOf("\"", color1Start);
            var color1 = cssRules[color1Start..color1End];

            var color2Start = cssRules.IndexOf("\"--accent-color2\": \"") + 20;
            var color2End = cssRules.IndexOf("\"", color2Start);
            var color2 = cssRules[color2Start..color2End];

            // Verify color2 is lightened version of color1
            var expectedColor2 = ThemeFactory.LightenColor(color1, 0.2);
            Assert.Equal(expectedColor2, color2);
        }
    }

    [Fact]
    public void GetSystemThemes_ReturnsConsistentResults()
    {
        // Act
        var themes1 = ThemeFactory.GetSystemThemes().ToList();
        var themes2 = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        Assert.Equal(themes1.Count, themes2.Count);

        for (int i = 0; i < themes1.Count; i++)
        {
            Assert.Equal(themes1[i].Id, themes2[i].Id);
            Assert.Equal(themes1[i].ThemeName, themes2[i].ThemeName);
            Assert.Equal(themes1[i].CssRules, themes2[i].CssRules);
            Assert.Equal(themes1[i].ThemeType, themes2[i].ThemeType);
        }
    }

    #endregion
}