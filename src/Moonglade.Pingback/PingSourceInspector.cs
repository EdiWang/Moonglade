using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;

namespace Moonglade.Pingback;

public interface IPingSourceInspector
{
    Task<PingRequest> ExamineSourceAsync(string sourceUrl, string targetUrl);
}

public class PingSourceInspector : IPingSourceInspector
{
    private readonly ILogger<PingSourceInspector> _logger;
    private readonly HttpClient _httpClient;

    public PingSourceInspector(ILogger<PingSourceInspector> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<PingRequest> ExamineSourceAsync(string sourceUrl, string targetUrl)
    {
        try
        {
            var regexHtml = new Regex(
                @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>",
                RegexOptions.Singleline | RegexOptions.Compiled);

            var regexTitle = new Regex(
                @"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var html = await _httpClient.GetStringAsync(sourceUrl);
            var title = regexTitle.Match(html).Value.Trim();
            var containsHtml = regexHtml.IsMatch(title);
            var sourceHasLink = html.ToUpperInvariant().Contains(targetUrl.ToUpperInvariant());

            var pingRequest = new PingRequest
            {
                Title = title,
                ContainsHtml = containsHtml,
                SourceHasLink = sourceHasLink,
                TargetUrl = targetUrl,
                SourceUrl = sourceUrl
            };

            return pingRequest;
        }
        catch (WebException ex)
        {
            _logger.LogError(ex, nameof(ExamineSourceAsync));
            return null;
        }
    }
}