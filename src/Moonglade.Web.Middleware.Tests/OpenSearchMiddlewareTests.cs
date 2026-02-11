using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moq;
using System.Xml;

namespace Moonglade.Web.Middleware.Tests;

public class OpenSearchMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<IBlogConfig> _mockBlogConfig;
    private readonly GeneralSettings _generalSettings;
    private readonly AdvancedSettings _advancedSettings;
    private readonly OpenSearchMiddleware _middleware;

    public OpenSearchMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockBlogConfig = new Mock<IBlogConfig>();
        _generalSettings = new GeneralSettings();
        _advancedSettings = new AdvancedSettings();
        _middleware = new OpenSearchMiddleware(_mockNext.Object);

        // Setup default middleware options for testing
        OpenSearchMiddleware.Options = new OpenSearchMiddlewareOptions
        {
            RequestPath = "/opensearch",
            IconFileType = "image/png",
            IconFilePath = "/favicon-16x16.png",
            Xmlns = "http://a9.com/-/spec/opensearch/1.1/"
        };

        // Setup default blog config mocks
        _mockBlogConfig.Setup(x => x.GeneralSettings).Returns(_generalSettings);
        _mockBlogConfig.Setup(x => x.AdvancedSettings).Returns(_advancedSettings);
    }

    [Fact]
    public async Task Invoke_WhenRequestPathMatchesAndOpenSearchEnabled_ReturnsOpenSearchXml()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Test Blog";
        _generalSettings.Description = "Test Description";
        _generalSettings.CanonicalPrefix = "https://example.com";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        Assert.Equal("text/xml", context.Response.ContentType);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Contains("OpenSearchDescription", responseBody);
        Assert.Contains("Test Blog", responseBody);
        Assert.Contains("Test Description", responseBody);
        Assert.Contains("https://example.com/favicon-16x16.png", responseBody);
        Assert.Contains("https://example.com/search?term={searchTerms}", responseBody);

        _mockNext.Verify(x => x.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_WhenRequestPathMatchesButOpenSearchDisabled_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";

        _advancedSettings.EnableOpenSearch = false;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        _mockNext.Verify(x => x.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task Invoke_WhenRequestPathDoesNotMatch_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/other-path";

        _advancedSettings.EnableOpenSearch = true;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        _mockNext.Verify(x => x.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task Invoke_WithCustomCanonicalPrefix_UsesCorrectBaseUrl()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Custom Blog";
        _generalSettings.Description = "Custom Description";
        _generalSettings.CanonicalPrefix = "https://custom.domain.com";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Contains("https://custom.domain.com/favicon-16x16.png", responseBody);
        Assert.Contains("https://custom.domain.com/search?term={searchTerms}", responseBody);
    }

    [Fact]
    public async Task Invoke_WithEmptyCanonicalPrefix_UsesRequestUrl()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Test Blog";
        _generalSettings.Description = "Test Description";
        _generalSettings.CanonicalPrefix = string.Empty;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Contains("https://example.com/favicon-16x16.png", responseBody);
        Assert.Contains("https://example.com/search?term={searchTerms}", responseBody);
    }

    [Fact]
    public async Task Invoke_GeneratesValidXml()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Test Blog";
        _generalSettings.Description = "Test Description";
        _generalSettings.CanonicalPrefix = "https://example.com";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync(TestContext.Current.CancellationToken);

        // Verify XML is valid by parsing it
        var xmlDoc = new XmlDocument();
        Assert.True(TryParseXml(responseBody, out xmlDoc));

        // Verify XML structure
        Assert.NotNull(xmlDoc.DocumentElement);
        Assert.Equal("OpenSearchDescription", xmlDoc.DocumentElement.Name);
        Assert.Equal("http://a9.com/-/spec/opensearch/1.1/", xmlDoc.DocumentElement.NamespaceURI);

        // Create namespace manager for XPath queries
        var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("os", "http://a9.com/-/spec/opensearch/1.1/");

        var shortNameNode = xmlDoc.SelectSingleNode("//os:ShortName", namespaceManager);
        Assert.NotNull(shortNameNode);
        Assert.Equal("Test Blog", shortNameNode.InnerText);

        var descriptionNode = xmlDoc.SelectSingleNode("//os:Description", namespaceManager);
        Assert.NotNull(descriptionNode);
        Assert.Equal("Test Description", descriptionNode.InnerText);

        var imageNode = xmlDoc.SelectSingleNode("//os:Image", namespaceManager);
        Assert.NotNull(imageNode);
        Assert.Equal("16", imageNode.Attributes["height"]?.Value);
        Assert.Equal("16", imageNode.Attributes["width"]?.Value);
        Assert.Equal("image/png", imageNode.Attributes["type"]?.Value);

        var urlNode = xmlDoc.SelectSingleNode("//os:Url", namespaceManager);
        Assert.NotNull(urlNode);
        Assert.Equal("text/html", urlNode.Attributes["type"]?.Value);
        Assert.Contains("{searchTerms}", urlNode.Attributes["template"]?.Value);
    }

    [Fact]
    public async Task Invoke_WithSpecialCharactersInTitleAndDescription_EscapesXmlProperly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Test & Blog <Special>";
        _generalSettings.Description = "Description with \"quotes\" & ampersands";
        _generalSettings.CanonicalPrefix = "https://example.com";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync(TestContext.Current.CancellationToken);

        // Verify XML is still valid with special characters
        var xmlDoc = new XmlDocument();
        Assert.True(TryParseXml(responseBody, out xmlDoc));

        // Create namespace manager for XPath queries
        var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("os", "http://a9.com/-/spec/opensearch/1.1/");

        var shortNameNode = xmlDoc.SelectSingleNode("//os:ShortName", namespaceManager);
        Assert.Equal("Test & Blog <Special>", shortNameNode.InnerText);

        var descriptionNode = xmlDoc.SelectSingleNode("//os:Description", namespaceManager);
        Assert.Equal("Description with \"quotes\" & ampersands", descriptionNode.InnerText);
    }

    [Fact]
    public async Task Invoke_WithCustomMiddlewareOptions_UsesCustomValues()
    {
        // Arrange
        OpenSearchMiddleware.Options = new OpenSearchMiddlewareOptions
        {
            RequestPath = "/custom-search",
            IconFileType = "image/x-icon",
            IconFilePath = "/custom-icon.ico",
            Xmlns = "http://custom.namespace.com/"
        };

        var context = new DefaultHttpContext();
        context.Request.Path = "/custom-search";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Test Blog";
        _generalSettings.Description = "Test Description";
        _generalSettings.CanonicalPrefix = "https://example.com";

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        await _middleware.Invoke(context, _mockBlogConfig.Object);

        // Assert
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync(TestContext.Current.CancellationToken);

        Assert.Contains("https://example.com/custom-icon.ico", responseBody);
        Assert.Contains("image/x-icon", responseBody);

        var xmlDoc = new XmlDocument();
        Assert.True(TryParseXml(responseBody, out xmlDoc));
        Assert.Equal("http://custom.namespace.com/", xmlDoc.DocumentElement.NamespaceURI);
    }

    [Fact]
    public async Task Invoke_RespectsCancellationToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/opensearch";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com");

        _advancedSettings.EnableOpenSearch = true;
        _generalSettings.SiteTitle = "Test Blog";
        _generalSettings.Description = "Test Description";
        _generalSettings.CanonicalPrefix = "https://example.com";

        var cts = new CancellationTokenSource();
        context.RequestAborted = cts.Token;

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        // Act
        cts.Cancel();

        // Assert - Should throw TaskCanceledException when cancellation token is cancelled
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _middleware.Invoke(context, _mockBlogConfig.Object));
    }

    private static bool TryParseXml(string xml, out XmlDocument document)
    {
        document = new XmlDocument();
        try
        {
            document.LoadXml(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }
}