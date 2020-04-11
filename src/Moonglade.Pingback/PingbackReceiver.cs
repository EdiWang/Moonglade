using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback
{
    public class PingbackReceiver : IPingbackReceiver
    {
        public ILogger<PingbackReceiver> Logger { get; set; }

        public int RemoteTimeout { get; set; }

        public HttpContext HttpContext { get; private set; }

        #region Events

        public delegate void PingSuccessHandler(object sender, PingSuccessEventArgs e);
        public event PingSuccessHandler OnPingSuccess;

        #endregion

        private string _remoteIpAddress;
        private string _sourceUrl;
        private string _targetUrl;

        public PingbackReceiver(ILogger<PingbackReceiver> logger = null)
        {
            Logger = logger;
            RemoteTimeout = 30;
        }

        public async Task<PingbackValidationResult> ValidatePingRequest(HttpContext context)
        {
            try
            {
                HttpContext = context;

                Logger.LogInformation($"Receiving Pingback from {HttpContext.Connection.RemoteIpAddress}");

                var xml = await new StreamReader(HttpContext.Request.Body, Encoding.Default).ReadToEndAsync();
                Logger.LogInformation($"Pingback received xml: {xml}");
                if (!xml.Contains("<methodName>pingback.ping</methodName>"))
                {
                    return PingbackValidationResult.TerminatedMethodNotFound;
                }

                var doc = new XmlDocument();
                doc.LoadXml(xml);

                var list = doc.SelectNodes("methodCall/params/param/value/string") ??
                           doc.SelectNodes("methodCall/params/param/value");

                if (list == null)
                {
                    Logger.LogWarning("Could not find Pingback sourceUrl and targetUrl, request has been terminated.");
                    return PingbackValidationResult.TerminatedUrlNotFound;
                }

                _sourceUrl = list[0].InnerText.Trim();
                _targetUrl = list[1].InnerText.Trim();
                _remoteIpAddress = context.Connection.RemoteIpAddress.ToString();

                return PingbackValidationResult.ValidPingRequest;
            }
            catch (Exception e)
            {
                Logger.LogError(e, nameof(ValidatePingRequest));
                return PingbackValidationResult.GenericError;
            }
        }

        public async Task<PingRequest> GetPingRequest()
        {
            Logger.LogInformation($"Processing Pingback from: {_sourceUrl} to {_targetUrl}");
            var req = await ExamineSourceAsync();
            return req;
        }

        public PingbackServiceResponse ProcessReceivedPingback(PingRequest req, Func<bool> ifTargetResourceExists, Func<bool> ifAlreadyBeenPinged)
        {
            try
            {
                if (null == req)
                {
                    return PingbackServiceResponse.InvalidPingRequest;
                }

                var ti = ifTargetResourceExists();
                if (!ti) return PingbackServiceResponse.Error32TargetUriNotExist;

                var pd = ifAlreadyBeenPinged();
                if (pd) return PingbackServiceResponse.Error48PingbackAlreadyRegistered;

                if (req.SourceDocumentInfo.SourceHasLink && !req.SourceDocumentInfo.ContainsHtml)
                {
                    Logger.LogInformation("Adding received pingback...");
                    var domain = GetDomain(_sourceUrl);

                    OnPingSuccess?.Invoke(this, new PingSuccessEventArgs(domain, req));
                    return PingbackServiceResponse.Success;
                }

                if (!req.SourceDocumentInfo.SourceHasLink)
                {
                    Logger.LogError("Pingback error: The source URI does not contain a link to the target URI, and so cannot be used as a source.");
                    return PingbackServiceResponse.Error17SourceNotContainTargetUri;
                }
                Logger.LogWarning("Spam detected on current Pingback...");
                return PingbackServiceResponse.SpamDetectedFakeNotFound;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, nameof(ProcessReceivedPingback));
                return PingbackServiceResponse.GenericError;
            }
        }

        private async Task<PingRequest> ExamineSourceAsync()
        {
            try
            {
                var regexHtml = new Regex(
                    @"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>",
                    RegexOptions.Singleline | RegexOptions.Compiled);

                var regexTitle = new Regex(
                    @"(?<=<title.*>)([\s\S]*)(?=</title>)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(RemoteTimeout) })
                {
                    var html = await httpClient.GetStringAsync(_sourceUrl);
                    var title = regexTitle.Match(html).Value.Trim();
                    Logger.LogInformation($"ExamineSourceAsync:title: {title}");

                    var containsHtml = regexHtml.IsMatch(title);
                    Logger.LogInformation($"ExamineSourceAsync:containsHtml: {containsHtml}");

                    var sourceHasLink = html.ToUpperInvariant().Contains(_targetUrl.ToUpperInvariant());
                    Logger.LogInformation($"ExamineSourceAsync:sourceHasLink: {sourceHasLink}");

                    var pingRequest = new PingRequest
                    {
                        SourceDocumentInfo = new SourceDocumentInfo
                        {
                            Title = title,
                            ContainsHtml = containsHtml,
                            SourceHasLink = sourceHasLink
                        },
                        TargetUrl = _targetUrl,
                        SourceUrl = _sourceUrl,
                        SourceIpAddress = _remoteIpAddress
                    };

                    return pingRequest;
                }
            }
            catch (WebException ex)
            {
                Logger.LogError(ex, nameof(ExamineSourceAsync));
                return new PingRequest
                {
                    SourceDocumentInfo = new SourceDocumentInfo
                    {
                        SourceHasLink = false
                    },
                    SourceUrl = _sourceUrl,
                    SourceIpAddress = _remoteIpAddress
                };
            }
        }

        private static string GetDomain(string sourceUrl)
        {
            var start = sourceUrl.IndexOf("://", StringComparison.Ordinal) + 3;
            var stop = sourceUrl.IndexOf("/", start, StringComparison.Ordinal);
            return sourceUrl.Substring(start, stop - start).Replace("www.", string.Empty);
        }
    }
}
