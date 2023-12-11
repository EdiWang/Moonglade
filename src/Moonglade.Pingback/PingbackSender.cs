using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Moonglade.Pingback;

public class PingbackSender(HttpClient httpClient,
        IPingbackWebRequest pingbackWebRequest,
        ILogger<PingbackSender> logger = null)
    : IPingbackSender
{
    public async Task TrySendPingAsync(string postUrl, string postContent)
    {
        try
        {
            var uri = new Uri(postUrl);
            var content = postContent.ToUpperInvariant();
            if (content.Contains("HTTP://") || content.Contains("HTTPS://"))
            {
                logger?.LogInformation("URL is detected in post content, trying to send ping requests.");

                foreach (var url in GetUrlsFromContent(postContent))
                {
                    logger?.LogInformation("Pinging URL: " + url);
                    try
                    {
                        await SendAsync(uri, url);
                    }
                    catch (Exception e)
                    {
                        logger?.LogError(e, "SendAsync Ping Error.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"{nameof(TrySendPingAsync)}({postUrl})");
        }
    }

    private async Task SendAsync(Uri sourceUrl, Uri targetUrl)
    {
        if (sourceUrl is null || targetUrl is null)
        {
            return;
        }

        try
        {
            var response = await httpClient.GetAsync(targetUrl);

            var (key, value) = response.Headers.FirstOrDefault(
                h => h.Key.ToLower() == "x-pingback" || h.Key.ToLower() == "pingback");

            if (key is null || value is null)
            {
                logger?.LogInformation($"Pingback endpoint is not found for URL '{targetUrl}', ping request is terminated.");
                return;
            }

            var pingUrl = value.FirstOrDefault();
            if (pingUrl is not null)
            {
                logger?.LogInformation($"Found Ping service URL '{pingUrl}' on target '{sourceUrl}'");

                bool successUrlCreation = Uri.TryCreate(pingUrl, UriKind.Absolute, out var url);
                if (successUrlCreation)
                {
                    var pResponse = await pingbackWebRequest.Send(sourceUrl, targetUrl, url);
                }
                else
                {
                    logger?.LogInformation($"Invliad Ping service URL '{pingUrl}'");
                }
            }
        }
        catch (Exception e)
        {
            logger?.LogError(e, $"{nameof(SendAsync)}({sourceUrl},{targetUrl})");
        }
    }

    private static readonly Regex UrlsRegex = new(
        @"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static IEnumerable<Uri> GetUrlsFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentNullException(content);
        }

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