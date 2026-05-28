using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Web.Handlers;
using System.Text;

namespace Moonglade.Web.Tests;

public class RobotsTxtMapHandlerTests
{
    [Fact]
    public async Task Handle_WhenRobotsTxtContentIsMissing_ReturnsNotFound()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var blogConfig = new BlogConfig
        {
            AdvancedSettings = new AdvancedSettings
            {
                RobotsTxtContent = string.Empty
            }
        };

        var result = RobotsTxtMapHandler.Handle(httpContext, blogConfig);

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.Headers.ContainsKey("Cache-Control"));
    }

    [Fact]
    public async Task Handle_WhenRobotsTxtContentExists_ReturnsTextResponseWithCacheHeader()
    {
        const string robotsTxt = "User-agent: *\nDisallow: /admin";

        var responseBody = new MemoryStream();
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = responseBody;
        httpContext.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var blogConfig = new BlogConfig
        {
            AdvancedSettings = new AdvancedSettings
            {
                RobotsTxtContent = robotsTxt
            }
        };

        var result = RobotsTxtMapHandler.Handle(httpContext, blogConfig);

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
        Assert.Equal("public, max-age=86400", httpContext.Response.Headers.CacheControl.ToString());

        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(robotsTxt, body);
    }
}