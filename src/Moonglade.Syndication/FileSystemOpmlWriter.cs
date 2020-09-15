using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Moonglade.Syndication
{
    public class FileSystemOpmlWriter : IFileSystemOpmlWriter
    {
        public async Task WriteOpmlFileAsync(string opmlFilePath, OpmlDoc opmlDoc)
        {
            await using var fs = new FileStream(opmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, Async = true };
            using (var writer = XmlWriter.Create(fs, writerSettings))
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
                foreach (var cat in opmlDoc.CategoryInfo)
                {
                    // open OUTLINE
                    writer.WriteStartElement("outline");

                    writer.WriteAttributeString("title", cat.DisplayName);
                    writer.WriteAttributeString("text", cat.Title);
                    writer.WriteAttributeString("type", "rss");
                    writer.WriteAttributeString("xmlUrl", opmlDoc.CategoryXmlUrlTemplate.Replace("[catTitle]", cat.Title).ToLower());
                    writer.WriteAttributeString("htmlUrl", opmlDoc.CategoryHtmlUrlTemplate.Replace("[catTitle]", cat.Title).ToLower());

                    // close OUTLINE
                    await writer.WriteEndElementAsync();
                }

                // close BODY
                await writer.WriteEndElementAsync();

                // close OPML
                await writer.WriteEndElementAsync();
            }
            await fs.FlushAsync();
        }
    }
}
