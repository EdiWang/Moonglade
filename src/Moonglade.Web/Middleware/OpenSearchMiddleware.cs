using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration.Abstraction;
using Moonglade.Utils;
using Moonglade.Web.BlogProtocols;

namespace Moonglade.Web.Middleware
{
    public class OpenSearchMiddleware
    {
        private readonly RequestDelegate _next;

        public OpenSearchMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
        {
            if (httpContext.Request.Path == "/opensearch")
            {
                var siteRootUrl = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
                var xml = await OpenSearchWriter.GetOpenSearchData(siteRootUrl, blogConfig.GeneralSettings.SiteTitle, blogConfig.GeneralSettings.Description);

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
