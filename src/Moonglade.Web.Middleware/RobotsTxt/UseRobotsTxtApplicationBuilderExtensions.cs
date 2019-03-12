using Microsoft.AspNetCore.Builder;
using Moonglade.Web.Middleware.RobotsTxt;
using System;

namespace Moonglade.Web.Middleware
{
    public static class UseRobotsTxtApplicationBuilderExtensions
    {
        public static void UseRobotsTxt(this IApplicationBuilder app, Func<RobotsTxtOptionsBuilder, RobotsTxtOptionsBuilder> builderFunc)
        {
            var builder = new RobotsTxtOptionsBuilder();
            var options = builderFunc(builder).Build();
            app.UseRobotsTxt(options);
        }

        public static IApplicationBuilder UseRobotsTxt(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RobotsTxtMiddleware>();
        }

        public static void UseRobotsTxt(this IApplicationBuilder app, RobotsTxtOptions options)
        {
            app.UseMiddleware<RobotsTxtMiddleware>(options);
        }
    }
}
