using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;

namespace Moonglade.Web.Middleware.RouteDebugger
{
    public static class RouteDebuggerExtensions
    {
        public static IApplicationBuilder UseRouteDebugger(this IApplicationBuilder app)
        {
            app.UseMiddleware<RouteDebuggerMiddleware>();
            return app;
        }
    }
}
