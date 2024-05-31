using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;

namespace Moonglade.Pingback;

public class PingbackSender(HttpClient httpClient,
        IPingbackRequestor requestor,
        IConfiguration configuration,
        ILogger<PingbackSender> logger)
    : IPingbackSender
{
    public async Task TrySendPingAsync(string postUrl, string postContent)
    {
        try
        {
            var uri = new Uri(postUrl);

            if (!bool.Parse(configuration["AllowPingbackFromLocalhost"]!) && uri.IsLocalhostUrl())
            {
                logger.LogWarning("Source URL is localhost, skipping.");
                return;
            }

            var content = postContent.ToUpperInvariant();
            if (content.Contains("HTTP://") || content.Contains("HTTPS://"))
            {
                logger.LogInformation("URL is detected in post content, trying to send ping requests.");

                foreach (var url in Helper.GetUrlsFromContent(postContent))
                {
                    if (!bool.Parse(configuration["AllowPingbackToLocalhost"]!) && url.IsLocalhostUrl())
                    {
                        logger.LogWarning("Target URL is localhost, skipping.");
                        continue;
                    }

                    logger.LogInformation("Pinging URL: " + url);
                    try
                    {
                        await SendAsync(uri, url);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "SendAsync Ping Error.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{nameof(TrySendPingAsync)}({postUrl})");
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
                logger.LogInformation($"Pingback endpoint is not found for URL '{targetUrl}', ping request is terminated.");
                return;
            }

            var endpoint = value.FirstOrDefault();
            if (endpoint is not null)
            {
                logger.LogInformation($"Found Ping service URL '{endpoint}' on target '{targetUrl}'");

                bool successUrlCreation = Uri.TryCreate(endpoint, UriKind.Absolute, out var url);
                if (successUrlCreation)
                {
                    var pResponse = await requestor.Send(sourceUrl, targetUrl, url);

                    if (!pResponse.IsSuccessStatusCode)
                    {
                        logger.LogError($"Ping request failed: {pResponse.StatusCode}");
                    }
                    else
                    {
                        logger.LogInformation($"Ping request successful: {pResponse.StatusCode}");
                    }
                }
                else
                {
                    logger.LogInformation($"Invliad Ping service URL '{endpoint}'");
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, $"{nameof(SendAsync)}({sourceUrl},{targetUrl})");
        }
    }
}