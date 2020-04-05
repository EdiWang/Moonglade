using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Moonglade.OpmlFileWriter
{
    public class FileSystemOpmlWriter : IFileSystemOpmlWriter
    {
        public async Task WriteOpmlFileAsync(string opmlFilePath, OpmlInfo opmlInfo)
        {
            using (var fs = new FileStream(opmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
                using (var writer = XmlWriter.Create(fs, writerSettings))
                {
                    // open OPML
                    writer.WriteStartElement("opml");

                    writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
                    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    writer.WriteAttributeString("version", "1.0");

                    // open HEAD
                    writer.WriteStartElement("head");
                    writer.WriteStartElement("title");
                    writer.WriteValue(opmlInfo.SiteTitle);
                    writer.WriteEndElement();
                    writer.WriteEndElement();

                    // open BODY
                    writer.WriteStartElement("body");

                    // allrss
                    writer.WriteStartElement("outline");
                    writer.WriteAttributeString("title", "All Posts");
                    writer.WriteAttributeString("text", "All Posts");
                    writer.WriteAttributeString("type", "rss");
                    writer.WriteAttributeString("xmlUrl", opmlInfo.XmlUrl);
                    writer.WriteAttributeString("htmlUrl", opmlInfo.HtmlUrl);
                    writer.WriteEndElement();

                    // categories
                    foreach (var cat in opmlInfo.CategoryInfo)
                    {
                        // open OUTLINE
                        writer.WriteStartElement("outline");

                        writer.WriteAttributeString("title", cat.DisplayName);
                        writer.WriteAttributeString("text", cat.Title);
                        writer.WriteAttributeString("type", "rss");
                        writer.WriteAttributeString("xmlUrl", opmlInfo.CategoryXmlUrlTemplate.Replace("[catTitle]", cat.Title).ToLower());
                        writer.WriteAttributeString("htmlUrl", opmlInfo.CategoryHtmlUrlTemplate.Replace("[catTitle]", cat.Title).ToLower());

                        // close OUTLINE
                        writer.WriteEndElement();
                    }

                    // close BODY
                    writer.WriteEndElement();

                    // close OPML
                    writer.WriteEndElement();
                }
                await fs.FlushAsync();
            }
        }
    }
}
