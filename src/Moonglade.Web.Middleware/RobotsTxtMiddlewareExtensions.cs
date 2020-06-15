using Microsoft.AspNetCore.Builder;

namespace Moonglade.Web.Middleware
{
    public static class RobotsTxtMiddlewareExtensions
    {
        public static IApplicationBuilder UseRobotsTxt(this IApplicationBuilder builder)
        {
            return builder.MapWhen(
                context => context.Request.Path == "/robots.txt", 
                p => p.UseMiddleware<RobotsTxtMiddleware>());
        }
    }
}