namespace Moonglade.Web.Middleware;

public class PrefersColorSchemeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Accept-CH"] = "Sec-CH-Prefers-Color-Scheme";
            context.Response.Headers["Vary"] = "Sec-CH-Prefers-Color-Scheme";
            context.Response.Headers["Critical-CH"] = "Sec-CH-Prefers-Color-Scheme";
            return Task.CompletedTask;
        });

        await next(context);
    }
}