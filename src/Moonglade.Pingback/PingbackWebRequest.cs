using System.Net;
using System.Text;
using System.Xml;

namespace Moonglade.Pingback
{
    public interface IPingbackWebRequest
    {
        HttpWebRequest BuildHttpWebRequest(Uri sourceUrl, Uri targetUrl, Uri url);
        WebResponse GetReponse(HttpWebRequest request);
    }

    public class PingbackWebRequest : IPingbackWebRequest
    {
        public HttpWebRequest BuildHttpWebRequest(Uri sourceUrl, Uri targetUrl, Uri url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "POST";
            httpWebRequest.Timeout = 30 * 1000;
            httpWebRequest.ContentType = "text/xml";
            httpWebRequest.ProtocolVersion = HttpVersion.Version11;
            httpWebRequest.Headers["Accept-Language"] = "en-us";
            AddXmlToRequest(sourceUrl, targetUrl, httpWebRequest);
            return httpWebRequest;
        }

        public WebResponse GetReponse(HttpWebRequest request)
        {
            var webResponse = request.GetResponse();
            webResponse.Close();
            return webResponse;
        }

        private static void AddXmlToRequest(Uri sourceUrl, Uri targetUrl, WebRequest webreqPing)
        {
            var stream = webreqPing.GetRequestStream();
            using var writer = new XmlTextWriter(stream, Encoding.ASCII);
            writer.WriteStartDocument(true);
            writer.WriteStartElement("methodCall");
            writer.WriteElementString("methodName", "pingback.ping");
            writer.WriteStartElement("params");

            writer.WriteStartElement("param");
            writer.WriteStartElement("value");
            writer.WriteElementString("string", sourceUrl.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("param");
            writer.WriteStartElement("value");
            writer.WriteElementString("string", targetUrl.ToString());
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.WriteEndElement();
        }
    }
}
