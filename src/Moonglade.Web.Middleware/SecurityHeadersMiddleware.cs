using Microsoft.AspNetCore.Http;

namespace Moonglade.Web.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext httpContext)
    {
        httpContext.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        return next.Invoke(httpContext);
    }
}
