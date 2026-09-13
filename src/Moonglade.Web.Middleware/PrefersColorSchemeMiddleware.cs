using Microsoft.AspNetCore.Http;

namespace Moonglade.Web.Middleware;

public class PrefersColorSchemeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        const string headerName = "Sec-CH-Prefers-Color-Scheme";

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("Vary", headerName);
            context.Response.Headers["Accept-CH"] = headerName;
            context.Response.Headers["Critical-CH"] = headerName;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
