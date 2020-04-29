using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Moonglade.Web.Middleware.RouteDebugger
{
    public class RouteDebuggerMiddleware
    {
        private readonly RequestDelegate _next;

        public RouteDebuggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var rd = context.GetRouteData();
            if (null != rd && rd.Values.Any())
            {
                var rdJson = JsonSerializer.Serialize(rd.Values);
                context.Response.Headers["anc-route-debugger"] = rdJson;
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
