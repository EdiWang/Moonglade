using System.Text;

namespace Moonglade.Utils.Tests;

public class ContentProcessorTests
{
    #region ReplaceCDNEndpointToImgTags Tests

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

    #endregion

    #region GetPostAbstract Tests

    [Theory]
    [InlineData("", 10, false, "")]
    //[InlineData("Simple text content", 5, false, "Simple text content\u00A0\u2026")]
    //[InlineData("Simple text content", 20, false, "Simple text content")]
    public void GetPostAbstract_WithPlainText_ReturnsExpectedResult(string content, int wordCount, bool useMarkdown, string expected)
    {
        // Act
        var result = ContentProcessor.GetPostAbstract(content, wordCount, useMarkdown);

        // Assert
        Assert.Equal(expected, result);
    }

    //[Fact]
    //public void GetPostAbstract_WithHtmlContent_RemovesTags()
    //{
    //    // Arrange
    //    const string content = "<p>This is <strong>bold</strong> text</p>";
    //    const int wordCount = 10;

    //    // Act
    //    var result = ContentProcessor.GetPostAbstract(content, wordCount, false);

    //    // Assert
    //    Assert.Equal("This is bold text", result);
    //}

    //[Fact]
    //public void GetPostAbstract_WithMarkdownContent_ConvertsToPlainText()
    //{
    //    // Arrange
    //    const string content = "# Heading\n\nThis is **bold** text with a [link](http://example.com).";
    //    const int wordCount = 20;

    //    // Act
    //    var result = ContentProcessor.GetPostAbstract(content, wordCount, true);

    //    // Assert
    //    Assert.Contains("Heading", result);
    //    Assert.Contains("This is bold text", result);
    //    Assert.DoesNotContain("#", result);
    //    Assert.DoesNotContain("**", result);
    //    Assert.DoesNotContain("[", result);
    //    Assert.DoesNotContain("](", result);
    //}

    //[Fact]
    //public void GetPostAbstract_WithHtmlEncodedContent_DecodesHtml()
    //{
    //    // Arrange
    //    const string content = "This &amp; that &lt;tag&gt;";
    //    const int wordCount = 10;

    //    // Act
    //    var result = ContentProcessor.GetPostAbstract(content, wordCount, false);

    //    // Assert
    //    Assert.Equal("This & that <tag>", result);
    //}

    #endregion

    #region HtmlDecode Tests

