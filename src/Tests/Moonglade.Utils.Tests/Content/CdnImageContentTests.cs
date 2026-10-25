using System.Text;

namespace Moonglade.Utils.Tests.Content;

public class CdnImageContentTests
{
    [Theory]
    [InlineData(null, "https://cdn.example.com", null)]
    [InlineData("", "https://cdn.example.com", "")]
    [InlineData("   ", "https://cdn.example.com", "   ")]
    public void ReplaceCDNEndpointToImgTags_WithNullOrEmptyHtml_ReturnsOriginalValue(string html, string endpoint, string expected)
    {
        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_WithImageSrcStartingWithImage_ReplacesCorrectly()
    {
        // Arrange
        const string html = "<img src=\"/image/test.jpg\" alt=\"test\">";
        const string endpoint = "https://cdn.example.com";
        const string expected = "<img src=\"https://cdn.example.com/test.jpg\" alt=\"test\">";

        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_WithEndpointHavingTrailingSlash_RemovesTrailingSlash()
    {
        // Arrange
        const string html = "<img src=\"/image/test.jpg\" alt=\"test\">";
        const string endpoint = "https://cdn.example.com/";
        const string expected = "<img src=\"https://cdn.example.com/test.jpg\" alt=\"test\">";

        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_WithImageSrcNotStartingWithImage_LeavesUnchanged()
    {
        // Arrange
        const string html = "<img src=\"/assets/test.jpg\" alt=\"test\">";
        const string endpoint = "https://cdn.example.com";

        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(html, result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_WithMultipleImages_ReplacesOnlyImagePaths()
    {
        // Arrange
        const string html = "<img src=\"/image/test1.jpg\" alt=\"test1\"><img src=\"/assets/test2.jpg\" alt=\"test2\"><img src=\"/image/test3.png\" alt=\"test3\">";
        const string endpoint = "https://cdn.example.com";
        const string expected = "<img src=\"https://cdn.example.com/test1.jpg\" alt=\"test1\"><img src=\"/assets/test2.jpg\" alt=\"test2\"><img src=\"https://cdn.example.com/test3.png\" alt=\"test3\">";

        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_WithSingleQuotes_ReplacesCorrectly()
    {
        // Arrange
        const string html = "<img src='/image/test.jpg' alt='test'>";
        const string endpoint = "https://cdn.example.com";
        const string expected = "<img src='https://cdn.example.com/test.jpg' alt='test'>";

        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_AfterMarkdownToContent_ReplacesMarkdownImageHtmlCorrectly()
    {
        // Arrange
        const string markdown = "![test](/image/test.jpg)";
        const string endpoint = "https://cdn.example.com";

        // Act
        var html = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html, false);
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Contains("src=\"https://cdn.example.com/test.jpg\"", result);
        Assert.Contains("alt=\"test\"", result);
        Assert.DoesNotContain("src=\"/image/test.jpg\"", result);
    }

    [Fact]
    public void ReplaceCDNEndpointToImgTags_WithComplexHtml_WorksCorrectly()
    {
        // Arrange
        var html = new StringBuilder()
            .Append("<div class=\"content\">")
            .Append("<p>Some text</p>")
            .Append("<img src=\"/image/photo1.jpg\" alt=\"Photo 1\" class=\"img-fluid\">")
            .Append("<p>More text</p>")
            .Append("<img src=\"/assets/icon.png\" alt=\"Icon\">")
            .Append("<img src=\"/image/photo2.png\" alt=\"Photo 2\">")
            .Append("</div>")
            .ToString();

        const string endpoint = "https://cdn.example.com";

        var expected = new StringBuilder()
            .Append("<div class=\"content\">")
            .Append("<p>Some text</p>")
            .Append("<img src=\"https://cdn.example.com/photo1.jpg\" alt=\"Photo 1\" class=\"img-fluid\">")
            .Append("<p>More text</p>")
            .Append("<img src=\"/assets/icon.png\" alt=\"Icon\">")
            .Append("<img src=\"https://cdn.example.com/photo2.png\" alt=\"Photo 2\">")
            .Append("</div>")
            .ToString();

        // Act
        var result = html.ReplaceCDNEndpointToImgTags(endpoint);

        // Assert
        Assert.Equal(expected, result);
    }
}
