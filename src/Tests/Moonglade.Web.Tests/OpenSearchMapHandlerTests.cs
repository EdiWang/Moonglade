using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Web.Handlers;
using System.Text;
using System.Xml.Linq;

namespace Moonglade.Web.Tests;

public class OpenSearchMapHandlerTests
{
    [Fact]
    public async Task Handle_WhenOpenSearchIsDisabled_ReturnsNotFound()
    {
        var httpContext = new DefaultHttpContext();
        var blogConfig = new BlogConfig
        {
            AdvancedSettings = new AdvancedSettings
            {
                EnableOpenSearch = false
            }
        };

        await OpenSearchMapHandler.Handle(httpContext, blogConfig);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Null(httpContext.Response.ContentType);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task Handle_WhenOpenSearchIsEnabled_WritesExpectedXmlResponse()
    {
        var responseBody = new MemoryStream();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("blog.example.com");
        httpContext.Response.Body = responseBody;

        var blogConfig = new BlogConfig
        {
            GeneralSettings = new GeneralSettings
            {
                SiteTitle = "My Blog",
                Description = "Search the blog",
                CanonicalPrefix = "https://canonical.example.com/base/"
            },
            AdvancedSettings = new AdvancedSettings
            {
                EnableOpenSearch = true
            }
        };

        await OpenSearchMapHandler.Handle(httpContext, blogConfig);

        Assert.Equal("text/xml", httpContext.Response.ContentType);
        Assert.Equal("public, max-age=3600", httpContext.Response.Headers.CacheControl.ToString());

        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
        var xml = await reader.ReadToEndAsync();

        var document = XDocument.Parse(xml);
        XNamespace ns = "http://a9.com/-/spec/opensearch/1.1/";

        Assert.Equal("OpenSearchDescription", document.Root?.Name.LocalName);
        Assert.Equal("My Blog", document.Root?.Element(ns + "ShortName")?.Value);
        Assert.Equal("Search the blog", document.Root?.Element(ns + "Description")?.Value);

        var imageElement = document.Root?.Element(ns + "Image");
        Assert.NotNull(imageElement);
        Assert.Equal("16", imageElement.Attribute("height")?.Value);
        Assert.Equal("16", imageElement.Attribute("width")?.Value);
        Assert.Equal("image/png", imageElement.Attribute("type")?.Value);
        Assert.Equal("https://canonical.example.com/base/favicon-16x16.png", imageElement.Value);

        var urlElement = document.Root?.Element(ns + "Url");
        Assert.NotNull(urlElement);
        Assert.Equal("text/html", urlElement.Attribute("type")?.Value);
        Assert.Equal("https://canonical.example.com/base/search?term={searchTerms}", urlElement.Attribute("template")?.Value);
    }
}