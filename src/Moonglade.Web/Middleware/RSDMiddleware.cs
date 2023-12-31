using System.Xml;

namespace Moonglade.Web.Middleware;

// Really Simple Discovery (RSD) is a protocol or method that makes it easier for client software to automatically discover the API endpoint needed to interact with a web service. It's primarily used in the context of blog software and content management systems (CMS).
// 
// RSD allows client applications, like blog editors and content aggregators, to find the services needed to read, edit, or work with the content of a website without the user having to manually input the details of the API endpoints. For example, if you're using a desktop blogging application, RSD would enable that application to find the endpoint for the XML-RPC API of your blog so you can post directly from your desktop.

public class RSDMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, IBlogConfig blogConfig)
    {
        if (httpContext.Request.Path == "/rsd")
        {
            var siteRootUrl = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
            var xml = await GetRSDData(siteRootUrl);

            httpContext.Response.ContentType = "text/xml";
            await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);
        }
        else
        {
            await next(httpContext);
        }
    }

    private static async Task<string> GetRSDData(string siteRootUrl)
    {
        var sb = new StringBuilder();

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
        await using (var writer = XmlWriter.Create(sb, writerSettings))
        {
            await writer.WriteStartDocumentAsync();

            // Rsd tag
            writer.WriteStartElement("rsd");
            writer.WriteAttributeString("version", "1.0");

            // Service 
            writer.WriteStartElement("service");
            writer.WriteElementString("engineName", $"Moonglade {Helper.AppVersion}");
            writer.WriteElementString("engineLink", "https://github.com/EdiWang/Moonglade");
            writer.WriteElementString("homePageLink", siteRootUrl);

            // APIs
            writer.WriteStartElement("apis");

            // MetaWeblog
            writer.WriteStartElement("api");
            writer.WriteAttributeString("name", "MetaWeblog");
            writer.WriteAttributeString("preferred", "true");
            writer.WriteAttributeString("apiLink", $"{siteRootUrl}metaweblog");
            writer.WriteAttributeString("blogID", siteRootUrl);
            await writer.WriteEndElementAsync();

            // End APIs
            await writer.WriteEndElementAsync();

            // End Service
            await writer.WriteEndElementAsync();

            // End Rsd
            await writer.WriteEndElementAsync();

            await writer.WriteEndDocumentAsync();
        }

        var xml = sb.ToString();
        return xml;
    }
}