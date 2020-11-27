using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback
{
    public class PingbackService : IPingbackService
    {
        private readonly ILogger<PingbackService> _logger;
        private readonly IPingSourceInspector _pingSourceInspector;
        private readonly IPingbackRepository _pingbackRepository;

        private readonly IDbConnection _dbConnection;
        private string _sourceUrl;
        private string _targetUrl;

        public PingbackService(
            ILogger<PingbackService> logger,
            IDbConnection dbConnection,
            IPingSourceInspector pingSourceInspector,
            IPingbackRepository pingbackRepository)
        {
            _logger = logger;
            _pingSourceInspector = pingSourceInspector;
            _pingbackRepository = pingbackRepository;
            _dbConnection = dbConnection;
        }

        public async Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackRecord> pingSuccessAction)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogError("Pingback requestBody is null");
                    return PingbackResponse.GenericError;
                }

                var valid = ValidatePingRequest(requestBody);
                if (!valid) return PingbackResponse.InvalidPingRequest;

                _logger.LogInformation($"Processing Pingback from: {_sourceUrl} ({ip}) to {_targetUrl}");

                var pingRequest = await _pingSourceInspector.ExamineSourceAsync(_sourceUrl, _targetUrl);
                if (null == pingRequest) return PingbackResponse.InvalidPingRequest;
                if (!pingRequest.SourceHasLink)
                {
                    _logger.LogError("Pingback error: The source URI does not contain a link to the target URI.");
                    return PingbackResponse.Error17SourceNotContainTargetUri;
                }
                if (pingRequest.ContainsHtml)
                {
                    _logger.LogWarning("Spam detected on current Pingback...");
                    return PingbackResponse.SpamDetectedFakeNotFound;
                }

                var (Id, Title) = await _pingbackRepository.GetPostIdTitle(pingRequest.TargetUrl, _dbConnection);
                if (Id == Guid.Empty)
                {
                    _logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                    return PingbackResponse.Error32TargetUriNotExist;
                }
                _logger.LogInformation($"Post '{Id}:{Title}' is found for ping.");

                var pinged = await _pingbackRepository.HasAlreadyBeenPinged(Id, pingRequest.SourceUrl, ip, _dbConnection);
                if (pinged) return PingbackResponse.Error48PingbackAlreadyRegistered;

                _logger.LogInformation("Adding received pingback...");

                var uri = new Uri(_sourceUrl);
                var obj = new PingbackRecord
                {
                    Id = Guid.NewGuid(),
                    PingTimeUtc = DateTime.UtcNow,
                    Domain = uri.Host,
                    SourceUrl = _sourceUrl,
                    SourceTitle = pingRequest.Title,
                    TargetPostId = Id,
                    TargetPostTitle = Title,
                    SourceIp = ip
                };

                await _pingbackRepository.SavePingbackRecordAsync(obj, _dbConnection);
                pingSuccessAction?.Invoke(obj);

                return PingbackResponse.Success;
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(ReceivePingAsync));
                return PingbackResponse.GenericError;
            }
        }

        public async Task<IEnumerable<PingbackRecord>> GetPingbackHistoryAsync()
        {
            try
            {
                var list = await _pingbackRepository.GetPingbackHistoryAsync(_dbConnection);
                return list;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error {nameof(GetPingbackHistoryAsync)}");
                throw;
            }
        }

        public async Task DeletePingbackHistory(Guid id)
        {
            try
            {
                await _pingbackRepository.DeletePingbackHistory(id, _dbConnection);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error {nameof(DeletePingbackHistory)}");
                throw;
            }
        }

        private bool ValidatePingRequest(string requestBody)
        {
            _logger.LogInformation($"Pingback received xml: {requestBody}");

            if (!requestBody.Contains("<methodName>pingback.ping</methodName>"))
            {
                _logger.LogWarning("Could not find pingback method, request has been terminated.");
                return false;
            }

            var doc = new XmlDocument();
            doc.LoadXml(requestBody);

            var list = doc.SelectNodes("methodCall/params/param/value/string") ??
                       doc.SelectNodes("methodCall/params/param/value");

            if (list is null)
            {
                _logger.LogWarning("Could not find Pingback sourceUrl and targetUrl, request has been terminated.");
                return false;
            }

            _sourceUrl = list[0]?.InnerText.Trim();
            _targetUrl = list[1]?.InnerText.Trim();

            return true;
        }
    }
}
