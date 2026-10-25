using Moonglade.Data.Entities;
using System.Text.Json;

namespace Moonglade.Theme.Tests;

public class ThemeFactoryTests
{
    [Fact]
    public void GetSystemThemes_ReturnsUniqueSystemThemesWithValidCssRules()
    {
        var themes = ThemeFactory.GetSystemThemes().ToList();

        Assert.Equal(Enumerable.Range(100, 10), themes.Select(theme => theme.Id));
        Assert.All(themes, theme =>
        {
            Assert.Equal(ThemeType.System, theme.ThemeType);
            Assert.False(string.IsNullOrWhiteSpace(theme.ThemeName));
            var rules = JsonSerializer.Deserialize<Dictionary<string, string>>(theme.CssRules);
            Assert.NotNull(rules);
            Assert.Equal(2, rules.Count);
            Assert.Matches("^#[0-9A-F]{6}$", rules["--accent-color1"]);
            Assert.Matches("^#[0-9A-F]{6}$", rules["--accent-color2"]);
        });
    }

    [Theory]
    [InlineData(100, "Word Blue (Default)", "#2A579A", "#5579AE")]
    [InlineData(101, "Excel Green", "#165331", "#45755A")]
    [InlineData(102, "PowerPoint Orange", "#983B22", "#AD624E")]
    [InlineData(103, "OneNote Purple", "#663276", "#855B91")]
    [InlineData(104, "Outlook Blue", "#035AA6", "#357BB8")]
    [InlineData(105, "Metal Blue", "#4E5967", "#717A85")]
    [InlineData(106, "Mars Green", "#008C8C", "#33A3A3")]
    [InlineData(107, "Prussian Blue", "#003153", "#335A75")]
    [InlineData(108, "Hermes Orange", "#E85827", "#ED7952")]
    [InlineData(109, "Burgundy Red", "#800020", "#99334D")]
    public void GetSystemThemes_ReturnsExpectedThemes(int expectedId, string expectedName, string expectedColor, string expectedLightColor)
    {
        // Act
        var themes = ThemeFactory.GetSystemThemes().ToList();

        // Assert
        var theme = themes.FirstOrDefault(t => t.Id == expectedId);
        Assert.NotNull(theme);
        Assert.Equal(expectedName, theme.ThemeName);
        var rules = JsonSerializer.Deserialize<Dictionary<string, string>>(theme.CssRules);
        Assert.NotNull(rules);
        Assert.Equal(expectedColor, rules["--accent-color1"]);
        Assert.Equal(expectedLightColor, rules["--accent-color2"]);
    }

}
