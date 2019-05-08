using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Moonglade.Web.Middleware.PoweredBy
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
            httpContext.Response.Headers["X-Powered-By"] = "Moonglade";
            return _next.Invoke(httpContext);
        }
    }
}
