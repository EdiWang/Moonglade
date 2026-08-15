using Edi.AspNetCore.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Moonglade.Webmention.Tests;

public class MentionSourceInspectorTests
{
    private readonly Mock<ILogger<MentionSourceInspector>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IPublicHttpUrlValidator> _mockUrlSafetyValidator;
    private readonly HttpClient _httpClient;

    public MentionSourceInspectorTests()
    {
        _mockLogger = new Mock<ILogger<MentionSourceInspector>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockUrlSafetyValidator = new Mock<IPublicHttpUrlValidator>();
        _mockUrlSafetyValidator
            .Setup(x => x.IsPublicHttpUrlAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    [Fact]
    public async Task ExamineSourceAsync_NullSourceUrl_ThrowsArgumentException()
    {
        var inspector = CreateInspector();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            inspector.ExamineSourceAsync(null!, "https://example.com/target"));
    }

    [Fact]
    public async Task ExamineSourceAsync_EmptySourceUrl_ThrowsArgumentException()
    {
        var inspector = CreateInspector();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            inspector.ExamineSourceAsync("", "https://example.com/target"));
    }

    [Fact]
    public async Task ExamineSourceAsync_WhitespaceSourceUrl_ThrowsArgumentException()
    {
        var inspector = CreateInspector();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            inspector.ExamineSourceAsync("   ", "https://example.com/target"));
    }

    [Fact]
    public async Task ExamineSourceAsync_NullTargetUrl_ThrowsArgumentException()
    {
        var inspector = CreateInspector();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            inspector.ExamineSourceAsync("https://example.com/source", null!));
    }

    [Fact]
    public async Task ExamineSourceAsync_EmptyTargetUrl_ThrowsArgumentException()
    {
        var inspector = CreateInspector();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            inspector.ExamineSourceAsync("https://example.com/source", ""));
    }

    [Fact]
    public async Task ExamineSourceAsync_ValidHtmlWithTitleAndTargetLink_ReturnsCorrectMentionRequest()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                    <title>Test Blog Post</title>
                </head>
                <body>
                    <p>Check out this <a href=""https://example.com/target"">awesome article</a>!</p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.Equal("Test Blog Post", result.Title);
        Assert.False(result.ContainsHtml);
        Assert.True(result.SourceHasTarget);
        Assert.Equal(targetUrl, result.TargetUrl);
        Assert.Equal(sourceUrl, result.SourceUrl);
    }

    [Fact]
    public async Task ExamineSourceAsync_HtmlWithoutTargetLink_ReturnsFalseForSourceHasTarget()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                    <title>Test Blog Post</title>
                </head>
                <body>
                    <p>This post has no link to the target.</p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.Equal("Test Blog Post", result.Title);
        Assert.False(result.SourceHasTarget);
    }

    [Fact]
    public async Task ExamineSourceAsync_HtmlWithTitleContainingHtmlTags_ReturnsTrueForContainsHtml()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                    <title>Test <strong>Blog</strong> Post</title>
                </head>
                <body>
                    <p>Content here</p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.True(result.ContainsHtml);
    }

    [Fact]
    public async Task ExamineSourceAsync_HtmlWithoutTitle_ReturnsEmptyTitle()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                </head>
                <body>
                    <p>Content without title</p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.Title);
    }

    [Fact]
    public async Task ExamineSourceAsync_TargetUrlWithTrailingSlash_MatchesUrlWithoutSlash()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target/";
        var html = @"
            <html>
                <head>
                    <title>Test</title>
                </head>
                <body>
                    <p>Link to <a href=""https://example.com/target"">target</a></p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.True(result.SourceHasTarget);
    }

    [Fact]
    public async Task ExamineSourceAsync_TargetUrlCaseInsensitive_Matches()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/TARGET";
        var html = @"
            <html>
                <head>
                    <title>Test</title>
                </head>
                <body>
                    <p>Link to <a href=""https://example.com/target"">target</a></p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.True(result.SourceHasTarget);
    }

    [Fact]
    public async Task ExamineSourceAsync_HttpRequestException_ReturnsNull()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExamineSourceAsync_UnsafeSourceUrl_ReturnsNullWithoutFetching()
    {
        var sourceUrl = "https://internal.example/source";
        var targetUrl = "https://example.com/target";
        _mockUrlSafetyValidator
            .Setup(x => x.IsPublicHttpUrlAsync(It.Is<Uri>(uri => uri.Host == "internal.example"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.Null(result);
        _mockHttpMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExamineSourceAsync_UnsafeRedirectUrl_ReturnsNullWithoutFollowingRedirect()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var redirectResponse = new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers =
            {
                Location = new Uri("http://10.0.0.5/private")
            }
        };
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(redirectResponse);
        _mockUrlSafetyValidator
            .Setup(x => x.IsPublicHttpUrlAsync(It.Is<Uri>(uri => uri.Host == "10.0.0.5"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.Null(result);
        _mockHttpMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(request => request.RequestUri!.ToString() == sourceUrl),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExamineSourceAsync_SafeRelativeRedirect_FollowsRedirect()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var redirectResponse = new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers =
            {
                Location = new Uri("/redirected", UriKind.Relative)
            }
        };
        var html = """
            <html>
                <head><title>Redirected</title></head>
                <body><a href="https://example.com/target">target</a></body>
            </html>
            """;
        var finalResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        };
        _mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(redirectResponse)
            .ReturnsAsync(finalResponse);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.Equal("Redirected", result.Title);
        Assert.True(result.SourceHasTarget);
        _mockUrlSafetyValidator.Verify(
            x => x.IsPublicHttpUrlAsync(It.Is<Uri>(uri => uri.ToString() == sourceUrl), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUrlSafetyValidator.Verify(
            x => x.IsPublicHttpUrlAsync(It.Is<Uri>(uri => uri.ToString() == "https://example.com/redirected"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExamineSourceAsync_ResponseTooLarge_ReturnsNull()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("content")
        };
        response.Content.Headers.ContentLength = 2 * 1024 * 1024; // 2 MB

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExamineSourceAsync_ContentTooLarge_ReturnsNull()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";

        // Create content larger than 1 MB
        var largeContent = new string('x', 2 * 1024 * 1024);

        SetupHttpResponse(largeContent);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExamineSourceAsync_MultipleLinksToTarget_ReturnsTrue()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                    <title>Test</title>
                </head>
                <body>
                    <p>First link to <a href=""https://example.com/target"">target</a></p>
                    <p>Second link to <a href=""https://example.com/target"">target again</a></p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.True(result.SourceHasTarget);
    }

    [Fact]
    public async Task ExamineSourceAsync_LinkWithSingleQuotes_MatchesTargetUrl()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                    <title>Test</title>
                </head>
                <body>
                    <p>Link with <a href='https://example.com/target'>single quotes</a></p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.True(result.SourceHasTarget);
    }

    [Fact]
    public async Task ExamineSourceAsync_TitleWithWhitespace_TrimsCorrectly()
    {
        var sourceUrl = "https://example.com/source";
        var targetUrl = "https://example.com/target";
        var html = @"
            <html>
                <head>
                    <title>  Test Title With Spaces  </title>
                </head>
                <body>
                    <p>Content</p>
                </body>
            </html>";

        SetupHttpResponse(html);

        var inspector = CreateInspector();
        var result = await inspector.ExamineSourceAsync(sourceUrl, targetUrl);

        Assert.NotNull(result);
        Assert.Equal("Test Title With Spaces", result.Title);
    }

    private void SetupHttpResponse(string content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private MentionSourceInspector CreateInspector() =>
        new(_mockLogger.Object, _httpClient, _mockUrlSafetyValidator.Object);
}
