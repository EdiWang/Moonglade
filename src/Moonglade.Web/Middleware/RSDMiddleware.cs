using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration.Abstraction;
using Moonglade.Utils;
using Moonglade.Web.BlogProtocols;

namespace Moonglade.Web.Middleware
{
    public class RSDMiddleware
    {
        private readonly RequestDelegate _next;

        public RSDMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
        {
            if (httpContext.Request.Path == "/rsd")
            {
                var siteRootUrl = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
                var xml = await RSDWriter.GetRSDData(siteRootUrl);

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
