using Microsoft.Extensions.Logging;
using System.Net;
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
        try
        {
            var regexHtml = new Regex(
                @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>",
                RegexOptions.Singleline | RegexOptions.Compiled);

            var regexTitle = new Regex(
                @"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var html = await httpClient.GetStringAsync(sourceUrl);
            var title = regexTitle.Match(html).Value.Trim();
            var containsHtml = regexHtml.IsMatch(title);
            var sourceHasLink = html.ToUpperInvariant().Contains(targetUrl.ToUpperInvariant());

            var mentionRequest = new MentionRequest
            {
                Title = title,
                ContainsHtml = containsHtml,
                SourceHasLink = sourceHasLink,
                TargetUrl = targetUrl,
                SourceUrl = sourceUrl
            };

            return mentionRequest;
        }
        catch (WebException ex)
        {
            logger.LogError(ex, nameof(ExamineSourceAsync));
            return null;
        }
    }
}