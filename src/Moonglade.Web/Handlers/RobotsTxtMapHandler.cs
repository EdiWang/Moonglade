namespace Moonglade.Web.Handlers;

public class RobotsTxtMapHandler
{
    public static Delegate Handler => Handle;

    public static async Task Handle(HttpContext httpContext, IBlogConfig blogConfig)
    {
        var robotsTxtContent = blogConfig.AdvancedSettings?.RobotsTxtContent;

        if (string.IsNullOrWhiteSpace(robotsTxtContent))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            httpContext.Response.ContentType = null; // Make sure no content type is set
            await httpContext.Response.WriteAsync("No robots.txt is present.", httpContext.RequestAborted);
        }
        else
        {
            httpContext.Response.ContentType = "text/plain; charset=utf-8";
            await httpContext.Response.WriteAsync(robotsTxtContent, httpContext.RequestAborted);
        }
    }
}
