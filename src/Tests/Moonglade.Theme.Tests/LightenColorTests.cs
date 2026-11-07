namespace Moonglade.Theme.Tests;

public class LightenColorTests
{
    [Theory]
    [InlineData("#000000", 0.5, "#808080")]
    [InlineData("#FFFFFF", 0.5, "#FFFFFF")]
    [InlineData("#FF0000", 0.2, "#FF3333")]
    [InlineData("#00FF00", 0.2, "#33FF33")]
    [InlineData("#0000FF", 0.2, "#3333FF")]
    [InlineData("#2A579A", 0.0, "#2A579A")]
    [InlineData("#2A579A", 1.0, "#FFFFFF")]
    [InlineData("#FF0000", 0.0, "#FF0000")]
    [InlineData("#FF0000", 1.0, "#FFFFFF")]
    [InlineData("#123456", 1.0, "#FFFFFF")]
    [InlineData("000000", 0.2, "#333333")]
    [InlineData("FF0000", 0.2, "#FF3333")]
    [InlineData("00FF00", 0.2, "#33FF33")]
    public void LightenColor_WithValidHexColor_ReturnsCorrectLightenedColor(string hexColor, double percentage, string expected)
    {
        // Act
        var result = ThemeFactory.LightenColor(hexColor, percentage);

        // Assert
        Assert.Equal(expected, result);
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
        Assert.Throws<ArgumentException>(() => ThemeFactory.LightenColor(invalidHexColor, 0.2));
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

}
