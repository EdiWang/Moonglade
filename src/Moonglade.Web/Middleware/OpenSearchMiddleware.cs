using Moonglade.Configuration;
using Moonglade.Utils;
using System.Text;
using System.Xml;

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
                var xml = await GetOpenSearchData(siteRootUrl, blogConfig.GeneralSettings.SiteTitle, blogConfig.GeneralSettings.Description);

                httpContext.Response.ContentType = "text/xml";
                await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);
            }
            else
            {
                await _next(httpContext);
            }
        }

        private static async Task<string> GetOpenSearchData(string siteRootUrl, string shortName, string description)
        {
            var sb = new StringBuilder();

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
            await using (var writer = XmlWriter.Create(sb, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("OpenSearchDescription", Options.Xmlns);
                writer.WriteAttributeString("xmlns", Options.Xmlns);

                writer.WriteElementString("ShortName", shortName);
                writer.WriteElementString("Description", description);

                writer.WriteStartElement("Image");
                writer.WriteAttributeString("height", "16");
                writer.WriteAttributeString("width", "16");
                writer.WriteAttributeString("type", Options.IconFileType);
                writer.WriteValue($"{siteRootUrl.TrimEnd('/')}{Options.IconFilePath}");
                await writer.WriteEndElementAsync();

                writer.WriteStartElement("Url");
                writer.WriteAttributeString("type", "text/html");
                writer.WriteAttributeString("template", $"{siteRootUrl.TrimEnd('/')}/search?term={{searchTerms}}");
                await writer.WriteEndElementAsync();

                await writer.WriteEndElementAsync();
            }

            var xml = sb.ToString();
            return xml;
        }
    }

    public static partial class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseOpenSearch(this IApplicationBuilder app, Action<OpenSearchMiddlewareOptions> options)
        {
            options(OpenSearchMiddleware.Options);
            return app.UseMiddleware<OpenSearchMiddleware>();
        }
    }

    public class OpenSearchMiddlewareOptions
    {
        public string Xmlns { get; set; } = "http://a9.com/-/spec/opensearch/1.1/";
        public string IconFileType { get; set; } // image/vnd.microsoft.icon
        public PathString IconFilePath { get; set; }
        public PathString RequestPath { get; set; }
    }
}
