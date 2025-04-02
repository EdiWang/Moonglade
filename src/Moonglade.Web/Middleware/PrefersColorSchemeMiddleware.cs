namespace Moonglade.Web.Middleware;

public class PrefersColorSchemeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var headerName = configuration["PrefersColorSchemeHeader:HeaderName"];
        if (string.IsNullOrWhiteSpace(headerName))
        {
            await next(context);
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Vary = headerName;
            context.Response.Headers["Accept-CH"] = headerName;
            context.Response.Headers["Critical-CH"] = headerName;
            return Task.CompletedTask;
        });

        await next(context);
    }
}