﻿using Microsoft.Extensions.Logging;
using Moonglade.Utils;
using System.Text.RegularExpressions;

namespace Moonglade.Webmention;

public class WebmentionSender(
    HttpClient httpClient,
    IWebmentionRequestor requestor,
    ILogger<WebmentionSender> logger) : IWebmentionSender
{
    public async Task SendWebmentionAsync(string postUrl, string postContent)
    {
        try
        {
            var uri = new Uri(postUrl);

            if (uri.IsLocalhostUrl())
            {
                logger.LogWarning("Source URL is localhost, skipping.");
                return;
            }

            var content = postContent.ToUpperInvariant();
            if (content.Contains("HTTP://") || content.Contains("HTTPS://"))
            {
                logger.LogInformation("URL is detected in post content, trying to send webmention requests.");

                foreach (var url in UrlHelper.GetUrlsFromContent(postContent))
                {
                    if (url.IsLocalhostUrl())
                    {
                        logger.LogWarning("Target URL is localhost, skipping.");
                        continue;
                    }

                    logger.LogInformation("Sending webmention to URL: {TargetUrl}", url);
                    try
                    {
                        await SendAsync(uri, url);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "SendAsync Webmention Error.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "{MethodName}({PostUrl})", nameof(SendWebmentionAsync), postUrl);
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
            string endpoint = await DiscoverWebmentionEndpoint(targetUrl.ToString());
            if (endpoint is null)
            {
                logger.LogWarning("Webmention endpoint not found for '{TargetUrl}'.", targetUrl);
                return;
            }

            logger.LogInformation("Found Webmention service URL '{Endpoint}' on target '{TargetUrl}'", endpoint, targetUrl);

            bool successUrlCreation = Uri.TryCreate(endpoint, UriKind.Absolute, out var url);
            if (successUrlCreation)
            {
                var wmResponse = await requestor.Send(sourceUrl, targetUrl, url);

                if (!wmResponse.IsSuccessStatusCode)
                {
                    logger.LogError("Webmention request failed: {StatusCode}", wmResponse.StatusCode);
                }
                else
                {
                    logger.LogInformation("Webmention request successful: {StatusCode}", wmResponse.StatusCode);
                }
            }
            else
            {
                logger.LogInformation("Invliad Webmention service URL '{Endpoint}'", endpoint);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "{MethodName}({SourceUrl}, {TargetUrl})", nameof(SendAsync), sourceUrl, targetUrl);
        }
    }

    private async Task<string> DiscoverWebmentionEndpoint(string targetUrl)
    {
        var response = await httpClient.GetAsync(targetUrl);
        if (!response.IsSuccessStatusCode) return null;

        var html = await response.Content.ReadAsStringAsync();

        // Regex to find the Webmention endpoint in the HTML
        Regex regex = new Regex("<link rel=\"webmention\" href=\"([^\"]+)\"");
        Match match = regex.Match(html);

        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }
}