using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration.Abstraction;
using Moonglade.Utils;
using Moonglade.Web.BlogProtocols;

namespace Moonglade.Web.Middleware
{
    public class OpenSearchMiddleware
    {
        private readonly RequestDelegate _next;
        public static OpenSearchMiddlewareOptions Options { get; set; } = new();

        public OpenSearchMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
        {
            if (httpContext.Request.Path == Options.RequestPath && blogConfig.AdvancedSettings.EnableOpenSearch)
            {
                var siteRootUrl = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
                var xml = await OpenSearchWriter.GetOpenSearchData(siteRootUrl, blogConfig.GeneralSettings.SiteTitle, blogConfig.GeneralSettings.Description);

                httpContext.Response.ContentType = "text/xml";
                await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);
            }
            else
            {
                await _next(httpContext);
            }
        }
    }

    public static class OpenSearchMiddlewareOptionsExtensions
    {
        public static IApplicationBuilder UseOpenSearch(this IApplicationBuilder app, Action<OpenSearchMiddlewareOptions> options)
        {
            options(OpenSearchMiddleware.Options);
            return app.UseMiddleware<OpenSearchMiddleware>();
        }
    }

    public class OpenSearchMiddlewareOptions
    {
        public PathString RequestPath { get; set; }
    }
}
