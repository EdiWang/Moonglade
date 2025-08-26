using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moq;
using System.Text;

namespace Moonglade.Web.Middleware.Tests;

public class StyleSheetMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<StyleSheetMiddleware>> _mockLogger;
    private readonly Mock<IBlogConfig> _mockBlogConfig;
    private readonly Mock<IQueryMediator> _mockQueryMediator;
    private readonly AppearanceSettings _appearanceSettings;
    private readonly StyleSheetMiddleware _middleware;

    public StyleSheetMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<StyleSheetMiddleware>>();
        _mockBlogConfig = new Mock<IBlogConfig>();
        _mockQueryMediator = new Mock<IQueryMediator>();
        _appearanceSettings = new AppearanceSettings();
        _middleware = new StyleSheetMiddleware(_mockNext.Object, _mockLogger.Object);

        // Setup default middleware options for testing
        StyleSheetMiddleware.Options = new StyleSheetMiddlewareOptions
        {
            MaxContentLength = 65536,
            DefaultPath = "/custom.css",
            CacheMaxAge = 3600
        };

        // Setup default blog config mocks
        _mockBlogConfig.Setup(x => x.AppearanceSettings).Returns(_appearanceSettings);
    }

    #region Non-CSS Request Tests

    [Fact]
    public async Task Invoke_WhenRequestIsNotCss_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/regular-page";

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        _mockNext.Verify(x => x.Invoke(context), Times.Once);
    }

    [Theory]
    [InlineData("/styles.js")]
    [InlineData("/app.html")]
    [InlineData("/image.png")]
    [InlineData("/document.pdf")]
    public async Task Invoke_WhenRequestIsNotCssFile_CallsNextMiddleware(string path)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        _mockNext.Verify(x => x.Invoke(context), Times.Once);
    }

    #endregion

    #region Suspicious Path Tests

    [Theory]
    [InlineData("/styles/../admin.css")]
    [InlineData("/~/config.css")]
    [InlineData("/path\\with\\backslashes.css")]
    [InlineData("/null\0char.css")]
    public async Task Invoke_WhenPathContainsSuspiciousCharacters_ReturnsBadRequest(string suspiciousPath)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = suspiciousPath;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        _mockNext.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_WhenPathContainsSuspiciousCharacters_LogsWarning()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var suspiciousPath = "/styles/../admin.css";
        context.Request.Path = suspiciousPath;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Suspicious path detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region Default Path Tests

    [Fact]
    public async Task Invoke_WhenDefaultPathAndCustomCssDisabled_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        _appearanceSettings.EnableCustomCss = false;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        Assert.Equal("Stylesheet not found", responseBody);
    }

    [Fact]
    public async Task Invoke_WhenDefaultPathAndCssCodeEmpty_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = "";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_WhenDefaultPathAndValidCss_ReturnsStyleSheet()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        var cssCode = ".custom { color: red; }";
        
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = cssCode;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/css; charset=utf-8", context.Response.ContentType);
        Assert.Equal(Encoding.UTF8.GetByteCount(cssCode), context.Response.ContentLength);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        Assert.Equal(cssCode, responseBody);
    }

    [Fact]
    public async Task Invoke_WhenDefaultPathCaseInsensitive_ReturnsStyleSheet()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/CUSTOM.CSS";
        var cssCode = ".test { font-size: 14px; }";
        
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = cssCode;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    #endregion

    #region Content CSS Tests

    [Fact]
    public async Task Invoke_WhenContentCssWithoutQueryString_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/content.css";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_WhenContentCssWithEmptyQueryString_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/content.css";
        context.Request.QueryString = new QueryString("");

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_WhenContentCssWithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/content.css";
        context.Request.QueryString = new QueryString("?id=invalid-guid");

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_WhenContentCssWithMissingId_ReturnsNotFound()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/content.css";
        context.Request.QueryString = new QueryString("?other=value");

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    #endregion

    #region CSS Content Size Tests

    [Fact]
    public async Task Invoke_WhenCssContentTooLarge_ReturnsPayloadTooLarge()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        
        // Create CSS content larger than MaxContentLength
        var largeCssCode = new string('a', StyleSheetMiddleware.Options.MaxContentLength + 1);
        
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = largeCssCode;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        Assert.Equal("CSS content too large", responseBody);
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task Invoke_WhenValidCss_SetsCachingHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        var cssCode = ".test { color: green; }";
        
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = cssCode;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("ETag"));
        Assert.True(context.Response.Headers.ContainsKey("Last-Modified"));
        Assert.True(context.Response.Headers.ContainsKey("Cache-Control"));
        Assert.True(context.Response.Headers.ContainsKey("Expires"));

        Assert.Contains("public", context.Response.Headers.CacheControl.ToString());
        Assert.Contains("max-age=3600", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Invoke_WhenIfNoneMatchMatches_ReturnsNotModified()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        var cssCode = ".test { color: blue; }";
        
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = cssCode;

        // Calculate expected ETag
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(cssCode));
        var expectedETag = $"\"{Convert.ToHexString(hash)[..16]}\"";
        
        context.Request.Headers.IfNoneMatch = expectedETag;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status304NotModified, context.Response.StatusCode);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Invoke_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";

        _mockBlogConfig.Setup(x => x.AppearanceSettings).Throws(new InvalidOperationException("Config error"));

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_WhenExceptionThrown_LogsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";

        _mockBlogConfig.Setup(x => x.AppearanceSettings).Throws(new InvalidOperationException("Config error"));

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error processing CSS request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_WhenResponseAlreadyStarted_DoesNotSetStatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/custom.css";
        
        // Simulate response already started
        await context.Response.WriteAsync("some content");

        _mockBlogConfig.Setup(x => x.AppearanceSettings).Throws(new InvalidOperationException("Config error"));

        // Act & Assert - Should not throw
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);
    }

    #endregion

    #region Other CSS Requests Tests

    [Fact]
    public async Task Invoke_WhenOtherCssRequest_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/other-styles.css";

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object, _mockQueryMediator.Object);

        // Assert
        _mockNext.Verify(x => x.Invoke(context), Times.Once);
    }

    #endregion
}