using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;
using Moq;
using System.Text;

namespace Moonglade.Web.Middleware.Tests;

public class StyleSheetHandlerTests
{
    private readonly Mock<IBlogConfig> _mockBlogConfig;
    private readonly Mock<IQueryMediator> _mockQueryMediator;
    private readonly Mock<ILogger> _mockLogger;
    private readonly AppearanceSettings _appearanceSettings;
    private readonly StyleSheetOptions _options;

    public StyleSheetHandlerTests()
    {
        _mockBlogConfig = new Mock<IBlogConfig>();
        _mockQueryMediator = new Mock<IQueryMediator>();
        _mockLogger = new Mock<ILogger>();
        _appearanceSettings = new AppearanceSettings();
        _options = new StyleSheetOptions { MaxContentLength = 65536, CacheMaxAge = 3600 };
        _mockBlogConfig.Setup(x => x.AppearanceSettings).Returns(_appearanceSettings);
    }

    #region Custom CSS Tests

    [Fact]
    public async Task HandleCustomCss_WhenCustomCssDisabled_ReturnsNotFound()
    {
        var context = CreateContext();
        _appearanceSettings.EnableCustomCss = false;

        await StyleSheetHandler.HandleCustomCssAsync(context, _mockBlogConfig.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("text/plain", context.Response.ContentType);
        Assert.Equal("Stylesheet not found", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task HandleCustomCss_WhenCssCodeEmpty_ReturnsNotFound()
    {
        var context = CreateContext();
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = "";

        await StyleSheetHandler.HandleCustomCssAsync(context, _mockBlogConfig.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleCustomCss_WhenValidCss_ReturnsStyleSheet()
    {
        var context = CreateContext();
        var cssCode = ".custom { color: red; }";
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = cssCode;

        await StyleSheetHandler.HandleCustomCssAsync(context, _mockBlogConfig.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/css; charset=utf-8", context.Response.ContentType);
        Assert.Equal(Encoding.UTF8.GetByteCount(cssCode), context.Response.ContentLength);
        Assert.Equal(cssCode, await ReadBodyAsync(context));
    }

    [Fact]
    public async Task HandleCustomCss_WhenValidCss_DoesNotSetLastModified()
    {
        var context = CreateContext();
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = ".test { }";

        await StyleSheetHandler.HandleCustomCssAsync(context, _mockBlogConfig.Object, _options, _mockLogger.Object);

        Assert.False(context.Response.Headers.ContainsKey("Last-Modified"));
    }

    [Fact]
    public async Task HandleCustomCss_WhenValidCss_SetsCachingHeaders()
    {
        var context = CreateContext();
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = ".test { color: green; }";

        await StyleSheetHandler.HandleCustomCssAsync(context, _mockBlogConfig.Object, _options, _mockLogger.Object);

        Assert.True(context.Response.Headers.ContainsKey("ETag"));
        Assert.True(context.Response.Headers.ContainsKey("Cache-Control"));
        Assert.True(context.Response.Headers.ContainsKey("Expires"));
        Assert.Contains("public", context.Response.Headers.CacheControl.ToString());
        Assert.Contains("max-age=3600", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task HandleCustomCss_WhenIfNoneMatchMatches_ReturnsNotModified()
    {
        var context = CreateContext();
        var cssCode = ".test { color: blue; }";
        _appearanceSettings.EnableCustomCss = true;
        _appearanceSettings.CssCode = cssCode;

        var expectedETag = StyleSheetHandler.GenerateETag(cssCode);
        context.Request.Headers.IfNoneMatch = expectedETag;

        await StyleSheetHandler.HandleCustomCssAsync(context, _mockBlogConfig.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status304NotModified, context.Response.StatusCode);
    }

    #endregion

    #region Content CSS Tests

    [Fact]
    public async Task HandleContentCss_WhenMissingId_ReturnsNotFound()
    {
        var context = CreateContext();

        await StyleSheetHandler.HandleContentCssAsync(context, _mockQueryMediator.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleContentCss_WhenInvalidId_ReturnsNotFound()
    {
        var context = CreateContext(query: "?id=not-a-guid");

        await StyleSheetHandler.HandleContentCssAsync(context, _mockQueryMediator.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleContentCss_WhenNotFoundInDb_ReturnsNotFound()
    {
        var context = CreateContext(query: $"?id={Guid.NewGuid()}");

        _mockQueryMediator
            .Setup(x => x.QueryAsync<StyleSheetEntity>(It.IsAny<GetStyleSheetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StyleSheetEntity)null);

        await StyleSheetHandler.HandleContentCssAsync(context, _mockQueryMediator.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleContentCss_WhenValidId_ReturnsStyleSheet()
    {
        var context = CreateContext(query: $"?id={Guid.NewGuid()}");
        var cssCode = ".page { font-size: 16px; }";
        var lastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _mockQueryMediator
            .Setup(x => x.QueryAsync<StyleSheetEntity>(It.IsAny<GetStyleSheetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StyleSheetEntity { CssContent = cssCode, LastModifiedTimeUtc = lastModified });

        await StyleSheetHandler.HandleContentCssAsync(context, _mockQueryMediator.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/css; charset=utf-8", context.Response.ContentType);
        Assert.Equal(cssCode, await ReadBodyAsync(context));
        Assert.Equal(lastModified.ToString("R"), context.Response.Headers.LastModified.ToString());
    }

    [Fact]
    public async Task HandleContentCss_WhenIfNoneMatchMatches_ReturnsNotModified()
    {
        var cssCode = ".page { margin: 0; }";
        var context = CreateContext(query: $"?id={Guid.NewGuid()}");
        var expectedETag = StyleSheetHandler.GenerateETag(cssCode);
        context.Request.Headers.IfNoneMatch = expectedETag;

        _mockQueryMediator
            .Setup(x => x.QueryAsync<StyleSheetEntity>(It.IsAny<GetStyleSheetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StyleSheetEntity { CssContent = cssCode, LastModifiedTimeUtc = DateTime.UtcNow });

        await StyleSheetHandler.HandleContentCssAsync(context, _mockQueryMediator.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status304NotModified, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleContentCss_WhenIfModifiedSinceAfterLastModified_ReturnsNotModified()
    {
        var cssCode = ".page { padding: 0; }";
        var lastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var context = CreateContext(query: $"?id={Guid.NewGuid()}");
        context.Request.Headers.IfModifiedSince = lastModified.ToString("R");

        _mockQueryMediator
            .Setup(x => x.QueryAsync<StyleSheetEntity>(It.IsAny<GetStyleSheetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StyleSheetEntity { CssContent = cssCode, LastModifiedTimeUtc = lastModified });

        await StyleSheetHandler.HandleContentCssAsync(context, _mockQueryMediator.Object, _options, _mockLogger.Object);

        Assert.Equal(StatusCodes.Status304NotModified, context.Response.StatusCode);
    }

    #endregion

    #region Content Size Tests

    [Fact]
    public async Task WriteStyleSheet_WhenCssContentTooLarge_ReturnsPayloadTooLarge()
    {
        var context = CreateContext();
        var largeCssCode = new string('a', 65537); // MaxContentLength (65536) + 1

        await StyleSheetHandler.WriteStyleSheetAsync(context, largeCssCode, null, _options);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Equal("CSS content too large", await ReadBodyAsync(context));
    }

    #endregion

    #region IsNotModified Tests

    [Fact]
    public void IsNotModified_WhenWildcardETag_ReturnsTrue()
    {
        var context = CreateContext();
        context.Request.Headers.IfNoneMatch = "*";

        Assert.True(StyleSheetHandler.IsNotModified(context, "\"anyetag\"", null));
    }

    [Fact]
    public void IsNotModified_WhenETagMismatch_ReturnsFalse()
    {
        var context = CreateContext();
        context.Request.Headers.IfNoneMatch = "\"differentetag\"";

        Assert.False(StyleSheetHandler.IsNotModified(context, "\"actualetag\"", null));
    }

    [Fact]
    public void IsNotModified_WhenNoConditionalHeaders_ReturnsFalse()
    {
        var context = CreateContext();

        Assert.False(StyleSheetHandler.IsNotModified(context, "\"someetag\"", DateTime.UtcNow));
    }

    #endregion

    private static DefaultHttpContext CreateContext(string query = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (!string.IsNullOrEmpty(query))
        {
            context.Request.QueryString = new QueryString(query);
        }

        return context;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
