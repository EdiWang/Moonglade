using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback
{
    public interface IPingSourceInspector
    {
        Task<PingRequest> ExamineSourceAsync(string sourceUrl, string targetUrl, int timeoutSeconds = 30);
    }

    public class PingSourceInspector : IPingSourceInspector
    {
        private readonly ILogger<PingSourceInspector> _logger;

        public PingSourceInspector(ILogger<PingSourceInspector> logger)
        {
            _logger = logger;
        }

        public async Task<PingRequest> ExamineSourceAsync(string sourceUrl, string targetUrl, int timeoutSeconds = 30)
        {
            try
            {
                var regexHtml = new Regex(
                    @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>",
                    RegexOptions.Singleline | RegexOptions.Compiled);

                var regexTitle = new Regex(
                    @"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
                var html = await httpClient.GetStringAsync(sourceUrl);
                var title = regexTitle.Match(html).Value.Trim();
                var containsHtml = regexHtml.IsMatch(title);
                var sourceHasLink = html.ToUpperInvariant().Contains(targetUrl.ToUpperInvariant());

                var pingRequest = new PingRequest
                {
                    SourceDocumentInfo = new SourceDocumentInfo
                    {
                        Title = title,
                        ContainsHtml = containsHtml,
                        SourceHasLink = sourceHasLink
                    },
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
}
