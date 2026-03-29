using System.Xml;

namespace Moonglade.Web.Handlers;

public class OpenSearchMapHandler
{
    private const string Xmlns = "http://a9.com/-/spec/opensearch/1.1/";
    private const string ContentType = "text/xml";
    private const string IconFileType = "image/png";
    private const string IconFilePath = "/favicon-16x16.png";

    public static Delegate Handler => Handle;

    public static async Task Handle(HttpContext httpContext, IBlogConfig blogConfig)
    {
        if (!blogConfig.AdvancedSettings.EnableOpenSearch)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var hasCanonicalPrefix = !string.IsNullOrWhiteSpace(blogConfig.GeneralSettings.CanonicalPrefix);
        var siteRootUrl = UrlHelper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, hasCanonicalPrefix);
        var baseUrl = siteRootUrl.TrimEnd('/');

        await using var ms = new MemoryStream();
        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
        await using (var writer = XmlWriter.Create(ms, writerSettings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "OpenSearchDescription", Xmlns);

            await writer.WriteElementStringAsync(null, "ShortName", null, blogConfig.GeneralSettings.SiteTitle);
            await writer.WriteElementStringAsync(null, "Description", null, blogConfig.GeneralSettings.Description);

            await writer.WriteStartElementAsync(null, "Image", null);
            await writer.WriteAttributeStringAsync(null, "height", null, "16");
            await writer.WriteAttributeStringAsync(null, "width", null, "16");
            await writer.WriteAttributeStringAsync(null, "type", null, IconFileType);
            await writer.WriteStringAsync($"{baseUrl}{IconFilePath}");
            await writer.WriteEndElementAsync();

            await writer.WriteStartElementAsync(null, "Url", null);
            await writer.WriteAttributeStringAsync(null, "type", null, "text/html");
            await writer.WriteAttributeStringAsync(null, "template", null, $"{baseUrl}/search?term={{searchTerms}}");
            await writer.WriteEndElementAsync();

            await writer.WriteEndElementAsync();
            await writer.FlushAsync();
        }

        httpContext.Response.ContentType = ContentType;
        httpContext.Response.Headers.CacheControl = "public, max-age=3600";

        ms.Seek(0, SeekOrigin.Begin);
        await ms.CopyToAsync(httpContext.Response.Body, httpContext.RequestAborted);
    }
}
