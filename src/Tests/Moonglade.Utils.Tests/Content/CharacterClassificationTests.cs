namespace Moonglade.Utils.Tests.Content;

public class CharacterClassificationTests
{
    [Theory]
    [InlineData('a', true)]
    [InlineData('Z', true)]
    [InlineData('m', true)]
    [InlineData('1', false)]
    [InlineData(' ', false)]
    [InlineData('!', false)]
    [InlineData('ñ', false)] // Non-ASCII letter
    [InlineData('3', false)]
    [InlineData('_', false)]
    public void IsLetter_WithVariousCharacters_ReturnsExpectedResult(char c, bool expected)
    {
        // Act
        var result = c.IsLetter();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(' ', true)]
    [InlineData('\t', true)]
    [InlineData('\n', true)]
    [InlineData('\r', true)]
    [InlineData('\f', true)]
    [InlineData('a', false)]
    [InlineData('1', false)]
    [InlineData('!', false)]
    public void IsSpace_WithVariousCharacters_ReturnsExpectedResult(char c, bool expected)
    {
        // Act
        var result = c.IsSpace();

        // Assert
        Assert.Equal(expected, result);
    }
}
