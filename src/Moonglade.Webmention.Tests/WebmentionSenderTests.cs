using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Moonglade.Webmention.Tests;

public class WebmentionSenderTests
{
    private readonly Mock<ILogger<WebmentionSender>> _mockLogger;
    private readonly Mock<IWebmentionRequestor> _mockRequestor;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public WebmentionSenderTests()
    {
        _mockLogger = new Mock<ILogger<WebmentionSender>>();
        _mockRequestor = new Mock<IWebmentionRequestor>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    #region SendWebmentionAsync

    [Fact]
    public async Task SendWebmentionAsync_LocalhostSourceUrl_Skips()
    {
        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://localhost/post/2024/1/1/test",
            "<p>Hello <a href=\"https://example.com\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Never);
    }

    [Fact]
    public async Task SendWebmentionAsync_NoUrlsInContent_DoesNotSend()
    {
        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p>Hello world, no links here.</p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Never);
    }

    [Fact]
    public async Task SendWebmentionAsync_LocalhostTargetUrl_SkipsTarget()
    {
        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p>Visit <a href=\"https://localhost/page\">local</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Never);
    }

    [Fact]
    public async Task SendWebmentionAsync_ValidUrlInContent_SendsWebmention()
    {
        var targetUrl = "https://target.example.com/post";
        var endpointUrl = "https://target.example.com/webmention";

        SetupHttpResponse($"<html><link rel=\"webmention\" href=\"{endpointUrl}\"></html>");

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            $"<p>Check <a href=\"{targetUrl}\">this out</a></p>");

        _mockRequestor.Verify(
            r => r.Send(
                It.Is<Uri>(u => u.ToString() == "https://example.com/post/2024/1/1/test"),
                It.Is<Uri>(u => u.ToString() == targetUrl),
                It.Is<Uri>(u => u.ToString() == endpointUrl)),
            Times.Once);
    }

    [Fact]
    public async Task SendWebmentionAsync_SendAsyncThrows_CatchesAndContinues()
    {
        SetupHttpResponse("<html><link rel=\"webmention\" href=\"https://a.com/wm\"></html>");

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ThrowsAsync(new HttpRequestException("network error"));

        var sender = CreateSender();

        // Should not throw
        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://a.com/post\">link</a></p>");
    }

    [Fact]
    public async Task SendWebmentionAsync_InvalidPostUrl_CatchesAndLogs()
    {
        var sender = CreateSender();

        // "not a url" will fail new Uri(...)
        await sender.SendWebmentionAsync("not a url", "<p>hello</p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Never);
    }

    #endregion

    #region DiscoverWebmentionEndpoint (via SendAsync path)

    [Fact]
    public async Task SendAsync_EndpointNotFound_LogsWarning()
    {
        // Return HTML without a webmention link
        SetupHttpResponse("<html><head></head><body>No endpoint</body></html>");

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_NonSuccessResponse_DoesNotSend()
    {
        SetupHttpResponse(HttpStatusCode.NotFound, "");

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_WebmentionResponseFails_LogsError()
    {
        SetupHttpResponse("<html><link rel=\"webmention\" href=\"https://target.example.com/webmention\"></html>");

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Once);
    }

    [Fact]
    public async Task DiscoverEndpoint_LinkHeader_TakesPriority()
    {
        var endpointUrl = "https://target.example.com/webmention-endpoint";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html><link rel=\"webmention\" href=\"https://target.example.com/other\"></html>")
        };
        response.Headers.TryAddWithoutValidation("Link", $"<{endpointUrl}>; rel=\"webmention\"");

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        // Should use the Link header endpoint, not the HTML one
        _mockRequestor.Verify(
            r => r.Send(
                It.IsAny<Uri>(),
                It.IsAny<Uri>(),
                It.Is<Uri>(u => u.ToString() == endpointUrl)),
            Times.Once);
    }

    [Fact]
    public async Task DiscoverEndpoint_LinkHeader_WithoutQuotes_Works()
    {
        var endpointUrl = "https://target.example.com/webmention";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html></html>")
        };
        response.Headers.TryAddWithoutValidation("Link", $"<{endpointUrl}>; rel=webmention");

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.Is<Uri>(u => u.ToString() == endpointUrl)),
            Times.Once);
    }

    [Fact]
    public async Task DiscoverEndpoint_HtmlLinkTag_HrefBeforeRel_Works()
    {
        SetupHttpResponse("<html><link href=\"https://target.example.com/webmention\" rel=\"webmention\"></html>");

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(
                It.IsAny<Uri>(),
                It.IsAny<Uri>(),
                It.Is<Uri>(u => u.ToString() == "https://target.example.com/webmention")),
            Times.Once);
    }

    [Fact]
    public async Task DiscoverEndpoint_HtmlLinkTag_SingleQuotes_Works()
    {
        SetupHttpResponse("<html><link rel='webmention' href='https://target.example.com/webmention'></html>");

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(
                It.IsAny<Uri>(),
                It.IsAny<Uri>(),
                It.Is<Uri>(u => u.ToString() == "https://target.example.com/webmention")),
            Times.Once);
    }

    [Fact]
    public async Task DiscoverEndpoint_RelativeEndpointUrl_ResolvedAgainstTarget()
    {
        SetupHttpResponse("<html><link rel=\"webmention\" href=\"/webmention\"></html>");

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page\">link</a></p>");

        _mockRequestor.Verify(
            r => r.Send(
                It.IsAny<Uri>(),
                It.IsAny<Uri>(),
                It.Is<Uri>(u => u.ToString() == "https://target.example.com/webmention")),
            Times.Once);
    }

    [Fact]
    public async Task SendWebmentionAsync_MultipleUrlsInContent_SendsForEach()
    {
        var html = "<html><link rel=\"webmention\" href=\"https://target.example.com/webmention\"></html>";

        // Return a fresh response for each request so the content stream is not reused
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            });

        _mockRequestor
            .Setup(r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sender = CreateSender();

        await sender.SendWebmentionAsync(
            "https://example.com/post/2024/1/1/test",
            "<p><a href=\"https://target.example.com/page1\">one</a> and <a href=\"https://target.example.com/page2\">two</a></p>");

        _mockRequestor.Verify(
            r => r.Send(It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<Uri>()),
            Times.Exactly(2));
    }

    #endregion

    #region Helpers

    private WebmentionSender CreateSender()
    {
        return new WebmentionSender(_httpClient, _mockRequestor.Object, _mockLogger.Object);
    }

    private void SetupHttpResponse(string content)
    {
        SetupHttpResponse(HttpStatusCode.OK, content);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage(statusCode)
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

    #endregion
}
