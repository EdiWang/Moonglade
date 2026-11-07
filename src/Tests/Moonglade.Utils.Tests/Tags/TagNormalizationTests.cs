namespace Moonglade.Utils.Tests.Tags;

public class TagNormalizationTests
{
    [Theory]
    [InlineData("csharp", "csharp")]
    [InlineData("C#", "c-sharp")]
    [InlineData("ASP.NET", "asp-net")]
    [InlineData("F#", "f-sharp")]
    [InlineData("C++", "c-plus-plus")]
    [InlineData("Entity Framework", "entity-framework")]
    [InlineData("JAVA SCRIPT", "java-script")]
    [InlineData("Node.js", "node-js")]
    [InlineData(".NET", "dot-net")]
    [InlineData(".net", "dot-net")]
    [InlineData(".Net", "dot-net")]
    [InlineData("HTML5 CSS3.0", "html5-css3-0")]
    public void NormalizeName_WithEnglishTags_NormalizesCorrectly(string input, string expected)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "---")]
    public void NormalizeName_WithEmptyOrWhitespaceInput_NormalizesSpaces(string input, string expected)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeName_WithNullNormalizations_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            BlogTagHelper.NormalizeName("test", null!));
    }

    [Theory]
    [InlineData(".NET Framework", "dot-net-framework")]
    [InlineData(".net core", "dot-net-core")]
    [InlineData(".Net Standard", "dot-net-standard")]
    public void NormalizeName_DotNetVariations_NormalizesWholeName(string input, string expected)
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act
        var result = BlogTagHelper.NormalizeName(input, normalizations);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeName_WithNullInput_ThrowsException()
    {
        // Arrange
        var normalizations = BlogTagHelper.TagNormalizationDictionary;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BlogTagHelper.NormalizeName(null!, normalizations));
    }

    [Fact]
    public void NormalizeName_WithEmptyDictionary_HandlesEnglishTagsCorrectly()
    {
        // Arrange
        var emptyDict = new Dictionary<string, string>();

        // Act
        var result = BlogTagHelper.NormalizeName("Simple Tag", emptyDict);

        // Assert
        Assert.Equal("simple tag", result);
    }

    [Theory]
    [InlineData("编程", "16-7f-0b-7a")]
    [InlineData("プログラミング", "d7-30-ed-30-b0-30-e9-30-df-30-f3-30-b0-30")]
    [InlineData("C#编程", "43-00-23-00-16-7f-0b-7a")]
    public void NormalizeName_WithNonEnglishCharacters_ReturnsUtf16HexSlug(string input, string expected)
    {
        var result = BlogTagHelper.NormalizeName(input, BlogTagHelper.TagNormalizationDictionary);

        Assert.Equal(expected, result);
    }
}
