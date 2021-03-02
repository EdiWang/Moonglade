using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Moonglade.Web.BlogProtocols
{
    public class OpenSearchWriter
    {
        public static async Task<byte[]> GetOpenSearchData(string siteRootUrl, string shortName, string description)
        {
            await using var ms = new MemoryStream();
            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
            await using (var writer = XmlWriter.Create(ms, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("OpenSearchDescription", "http://a9.com/-/spec/opensearch/1.1/");
                writer.WriteAttributeString("xmlns", "http://a9.com/-/spec/opensearch/1.1/");

                writer.WriteElementString("ShortName", shortName);
                writer.WriteElementString("Description", description);

                writer.WriteStartElement("Image");
                writer.WriteAttributeString("height", "16");
                writer.WriteAttributeString("width", "16");
                writer.WriteAttributeString("type", "image/vnd.microsoft.icon");
                writer.WriteValue($"{siteRootUrl.TrimEnd('/')}/favicon.ico");
                await writer.WriteEndElementAsync();

                writer.WriteStartElement("Url");
                writer.WriteAttributeString("type", "text/html");
                writer.WriteAttributeString("template", $"{siteRootUrl.TrimEnd('/')}/search/{{searchTerms}}");
                await writer.WriteEndElementAsync();

                await writer.WriteEndElementAsync();
            }
            await ms.FlushAsync();
            return ms.ToArray();
        }
    }
}
