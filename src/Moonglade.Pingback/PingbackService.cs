using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.Pingback
{
    public class PingbackService : IPingbackService
    {
        private readonly ILogger<PingbackService> _logger;
        private readonly IPingSourceInspector _pingSourceInspector;
        private readonly IPingbackRepository _pingbackRepository;

        private string DatabaseConnectionString { get; }
        private string _sourceUrl;
        private string _targetUrl;

        public PingbackService(
            ILogger<PingbackService> logger,
            IConfiguration configuration,
            IPingSourceInspector pingSourceInspector,
            IPingbackRepository pingbackRepository)
        {
            _logger = logger;
            _pingSourceInspector = pingSourceInspector;
            _pingbackRepository = pingbackRepository;
            DatabaseConnectionString = configuration.GetConnectionString(Constants.DbConnectionName);
        }

        public async Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackRecord> pingSuccessAction)
        {
            try
            {
                await using var conn = new SqlConnection(DatabaseConnectionString);

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

                var postIdTitle = await _pingbackRepository.GetPostIdTitle(pingRequest.TargetUrl, conn);
                if (postIdTitle.Id == Guid.Empty)
                {
                    _logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                    return PingbackResponse.Error32TargetUriNotExist;
                }
                _logger.LogInformation($"Post '{postIdTitle.Id}:{postIdTitle.Title}' is found for ping.");

                var pinged = await _pingbackRepository.HasAlreadyBeenPinged(postIdTitle.Id, pingRequest.SourceUrl, ip, conn);
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
                    TargetPostId = postIdTitle.Id,
                    TargetPostTitle = postIdTitle.Title,
                    SourceIp = ip
                };

                await _pingbackRepository.SavePingbackRecordAsync(obj, conn);
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
                await using var conn = new SqlConnection(DatabaseConnectionString);
                var list = await _pingbackRepository.GetPingbackHistoryAsync(conn);
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
                await using var conn = new SqlConnection(DatabaseConnectionString);
                await _pingbackRepository.DeletePingbackHistory(id, conn);
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
