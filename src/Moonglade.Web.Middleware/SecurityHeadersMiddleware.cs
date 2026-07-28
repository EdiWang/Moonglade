using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Moonglade.Web.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public Task Invoke(HttpContext httpContext)
    {
        httpContext.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");

        if (configuration.GetValue<bool>("EnableCSP"))
        {
            var cspValue = configuration["CSPValue"];
            if (!string.IsNullOrWhiteSpace(cspValue))
            {
                httpContext.Response.Headers.TryAdd("Content-Security-Policy", cspValue);
            }
        }

        return next.Invoke(httpContext);
    }
}
