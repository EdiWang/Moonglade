using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moonglade.Mention.Common;
using Moonglade.Utils;

namespace Moonglade.Webmention;

public record ReceiveWebmentionCommand(string Source, string Target, string RemoteIp) : IRequest<WebmentionResponse>;

public class ReceiveWebmentionCommandHandler(
    ILogger<ReceiveWebmentionCommandHandler> logger,
    IMentionSourceInspector sourceInspector,
    MoongladeRepository<MentionEntity> mentionRepo,
    MoongladeRepository<PostEntity> postRepo
    ) : IRequestHandler<ReceiveWebmentionCommand, WebmentionResponse>
{
    private string _sourceUrl;
    private string _targetUrl;

    public async Task<WebmentionResponse> Handle(ReceiveWebmentionCommand request, CancellationToken ct)
    {
        try
        {
            // check request.Source and request.Target is valid URL
            if (!Uri.TryCreate(request.Source, UriKind.Absolute, out var sourceUri) ||
                !Uri.TryCreate(request.Target, UriKind.Absolute, out var targetUri))
            {
                logger.LogError("Invalid webmention request: source or target URL is invalid.");
                return WebmentionResponse.InvalidWebmentionRequest;
            }

            _sourceUrl = sourceUri.ToString();
            _targetUrl = targetUri.ToString();

            logger.LogInformation($"Processing Webmention from: {_sourceUrl} ({request.RemoteIp}) to {_targetUrl}");

            var mentionRequest = await sourceInspector.ExamineSourceAsync(_sourceUrl, _targetUrl);
            if (null == mentionRequest) return WebmentionResponse.InvalidWebmentionRequest;

            if (!mentionRequest.SourceHasTarget)
            {
                logger.LogError("Webmention error: The source URI does not contain a link to the target URI.");
                return WebmentionResponse.ErrorSourceNotContainTargetUri;
            }

            if (mentionRequest.ContainsHtml)
            {
                logger.LogWarning("Spam detected on current Webmention...");
                return WebmentionResponse.SpamDetectedFakeNotFound;
            }

            var (slug, pubDate) = Helper.GetSlugInfoFromUrl(mentionRequest.TargetUrl);
            var spec = new PostByDateAndSlugForIdTitleSpec(pubDate, slug);
            var (id, title) = await postRepo.FirstOrDefaultAsync(spec, ct);
            if (id == Guid.Empty)
            {
                logger.LogError($"Can not get post id and title for url '{_targetUrl}'");
                return WebmentionResponse.ErrorTargetUriNotExist;
            }

            logger.LogInformation($"Post '{id}:{title}' is found for ping.");

            var pinged = await mentionRepo.AnyAsync(new MentionSpec(id, mentionRequest.SourceUrl, request.RemoteIp), ct);
            if (pinged) return WebmentionResponse.ErrorWebmentionAlreadyRegistered;

            logger.LogInformation("Adding received Webmention...");

            var uri = new Uri(_sourceUrl);
            var obj = new MentionEntity
            {
                Id = Guid.NewGuid(),
                PingTimeUtc = DateTime.UtcNow,
                Domain = uri.Host,
                SourceUrl = _sourceUrl,
                SourceTitle = mentionRequest.Title,
                TargetPostId = id,
                TargetPostTitle = title,
                SourceIp = request.RemoteIp,
                Worker = "Webmention"
            };

            await mentionRepo.AddAsync(obj, ct);

            return new(WebmentionStatus.Success)
            {
                MentionEntity = obj
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return WebmentionResponse.GenericError;
        }
    }
}