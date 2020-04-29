using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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

        public async Task Invoke(HttpContext context, IActionDescriptorCollectionProvider provider = null)
        {
            if (context.Request.Path == "/anc-route-debugger")
            {
                if (null != provider)
                {
                    var routes = provider.ActionDescriptors.Items.Select(x => new {
                        Action = x.RouteValues["Action"],
                        Controller = x.RouteValues["Controller"],
                        Name = x.AttributeRouteInfo?.Name,
                        Template = x.AttributeRouteInfo?.Template,
                        Contraint = x.ActionConstraints
                    }).ToList();

                    var routesJson = JsonSerializer.Serialize(routes);

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(routesJson, Encoding.UTF8);
                }
                else
                {
                    await context.Response.WriteAsync("IActionDescriptorCollectionProvider is null", Encoding.UTF8);
                }
            }
            else
            {
                await SetCurrentRouteInfo(context);
            }
        }

        private async Task SetCurrentRouteInfo(HttpContext context)
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
                context.Response.Headers["current-route"] = rdJson;
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
