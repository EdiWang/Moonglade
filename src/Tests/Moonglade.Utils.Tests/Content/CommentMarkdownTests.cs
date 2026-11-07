namespace Moonglade.Utils.Tests.Content;

public class CommentMarkdownTests
{
    [Fact]
    public void MarkdownToCommentHtml_WithHttpLink_AddsCommentLinkRel()
    {
        const string markdown = "[example](https://example.com?a=1&b=2)";

        var result = ContentProcessor.MarkdownToCommentHtml(markdown, enableLinks: true);

        Assert.Contains("<a href=\"https://example.com?a=1&amp;b=2\" rel=\"nofollow ugc noopener noreferrer\">example</a>", result);
    }

    [Theory]
    [InlineData("[example](https://example.com?a=1&b=2)")]
    [InlineData("[**example**](https://example.com \"Example Site\")")]
    [InlineData("[script](javascript:alert(1))")]
    [InlineData("[relative](/about)")]
    [InlineData("[fragment](#comments)")]
    [InlineData("[mail](mailto:test@example.com)")]
    [InlineData("<https://example.com>")]
    [InlineData("<test@example.com>")]
    [InlineData("[example][site]\n\n[site]: https://example.com \"Example Site\"")]
    [InlineData("[example][]\n\n[example]: https://example.com")]
    [InlineData("[example]\n\n[example]: https://example.com")]
    public void MarkdownToCommentHtml_ByDefault_PreservesLinksAsPlainText(string markdown)
    {
        var result = ContentProcessor.MarkdownToCommentHtml(markdown);

        Assert.DoesNotContain("<a", result);
        foreach (var line in markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.Contains(line, System.Net.WebUtility.HtmlDecode(result));
        }
    }

    [Fact]
    public void MarkdownToCommentHtml_WithLinksDisabled_DoesNotDuplicateLinkText()
    {
        var result = ContentProcessor.MarkdownToCommentHtml("ccc [ddd](https://eee.com)");

        Assert.Equal("<p>ccc [ddd](https://eee.com)</p>\n", result.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void MarkdownToCommentHtml_WithLinksDisabled_PreservesOtherMarkdownAndEncodesHtml()
    {
        const string markdown = "**Hello** [<script>alert(1)</script>](https://example.com) `code` ![image](/image/test.png)";

        var result = ContentProcessor.MarkdownToCommentHtml(markdown, enableLinks: false);

        Assert.Contains("<strong>Hello</strong>", result);
        Assert.Contains("<code>code</code>", result);
        Assert.Contains("<img", result);
        Assert.Contains("[&lt;script&gt;alert(1)&lt;/script&gt;](https://example.com)", result);
        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain("<a ", result);
    }

    [Theory]
    [InlineData("[script](javascript:alert(1))")]
    [InlineData("[script](java&#x73;cript:alert(1))")]
    [InlineData("[data](data:text/html,hello)")]
    public void MarkdownToCommentHtml_WithUnsafeLinkProtocol_RemovesHref(string markdown)
    {
        var result = ContentProcessor.MarkdownToCommentHtml(markdown, enableLinks: true);

        Assert.DoesNotContain("href=", result);
        Assert.DoesNotContain("javascript:", result);
        Assert.DoesNotContain("data:", result);
    }

    [Theory]
    [InlineData("[relative](/about)", "href=\"/about\"")]
    [InlineData("[fragment](#comments)", "href=\"#comments\"")]
    [InlineData("[mail](mailto:test@example.com)", "href=\"mailto:test@example.com\"")]
    public void MarkdownToCommentHtml_WithAllowedLinkProtocol_KeepsHref(string markdown, string expectedHref)
    {
        var result = ContentProcessor.MarkdownToCommentHtml(markdown, enableLinks: true);

        Assert.Contains(expectedHref, result);
        Assert.Contains("rel=\"nofollow ugc noopener noreferrer\"", result);
    }

    [Fact]
    public void MarkdownToCommentHtml_WithLinkTitle_PreservesTitle()
    {
        const string markdown = "[example](https://example.com \"Example Site\")";

        var result = ContentProcessor.MarkdownToCommentHtml(markdown, enableLinks: true);

        Assert.Contains("href=\"https://example.com\"", result);
        Assert.Contains("title=\"Example Site\"", result);
        Assert.Contains("rel=\"nofollow ugc noopener noreferrer\"", result);
    }
}
