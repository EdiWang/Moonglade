using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Middleware
{
    public class BlogGraphAPIGuardMiddleware
    {
        private readonly RequestDelegate _next;

        public BlogGraphAPIGuardMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IOptions<AppSettings> appsettingsOptions)
        {
            if (context.Request.Path.StartsWithSegments("/api")
                && !appsettingsOptions.Value.EnableWebApi)
            {
                context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                await context.Response.WriteAsync("API is disabled", Encoding.UTF8);
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}
