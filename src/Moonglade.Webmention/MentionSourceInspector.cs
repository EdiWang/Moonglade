using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Moonglade.Mention.Common;

public interface IMentionSourceInspector
{
    Task<MentionRequest> ExamineSourceAsync(string sourceUrl, string targetUrl);
}

public class MentionSourceInspector(ILogger<MentionSourceInspector> logger, HttpClient httpClient) : IMentionSourceInspector
{
    public async Task<MentionRequest> ExamineSourceAsync(string sourceUrl, string targetUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl) || string.IsNullOrWhiteSpace(targetUrl))
        {
            throw new ArgumentException("Source and target URLs must be provided.");
        }

        try
        {
            var html = await httpClient.GetStringAsync(sourceUrl);

            var title = ExtractTitle(html);
            var containsHtml = ContainsHtmlTags(title);
            var sourceHasTarget = ContainsTargetUrl(html, targetUrl);

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

    private string ExtractTitle(string html)
    {
        var regexTitle = new Regex(@"<title.*?>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var match = regexTitle.Match(html);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private bool ContainsHtmlTags(string input)
    {
        var regexHtml = new Regex(@"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", RegexOptions.Singleline);
        return regexHtml.IsMatch(input);
    }

    private bool ContainsTargetUrl(string html, string targetUrl)
    {
        return html.IndexOf(targetUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase) >= 0;
    }
}