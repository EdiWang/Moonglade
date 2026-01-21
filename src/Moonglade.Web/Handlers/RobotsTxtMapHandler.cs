namespace Moonglade.Web.Handlers;

public class RobotsTxtMapHandler
{
    public static Delegate Handler => Handle;

    public static IResult Handle(HttpContext httpContext, IBlogConfig blogConfig)
    {
        var robotsTxtContent = blogConfig.AdvancedSettings?.RobotsTxtContent;

        if (string.IsNullOrWhiteSpace(robotsTxtContent))
        {
            return Results.NotFound("No robots.txt is present.");
        }

        httpContext.Response.Headers.CacheControl = "public, max-age=86400";
        return Results.Text(robotsTxtContent, "text/plain; charset=utf-8");
    }
}
