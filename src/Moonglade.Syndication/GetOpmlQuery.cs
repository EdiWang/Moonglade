using LiteBus.Queries.Abstractions;
using System.Text;
using System.Xml;

namespace Moonglade.Syndication;

public record GetOpmlQuery(OpmlDoc OpmlDoc) : IQuery<string>;

public class GetOpmlQueryHandler : IQueryHandler<GetOpmlQuery, string>
{
    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    private const string OpmlVersion = "1.0";
    private const string CategoryPlaceholder = "[catTitle]";

    public async Task<string> HandleAsync(GetOpmlQuery request, CancellationToken ct)
    {
        var sb = new StringBuilder();
        var writerSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Async = true,
            Indent = true
        };

        await using (var writer = XmlWriter.Create(sb, writerSettings))
        {
            await WriteOpmlDocumentAsync(writer, request.OpmlDoc, ct);
        }

        return sb.ToString();
    }

    private static async Task WriteOpmlDocumentAsync(XmlWriter writer, OpmlDoc opmlDoc, CancellationToken ct)
    {
        await writer.WriteStartElementAsync(null, "opml", null);
        await WriteNamespaceAttributesAsync(writer);

        await WriteHeadSectionAsync(writer, opmlDoc.SiteTitle);
        await WriteBodySectionAsync(writer, opmlDoc, ct);

        await writer.WriteEndElementAsync(); // close opml
    }

    private static async Task WriteNamespaceAttributesAsync(XmlWriter writer)
    {
        await writer.WriteAttributeStringAsync("xmlns", "xsd", null, XsdNamespace);
        await writer.WriteAttributeStringAsync("xmlns", "xsi", null, XsiNamespace);
        await writer.WriteAttributeStringAsync(null, "version", null, OpmlVersion);
    }

    private static async Task WriteHeadSectionAsync(XmlWriter writer, string siteTitle)
    {
        await writer.WriteStartElementAsync(null, "head", null);
        await writer.WriteElementStringAsync(null, "title", null, siteTitle);
        await writer.WriteEndElementAsync(); // close head
    }

    private static async Task WriteBodySectionAsync(XmlWriter writer, OpmlDoc opmlDoc, CancellationToken ct)
    {
        await writer.WriteStartElementAsync(null, "body", null);

        await WriteAllPostsOutlineAsync(writer, opmlDoc);
        await WriteCategoryOutlinesAsync(writer, opmlDoc, ct);

        await writer.WriteEndElementAsync(); // close body
    }

    private static async Task WriteAllPostsOutlineAsync(XmlWriter writer, OpmlDoc opmlDoc)
    {
        await writer.WriteStartElementAsync(null, "outline", null);
        await writer.WriteAttributeStringAsync(null, "title", null, "All Posts");
        await writer.WriteAttributeStringAsync(null, "text", null, "All Posts");
        await writer.WriteAttributeStringAsync(null, "type", null, "rss");
        await writer.WriteAttributeStringAsync(null, "xmlUrl", null, opmlDoc.XmlUrl);
        await writer.WriteAttributeStringAsync(null, "htmlUrl", null, opmlDoc.HtmlUrl);
        await writer.WriteEndElementAsync(); // close outline
    }

    private static async Task WriteCategoryOutlinesAsync(XmlWriter writer, OpmlDoc opmlDoc, CancellationToken ct)
    {
        foreach (var category in opmlDoc.ContentInfo)
        {
            ct.ThrowIfCancellationRequested();

            await writer.WriteStartElementAsync(null, "outline", null);
            await writer.WriteAttributeStringAsync(null, "title", null, category.Key);
            await writer.WriteAttributeStringAsync(null, "text", null, category.Value);
            await writer.WriteAttributeStringAsync(null, "type", null, "rss");

            var xmlUrl = opmlDoc.XmlUrlTemplate.Replace(CategoryPlaceholder, category.Value, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
            var htmlUrl = opmlDoc.HtmlUrlTemplate.Replace(CategoryPlaceholder, category.Value, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

            await writer.WriteAttributeStringAsync(null, "xmlUrl", null, xmlUrl);
            await writer.WriteAttributeStringAsync(null, "htmlUrl", null, htmlUrl);
            await writer.WriteEndElementAsync(); // close outline
        }
    }
}