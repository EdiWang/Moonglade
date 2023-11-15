namespace Moonglade.Web.Middleware;

public class PoweredByMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext httpContext)
    {
        httpContext.Response.Headers["X-Powered-By"] = $"Moonglade {Helper.AppVersion}";
        return next.Invoke(httpContext);
    }
}