using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Moonglade.Web.Middleware.RobotsTxt
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