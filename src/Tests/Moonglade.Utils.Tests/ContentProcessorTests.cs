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

    #region GetKeywords Tests

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  \t  \n  ", null)]
    public void GetKeywords_WithNullOrWhitespace_ReturnsNull(string rawKeywords, string expected)
    {
        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithSingleKeyword_ReturnsTrimmedKeyword()
    {
        // Arrange
        const string rawKeywords = "  technology  ";
        const string expected = "technology";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithMultipleKeywords_ReturnsCommaSeparatedTrimmedKeywords()
    {
        // Arrange
        const string rawKeywords = "technology, programming, csharp";
        const string expected = "technology,programming,csharp";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithKeywordsContainingWhitespace_TrimsEachKeyword()
    {
        // Arrange
        const string rawKeywords = "  technology  ,   programming   ,  csharp  ";
        const string expected = "technology,programming,csharp";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithDuplicateKeywords_RemovesDuplicates()
    {
        // Arrange
        const string rawKeywords = "technology,programming,technology,csharp,programming";
        const string expected = "technology,programming,csharp";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithDuplicateKeywordsDifferentCasing_TreatsAsDistinct()
    {
        // Arrange
        const string rawKeywords = "Technology,technology,TECHNOLOGY,Programming";
        const string expected = "Technology,technology,TECHNOLOGY,Programming";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithEmptyKeywordsBetweenCommas_FiltersOutEmptyKeywords()
    {
        // Arrange
        const string rawKeywords = "technology,,programming,   ,csharp,";
        const string expected = "technology,programming,csharp";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithOnlyEmptyKeywords_ReturnsNull()
    {
        // Arrange
        const string rawKeywords = " , ,   ,  , ";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetKeywords_WithComplexMixedInput_HandlesCorrectly()
    {
        // Arrange
        const string rawKeywords = "  ASP.NET Core  , ,   Entity Framework   , ASP.NET Core ,  Blazor  ,   ,  C# Programming  ,";
        const string expected = "ASP.NET Core,Entity Framework,Blazor,C# Programming";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("keyword1", "keyword1")]
    [InlineData("keyword1,keyword2", "keyword1,keyword2")]
    [InlineData("a,b,c,d,e", "a,b,c,d,e")]
    public void GetKeywords_WithValidKeywords_PreservesOrder(string rawKeywords, string expected)
    {
        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetKeywords_WithSpecialCharactersInKeywords_PreservesSpecialCharacters()
    {
        // Arrange
        const string rawKeywords = "C#, .NET 8, ASP.NET Core, Entity Framework 7.0, Visual Studio 2022";
        const string expected = "C#,.NET 8,ASP.NET Core,Entity Framework 7.0,Visual Studio 2022";

        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
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