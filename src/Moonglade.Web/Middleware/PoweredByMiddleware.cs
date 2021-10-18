using Moonglade.Utils;

namespace Moonglade.Web.Middleware
{
    public class PoweredByMiddleware
    {
        private readonly RequestDelegate _next;

        public PoweredByMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            httpContext.Response.Headers["X-Powered-By"] = $"Moonglade {Helper.AppVersion}, ASP.NET Core";
            return _next.Invoke(httpContext);
        }
    }
}
