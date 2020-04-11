using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback
{
    public class PingbackSender : IPingbackSender
    {
        public ILogger<PingbackSender> Logger { get; set; }

        public PingbackSender(ILogger<PingbackSender> logger = null)
        {
            Logger = logger;
        }

        public async Task TrySendPingAsync(string postUrl, string postContent)
        {
            try
            {
                var uri = new Uri(postUrl);
                var content = postContent.ToUpperInvariant();
                if (content.Contains("HTTP://") || content.Contains("HTTPS://"))
                {
                    Logger?.LogInformation("URL is detected in post content, trying to send ping requests.");

                    foreach (var url in GetUrlsFromContent(postContent))
                    {
                        Logger?.LogInformation("Pinging URL: " + url);
                        try
                        {
                            await SendAsync(uri, url);
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError(e, "SendAsync Ping Error.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"{nameof(TrySendPingAsync)}({postUrl})");
            }
        }

        private async Task SendAsync(Uri sourceUrl, Uri targetUrl)
        {
            if (sourceUrl == null || targetUrl == null)
            {
                return;
            }

            try
            {
                // TODO: Shit code, need to use HttpClientFactory if possible
                using var handler = new HttpClientHandler { Credentials = CredentialCache.DefaultNetworkCredentials };
                using var httpClient = new HttpClient(handler);
                var response = await httpClient.GetAsync(targetUrl);

                var pingbackHeader = response.Headers.FirstOrDefault(
                    h => h.Key.ToLower() == "x-pingback" || h.Key.ToLower() == "pingback");

                if (pingbackHeader.Key == null || pingbackHeader.Value == null)
                {
                    Logger?.LogInformation($"Pingback endpoint is not found for URL '{targetUrl}', ping request is terminated.");
                    return;
                }

                var pingUrl = pingbackHeader.Value.FirstOrDefault();

                if (null != pingUrl)
                {
                    Logger?.LogInformation($"Found Ping service URL '{pingUrl}' on target '{sourceUrl}'");

                    bool successUrlCreation = Uri.TryCreate(pingUrl, UriKind.Absolute, out var url);
                    if (successUrlCreation)
                    {
                        var request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "POST";

                        // request.Timeout = 10000;
                        request.ContentType = "text/xml";
                        request.ProtocolVersion = HttpVersion.Version11;
                        request.Headers["Accept-Language"] = "en-us";
                        AddXmlToRequest(sourceUrl, targetUrl, request);
                        var response2 = (HttpWebResponse)request.GetResponse();
                        response2.Close();
                    }
                    else
                    {
                        Logger?.LogInformation($"Invliad Ping service URL '{pingUrl}'");
                    }
                }
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"{nameof(SendAsync)}({sourceUrl},{targetUrl})");
            }
        }

        private void AddXmlToRequest(Uri sourceUrl, Uri targetUrl, WebRequest webreqPing)
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

        private static readonly Regex UrlsRegex = new Regex(
            @"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static IEnumerable<Uri> GetUrlsFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentNullException(content);
            }

            content = HttpUtility.HtmlDecode(content);

            var urlsList = new List<Uri>();
            foreach (var url in
                UrlsRegex.Matches(content).Select(myMatch => myMatch.Groups["url"].ToString().Trim()))
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    urlsList.Add(uri);
                }
            }

            return urlsList;
        }
    }
}
