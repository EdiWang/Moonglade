using Edi.AspNetCore.Utils;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;
using System.Net;
using System.Text.RegularExpressions;

namespace Moonglade.Webmention;

public partial class WebmentionSender(
    HttpClient httpClient,
    IWebmentionRequestor requestor,
    IPublicHttpUrlValidator publicUrlValidator,
    ILogger<WebmentionSender> logger) : IWebmentionSender
{
    private const int MaxRedirects = 5;

    public async Task SendWebmentionAsync(string postUrl, string postContent)
    {
        try
        {
            var uri = new Uri(postUrl);

            if (!await publicUrlValidator.IsPublicHttpUrlAsync(uri))
            {
                logger.LogWarning("Source URL is not public, skipping: {SourceUrl}", uri);
                return;
            }

            if (!ContainsUrl(postContent)) return;

            logger.LogInformation("URL is detected in post content, trying to send webmention requests.");

            foreach (var url in UrlHelper.GetUrlsFromContent(postContent))
            {
                if (!await publicUrlValidator.IsPublicHttpUrlAsync(url))
                {
                    logger.LogWarning("Target URL is not public, skipping: {TargetUrl}", url);
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
            var endpointUrl = await DiscoverWebmentionEndpoint(targetUrl);
            if (endpointUrl is null)
            {
                logger.LogWarning("Webmention endpoint not found for '{TargetUrl}'.", targetUrl);
                return;
            }

            logger.LogInformation("Found Webmention service URL '{Endpoint}' on target '{TargetUrl}'", endpointUrl, targetUrl);

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

    private async Task<Uri?> DiscoverWebmentionEndpoint(Uri targetUrl)
    {
        var currentUri = targetUrl;
        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            if (!await publicUrlValidator.IsPublicHttpUrlAsync(currentUri))
            {
                logger.LogWarning("Webmention discovery URL is not public, skipping: {TargetUrl}", currentUri);
                return null;
            }

            using var response = await httpClient.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead);
            if (IsRedirect(response.StatusCode))
            {
                if (redirectCount == MaxRedirects || response.Headers.Location is null)
                {
                    logger.LogWarning("Webmention discovery exceeded redirect limit or returned an invalid redirect: {TargetUrl}", currentUri);
                    return null;
                }

                currentUri = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(currentUri, response.Headers.Location);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var endpoint = await FindWebmentionEndpoint(response);
            if (endpoint is null || !Uri.TryCreate(currentUri, endpoint, out var endpointUrl))
            {
                return null;
            }

            if (!await publicUrlValidator.IsPublicHttpUrlAsync(endpointUrl))
            {
                logger.LogWarning("Webmention endpoint URL is not public, skipping: {EndpointUrl}", endpointUrl);
                return null;
            }

            return endpointUrl;
        }

        return null;
    }

    private static async Task<string?> FindWebmentionEndpoint(HttpResponseMessage response)
    {
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

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

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