    [Theory]
    [InlineData("", "")]
    [InlineData("Plain text", "Plain text")]
    [InlineData("&amp;", "&")]
    [InlineData("&lt;tag&gt;", "<tag>")]
    [InlineData("&quot;quoted&quot;", "\"quoted\"")]
    [InlineData("&#39;apostrophe&#39;", "'apostrophe'")]
    [InlineData("&nbsp;", "\u00A0")]
    public void HtmlDecode_WithVariousEncodedContent_DecodesCorrectly(string content, string expected)
    {
        // Act
        var result = ContentProcessor.HtmlDecode(content);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void HtmlDecode_WithNullContent_ReturnsNull()
    {
        // Act
        var result = ContentProcessor.HtmlDecode(null);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region RemoveTags Tests

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void RemoveTags_WithNullOrEmpty_ReturnsEmpty(string html, string expected)
    {
        // Act
        var result = ContentProcessor.RemoveTags(html);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveTags_WithValidHtml_RemovesTagsAndReturnsText()
    {
        // Arrange
        const string html = "<p>This is <strong>bold</strong> and <em>italic</em> text.</p>";
        const string expected = "This is bold and italic text.";

        // Act
        var result = ContentProcessor.RemoveTags(html);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveTags_WithNestedTags_RemovesAllTags()
    {
        // Arrange
        const string html = "<div><p>Paragraph with <span><strong>nested</strong></span> tags</p></div>";
        const string expected = "Paragraph with nested tags";

        // Act
        var result = ContentProcessor.RemoveTags(html);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveTags_WithSelfClosingTags_HandlesCorrectly()
    {
        // Arrange
        const string html = "<p>Text with <br/> line break and <img src='test.jpg' alt='test'/> image</p>";
        const string expected = "Text with  line break and  image";

        // Act
        var result = ContentProcessor.RemoveTags(html);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RemoveTags_WithMalformedHtml_FallsBackToBackupMethod()
    {
        // Arrange
        const string html = "Text with <unclosed tag and some > content";

        // Act
        var result = ContentProcessor.RemoveTags(html);

        // Assert
        Assert.Contains("Text with", result);
        Assert.Contains("content", result);
        Assert.DoesNotContain("<", result);
        Assert.DoesNotContain(">", result);
    }

    [Fact]
    public void RemoveTags_WithNbspEntities_ReplacesWithSpaces()
    {
        // Arrange - This tests the backup method behavior
        const string html = "Text with&nbsp;non-breaking spaces";

        // Act
        var result = ContentProcessor.RemoveTags(html);

        // Assert
        Assert.Equal("Text with non-breaking spaces", result);
    }

    #endregion

    #region Ellipsize Tests

    [Theory]
    [InlineData("", 10, "")]
    [InlineData("   ", 10, "")]
    [InlineData("Short", 10, "Short\u00A0\u2026")]
    [InlineData("This is a longer text", 10, "This is a \u00A0\u2026")]
    public void Ellipsize_WithVariousInputs_ReturnsExpectedResult(string text, int characterCount, string expected)
    {
        // Act
        var result = text.Ellipsize(characterCount);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Ellipsize_WithNegativeCharacterCount_ReturnsTextWithEllipsis()
    {
        // Arrange
        const string text = "Some text";
        const int characterCount = -5;

        // Act
        var result = text.Ellipsize(characterCount);

        // Assert
        Assert.Equal("Some text\u00A0\u2026", result);
    }

    [Fact]
    public void Ellipsize_WithTextShorterThanLimit_ReturnsOriginalTextWithEllipsis()
    {
        // Arrange
        const string text = "Short";
        const int characterCount = 10;

        // Act
        var result = text.Ellipsize(characterCount);

        // Assert
        Assert.Equal("Short\u00A0\u2026", result);
    }

    [Fact]
    public void Ellipsize_WithWordBoundary_BreaksAtWordBoundary()
    {
        // Arrange
        const string text = "This is a test sentence";
        const int characterCount = 10;

        // Act
        var result = text.Ellipsize(characterCount);

        // Assert
        Assert.Equal("This is a \u00A0\u2026", result);
        Assert.DoesNotContain("te", result); // Should not cut in middle of "test"
    }

    #endregion

    #region IsLetter Tests

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

    #endregion

    #region IsSpace Tests

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

    #endregion

    #region MarkdownToContent Tests

    [Fact]
    public void MarkdownToContent_WithNoneType_ReturnsOriginalMarkdown()
    {
        // Arrange
        const string markdown = "# Heading\n\nThis is **bold** text.";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.None);

        // Assert
        Assert.Equal(markdown, result);
    }

    [Fact]
    public void MarkdownToContent_WithHtmlType_ConvertsToHtml()
    {
        // Arrange
        const string markdown = "# Heading\n\nThis is **bold** text.";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html);

        // Assert
        Assert.Contains("<h1>Heading</h1>", result);
        Assert.Contains("<strong>bold</strong>", result);
        Assert.Contains("<p>", result);
    }

    [Fact]
    public void MarkdownToContent_WithTextType_ConvertsToPlainText()
    {
        // Arrange
        const string markdown = "# Heading\n\nThis is **bold** text with a [link](http://example.com).";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Text);

        // Assert
        Assert.Contains("Heading", result);
        Assert.Contains("bold", result);
        Assert.Contains("link", result);
        Assert.DoesNotContain("#", result);
        Assert.DoesNotContain("**", result);
        Assert.DoesNotContain("[", result);
        Assert.DoesNotContain("](", result);
    }

    [Fact]
    public void MarkdownToContent_WithPipeTables_ProcessesTables()
    {
        // Arrange
        const string markdown = "| Header 1 | Header 2 |\n|----------|----------|\n| Cell 1   | Cell 2   |";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html);

        // Assert
        Assert.Contains("<table", result);
        Assert.Contains("<thead>", result);
        Assert.Contains("<tbody>", result);
        Assert.Contains("Header 1", result);
        Assert.Contains("Cell 1", result);
    }

    [Fact]
    public void MarkdownToContent_WithDisableHtmlFalse_AllowsHtml()
    {
        // Arrange
        const string markdown = "# Heading\n\n<div>HTML content</div>";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html, false);

        // Assert
        Assert.Contains("<div>HTML content</div>", result);
    }

    [Fact]
    public void MarkdownToContent_WithDisableHtmlTrue_EscapesHtml()
    {
        // Arrange
        const string markdown = "# Heading\n\n<div>HTML content</div>";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html, true);

        // Assert
        Assert.DoesNotContain("<div>HTML content</div>", result);
    }

    [Fact]
    public void MarkdownToContent_WithInvalidType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string markdown = "# Heading";
        const ContentProcessor.MarkdownConvertType invalidType = (ContentProcessor.MarkdownConvertType)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            ContentProcessor.MarkdownToContent(markdown, invalidType));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkdownToContent_WithEmptyOrWhitespaceMarkdown_HandlesGracefully(string markdown)
    {
        // Act
        var htmlResult = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html);
        var textResult = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Text);

        // Assert
        Assert.NotNull(htmlResult);
        Assert.NotNull(textResult);
    }

    #endregion

    #region Integration Tests

    //[Fact]
    //public void GetPostAbstract_IntegrationWithMarkdownAndHtmlDecode_WorksCorrectly()
    //{
    //    // Arrange
    //    const string markdownContent = "# Title\n\nThis is **bold** text with &amp; entities.";
    //    const int wordCount = 15;

    //    // Act
    //    var result = ContentProcessor.GetPostAbstract(markdownContent, wordCount, true);

    //    // Assert
    //    Assert.Contains("Title", result);
    //    Assert.Contains("bold text with & entities", result);
    //    Assert.DoesNotContain("#", result);
    //    Assert.DoesNotContain("**", result);
    //    Assert.DoesNotContain("&amp;", result);
    //}

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

    #endregion
}