namespace Moonglade.Utils.Tests.Content;

public class HtmlContentTests
{
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
}
