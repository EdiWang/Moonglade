using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Moonglade.Syndication
{
    public class StringOpmlWriter : IOpmlWriter
    {
        public async Task<string> GetOpmlDataAsync(OpmlDoc opmlDoc)
        {
            var sb = new StringBuilder();

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
            await using (var writer = XmlWriter.Create(sb, writerSettings))
            {
                // open OPML
                writer.WriteStartElement("opml");

                await writer.WriteAttributeStringAsync("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                await writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("version", "1.0");

                // open HEAD
                writer.WriteStartElement("head");
                writer.WriteStartElement("title");
                writer.WriteValue(opmlDoc.SiteTitle);
                await writer.WriteEndElementAsync();
                await writer.WriteEndElementAsync();

                // open BODY
                writer.WriteStartElement("body");

                // allrss
                writer.WriteStartElement("outline");
                writer.WriteAttributeString("title", "All Posts");
                writer.WriteAttributeString("text", "All Posts");
                writer.WriteAttributeString("type", "rss");
                writer.WriteAttributeString("xmlUrl", opmlDoc.XmlUrl);
                writer.WriteAttributeString("htmlUrl", opmlDoc.HtmlUrl);
                await writer.WriteEndElementAsync();

                // categories
                foreach (var cat in opmlDoc.ContentInfo)
                {
                    // open OUTLINE
                    writer.WriteStartElement("outline");

                    writer.WriteAttributeString("title", cat.Key);
                    writer.WriteAttributeString("text", cat.Value);
                    writer.WriteAttributeString("type", "rss");
                    writer.WriteAttributeString("xmlUrl", opmlDoc.XmlUrlTemplate.Replace("[catTitle]", cat.Value).ToLower());
                    writer.WriteAttributeString("htmlUrl", opmlDoc.HtmlUrlTemplate.Replace("[catTitle]", cat.Value).ToLower());

                    // close OUTLINE
                    await writer.WriteEndElementAsync();
                }

                // close BODY
                await writer.WriteEndElementAsync();

                // close OPML
                await writer.WriteEndElementAsync();
            }

            var xml = sb.ToString();
            return xml;
        }
    }
}
