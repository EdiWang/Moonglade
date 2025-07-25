using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moonglade.Mention.Common;
using Moonglade.Utils;
using System.Xml;

namespace Moonglade.Pingback;

public class ReceivePingCommand(string requestBody, string ip) : ICommand<PingbackResponse>
{
    public string RequestBody { get; set; } = requestBody;

    public string IP { get; set; } = ip;
}

public class ReceivePingCommandHandler(
        ILogger<ReceivePingCommandHandler> logger,
        IMentionSourceInspector sourceInspector,
        MoongladeRepository<MentionEntity> mentionRepo,
        MoongladeRepository<PostEntity> postRepo) : ICommandHandler<ReceivePingCommand, PingbackResponse>
{
    private string _sourceUrl;
    private string _targetUrl;

    public async Task<PingbackResponse> HandleAsync(ReceivePingCommand request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RequestBody))
            {
                logger.LogError("Pingback requestBody is null");
                return PingbackResponse.GenericError;
            }

            var valid = ValidateRequest(request.RequestBody);
            if (!valid) return PingbackResponse.InvalidPingRequest;

            logger.LogInformation($"Processing Pingback from: {_sourceUrl} ({request.IP}) to {_targetUrl}");

            var pingRequest = await sourceInspector.ExamineSourceAsync(_sourceUrl, _targetUrl);
            if (null == pingRequest) return PingbackResponse.InvalidPingRequest;

            if (!pingRequest.SourceHasTarget)
            {
                logger.LogError("Pingback error: The source URI does not contain a link to the target URI.");
                return PingbackResponse.Error17SourceNotContainTargetUri;
            }

            if (pingRequest.ContainsHtml)
            {
                logger.LogWarning("Spam detected on current Pingback...");
                return PingbackResponse.SpamDetectedFakeNotFound;
            }

            var routeLink = Helper.GetRouteLinkFromUrl(pingRequest.TargetUrl);
            var spec = new PostByRouteLinkForIdTitleSpec(routeLink);
            var (id, title) = await postRepo.FirstOrDefaultAsync(spec, ct);
            if (id == Guid.Empty)
            {
                logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                return PingbackResponse.Error32TargetUriNotExist;
            }

            logger.LogInformation($"Post '{id}:{title}' is found for ping.");

            var pinged = await mentionRepo.AnyAsync(new MentionSpec(id, pingRequest.SourceUrl, request.IP), ct);
            if (pinged) return PingbackResponse.Error48PingbackAlreadyRegistered;

            logger.LogInformation("Adding received pingback...");

            var uri = new Uri(_sourceUrl);
            var obj = new MentionEntity
            {
                Id = Guid.NewGuid(),
                PingTimeUtc = DateTime.UtcNow,
                Domain = uri.Host,
                SourceUrl = _sourceUrl,
                SourceTitle = pingRequest.Title,
                TargetPostId = id,
                TargetPostTitle = title,
                SourceIp = request.IP,
                Worker = "Pingback"
            };

            await mentionRepo.AddAsync(obj, ct);

            return new(PingbackStatus.Success)
            {
                MentionEntity = obj
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return PingbackResponse.GenericError;
        }
    }

    private bool ValidateRequest(string requestBody)
    {
        logger.LogInformation($"Pingback received xml: {requestBody}");

        if (!requestBody.Contains("<methodName>pingback.ping</methodName>"))
        {
            logger.LogWarning("Could not find pingback method, request has been terminated.");
            return false;
        }

        var doc = new XmlDocument();
        doc.LoadXml(requestBody);

        var list = doc.SelectNodes("methodCall/params/param/value/string") ??
                   doc.SelectNodes("methodCall/params/param/value");

        if (list is null)
        {
            logger.LogWarning("Could not find Pingback sourceUrl and targetUrl, request has been terminated.");
            return false;
        }

        _sourceUrl = list[0]?.InnerText.Trim();
        _targetUrl = list[1]?.InnerText.Trim();

        return true;
    }
}