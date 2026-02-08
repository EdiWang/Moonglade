using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Moonglade.Webmention;

public interface IMentionSourceInspector
{
    Task<MentionRequest> ExamineSourceAsync(string sourceUrl, string targetUrl);
}

public partial class MentionSourceInspector(ILogger<MentionSourceInspector> logger, HttpClient httpClient) : IMentionSourceInspector
{
    private const int MaxResponseSizeBytes = 1024 * 1024; // 1 MB

    public async Task<MentionRequest> ExamineSourceAsync(string sourceUrl, string targetUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(targetUrl))
        {
            throw new ArgumentException("Source and target URLs must be provided.");
        }

        try
        {
            var html = await FetchHtmlAsync(sourceUrl);
            if (html is null)
            {
                return null;
            }

            var title = ExtractTitle(html);
            var containsHtml = ContainsHtmlTags(title);
            var sourceHasTarget = ContainsTargetLink(html, targetUrl);

            return new MentionRequest
            {
                Title = title,
                ContainsHtml = containsHtml,
                SourceHasTarget = sourceHasTarget,
                TargetUrl = targetUrl,
                SourceUrl = sourceUrl
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error fetching or processing the URL: {SourceUrl}", sourceUrl);
            return null;
        }
    }

    private async Task<string> FetchHtmlAsync(string sourceUrl)
    {
        using var response = await httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > MaxResponseSizeBytes)
        {
            logger.LogWarning("Source URL response too large ({ContentLength} bytes): {SourceUrl}", response.Content.Headers.ContentLength, sourceUrl);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        if (content.Length > MaxResponseSizeBytes)
        {
            logger.LogWarning("Source URL content too large ({ContentLength} chars): {SourceUrl}", content.Length, sourceUrl);
            return null;
        }

        return content;
    }

    private string ExtractTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static bool ContainsHtmlTags(string input)
    {
        return HtmlTagRegex().IsMatch(input);
    }

    private static bool ContainsTargetLink(string html, string targetUrl)
    {
        var normalized = targetUrl.TrimEnd('/');
        var matches = AnchorHrefRegex().Matches(html);

        foreach (Match match in matches)
        {
            var href = match.Groups[2].Value.TrimEnd('/');
            if (href.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"<title.*?>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", RegexOptions.Singleline)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"<a\s+[^>]*?href=([""'])(.*?)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorHrefRegex();
}