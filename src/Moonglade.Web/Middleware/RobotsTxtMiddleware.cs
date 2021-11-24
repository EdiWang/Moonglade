using System.Text;

namespace Moonglade.Web.Middleware;

public class RobotsTxtMiddleware
{
    private readonly RequestDelegate _next;

    public RobotsTxtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
    {
        // Double check path to prevent user from wrong usage like adding the middleware manually without MapWhen
        if (httpContext.Request.Path == "/robots.txt")
        {
            var robotsTxtContent = blogConfig.AdvancedSettings.RobotsTxtContent;
            if (string.IsNullOrWhiteSpace(robotsTxtContent))
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsync("No robots.txt is present.", httpContext.RequestAborted);
            }

            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(blogConfig.AdvancedSettings.RobotsTxtContent, Encoding.UTF8, httpContext.RequestAborted);
        }
        else
        {
            await _next(httpContext);
        }
    }
}

public static partial class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRobotsTxt(this IApplicationBuilder builder)
    {
        return builder.MapWhen(
            context => context.Request.Path == "/robots.txt",
            p => p.UseMiddleware<RobotsTxtMiddleware>());
    }
}