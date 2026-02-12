using Edi.AspNetCore.Utils;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;
using System.Text.RegularExpressions;

namespace Moonglade.Webmention;

public partial class WebmentionSender(
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

            if (!ContainsUrl(postContent)) return;

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
        catch (Exception e)
        {
            logger.LogError(e, "{MethodName}({PostUrl})", nameof(SendWebmentionAsync), postUrl);
        }
    }

    private async Task SendAsync(Uri sourceUrl, Uri targetUrl)
    {
        ArgumentNullException.ThrowIfNull(sourceUrl);
        ArgumentNullException.ThrowIfNull(targetUrl);

        try
        {
            var endpoint = await DiscoverWebmentionEndpoint(targetUrl);
            if (endpoint is null)
            {
                logger.LogWarning("Webmention endpoint not found for '{TargetUrl}'.", targetUrl);
                return;
            }

            logger.LogInformation("Found Webmention service URL '{Endpoint}' on target '{TargetUrl}'", endpoint, targetUrl);

            // Resolve relative URLs against the target
            if (!Uri.TryCreate(targetUrl, endpoint, out var endpointUrl))
            {
                logger.LogWarning("Invalid Webmention service URL '{Endpoint}'", endpoint);
                return;
            }

            var wmResponse = await requestor.Send(sourceUrl, targetUrl, endpointUrl);

            if (!wmResponse.IsSuccessStatusCode)
            {
                logger.LogError("Webmention request failed: {StatusCode}", wmResponse.StatusCode);
            }
            else
            {
                logger.LogInformation("Webmention request successful: {StatusCode}", wmResponse.StatusCode);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "{MethodName}({SourceUrl}, {TargetUrl})", nameof(SendAsync), sourceUrl, targetUrl);
        }
    }

    private async Task<string> DiscoverWebmentionEndpoint(Uri targetUrl)
    {
        using var response = await httpClient.GetAsync(targetUrl, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode) return null;

        // 1. Check HTTP Link header first (per W3C Webmention spec)
        if (response.Headers.TryGetValues("Link", out var linkHeaders))
        {
            foreach (var header in linkHeaders)
            {
                var linkMatch = LinkHeaderRegex().Match(header);
                if (linkMatch.Success)
                {
                    return linkMatch.Groups[1].Value;
                }
            }
        }

        // 2. Fall back to HTML <link> tag
        var html = await response.Content.ReadAsStringAsync();
        var match = HtmlLinkRegex().Match(html);

        return match.Success ? match.Groups["href"].Value : null;
    }

    private static bool ContainsUrl(string content) =>
        content.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
        content.Contains("https://", StringComparison.OrdinalIgnoreCase);

    // Matches: <url>; rel="webmention"  or  <url>; rel=webmention
    [GeneratedRegex("""<([^>]+)>;\s*rel="?webmention"?""", RegexOptions.IgnoreCase)]
    private static partial Regex LinkHeaderRegex();

    // Matches <link> with rel="webmention" regardless of attribute order
    [GeneratedRegex("""<link\s[^>]*rel=["']webmention["'][^>]*href=["'](?<href>[^"']+)["']|<link\s[^>]*href=["'](?<href>[^"']+)["'][^>]*rel=["']webmention["']""", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlLinkRegex();
}