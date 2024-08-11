namespace Moonglade.Web.Handlers;

public class RobotsTxtMapHandler
{
    public static Delegate Handler => async (HttpContext httpContext, IBlogConfig blogConfig) =>
    {
        await Handle(httpContext, blogConfig);
    };

    public static async Task Handle(HttpContext httpContext, IBlogConfig blogConfig)
    {
        var robotsTxtContent = blogConfig.AdvancedSettings.RobotsTxtContent;
        if (string.IsNullOrWhiteSpace(robotsTxtContent))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync("No robots.txt is present.", httpContext.RequestAborted);
        }
        else
        {
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(robotsTxtContent, Encoding.UTF8, httpContext.RequestAborted);
        }
    }
}