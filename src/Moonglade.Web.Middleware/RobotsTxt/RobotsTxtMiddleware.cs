using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration.Abstraction;

namespace Moonglade.Web.Middleware.RobotsTxt
{
    public class RobotsTxtMiddleware
    {
        private readonly RequestDelegate _next;

        public RobotsTxtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
        {
            // Double check path to prevent user from wrong usage like adding the middleware manually without MapWhen
            if (httpContext.Request.Path == "/robots.txt")
            {
                var robotsTxtContent = blogConfig.AdvancedSettings.RobotsTxtContent;
                if (string.IsNullOrWhiteSpace(robotsTxtContent))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    await httpContext.Response.WriteAsync("No robots.txt is present.");
                }

                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync(blogConfig.AdvancedSettings.RobotsTxtContent, Encoding.UTF8);
            }
            else
            {
                await _next(httpContext);
            }
        }
    }
}
