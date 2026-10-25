namespace Moonglade.Utils.Tests.Content;

public class MarkdownContentTests
{
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
    public void MarkdownToContent_WithMermaidCodeBlock_PreservesLanguageClass()
    {
        // Arrange
        const string markdown = "```mermaid\nflowchart TD\nA-->B\n```";

        // Act
        var result = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html);

        // Assert
        Assert.Contains("<pre><code class=\"language-mermaid\">", result);
        Assert.Contains("flowchart TD", result);
        Assert.Contains("A--&gt;B", result);
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
    public void MarkdownToContent_WithEmptyOrWhitespaceMarkdown_ReturnsEmpty(string markdown)
    {
        // Act
        var htmlResult = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Html);
        var textResult = ContentProcessor.MarkdownToContent(markdown, ContentProcessor.MarkdownConvertType.Text);

        // Assert
        Assert.Equal(string.Empty, htmlResult);
        Assert.Equal(string.Empty, textResult);
    }
}
