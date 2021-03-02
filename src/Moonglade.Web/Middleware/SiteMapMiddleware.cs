using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Utils;
using Moonglade.Web.BlogProtocols;

namespace Moonglade.Web.Middleware
{
    public class SiteMapMiddleware
    {
        private readonly RequestDelegate _next;

        public SiteMapMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig, IBlogCache cache, ISiteMapWriter siteMapWriter)
        {
            if (httpContext.Request.Path == "/sitemap.xml")
            {
                var xml = await cache.GetOrCreateAsync(CacheDivision.General, "sitemap", async _ =>
                {
                    var url = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
                    var data = await siteMapWriter.GetSiteMapData(url);
                    return data;
                });

                httpContext.Response.ContentType = "text/xml";
                await httpContext.Response.WriteAsync(xml);
            }
            else
            {
                await _next(httpContext);
            }
        }
    }
}
