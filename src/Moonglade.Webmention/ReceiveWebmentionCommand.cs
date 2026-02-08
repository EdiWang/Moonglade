using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moonglade.Utils;
using System.Net;

namespace Moonglade.Webmention;

public record ReceiveWebmentionCommand(string Source, string Target, string RemoteIp) : ICommand<WebmentionResponse>;

public class ReceiveWebmentionCommandHandler(
    ILogger<ReceiveWebmentionCommandHandler> logger,
    IMentionSourceInspector sourceInspector,
    IRepositoryBase<MentionEntity> mentionRepo,
    IRepositoryBase<PostEntity> postRepo
    ) : ICommandHandler<ReceiveWebmentionCommand, WebmentionResponse>
{
    public async Task<WebmentionResponse> HandleAsync(ReceiveWebmentionCommand request, CancellationToken ct)
    {
        try
        {
            var (isValid, sourceUrl, targetUrl) = ValidateUrls(request.Source, request.Target);
            if (!isValid)
            {
                return WebmentionResponse.InvalidWebmentionRequest;
            }

            logger.LogInformation("Processing Webmention from: {SourceUrl} ({RemoteIp}) to {TargetUrl}", sourceUrl, request.RemoteIp, targetUrl);

            var mentionRequest = await sourceInspector.ExamineSourceAsync(sourceUrl, targetUrl);
            if (mentionRequest is null)
            {
                return WebmentionResponse.InvalidWebmentionRequest;
            }

            var validationResponse = ValidateMentionRequest(mentionRequest);
            if (validationResponse is not null)
            {
                return validationResponse;
            }

            var (postId, postTitle) = await FindTargetPostAsync(mentionRequest.TargetUrl, targetUrl, ct);
            if (postId == Guid.Empty)
            {
                return WebmentionResponse.ErrorTargetUriNotExist;
            }

            if (await IsDuplicateMentionAsync(postId, mentionRequest.SourceUrl, request.RemoteIp, ct))
            {
                return WebmentionResponse.ErrorWebmentionAlreadyRegistered;
            }

            var mention = await CreateMentionAsync(sourceUrl, mentionRequest.Title, postId, postTitle, request.RemoteIp, ct);

            return new(WebmentionStatus.Success)
            {
                MentionEntity = mention
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return WebmentionResponse.GenericError;
        }
    }

    private (bool IsValid, string SourceUrl, string TargetUrl) ValidateUrls(string source, string target)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri) ||
            !Uri.TryCreate(target, UriKind.Absolute, out var targetUri))
        {
            logger.LogError("Invalid webmention request: source or target URL is invalid.");
            return (false, string.Empty, string.Empty);
        }

        if (!IsAllowedUri(sourceUri))
        {
            logger.LogError("Blocked webmention from disallowed source URI: {SourceUri}", sourceUri);
            return (false, string.Empty, string.Empty);
        }

        return (true, sourceUri.ToString(), targetUri.ToString());
    }

    private static bool IsAllowedUri(Uri uri)
    {
        if (uri.Scheme != "http" && uri.Scheme != "https") return false;
        if (uri.IsLoopback) return false;

        if (IPAddress.TryParse(uri.Host, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length >= 2)
            {
                // Block 10.0.0.0/8
                if (bytes[0] == 10) return false;
                // Block 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                // Block 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return false;
            }
        }

        return true;
    }

    private WebmentionResponse ValidateMentionRequest(MentionRequest mentionRequest)
    {
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

        return null;
    }

    private async Task<(Guid PostId, string PostTitle)> FindTargetPostAsync(string mentionTargetUrl, string targetUrl, CancellationToken ct)
    {
        var routeLink = UrlHelper.GetRouteLinkFromUrl(mentionTargetUrl);
        var spec = new PostByRouteLinkForIdTitleSpec(routeLink);
        var (id, title) = await postRepo.FirstOrDefaultAsync(spec, ct);

        if (id == Guid.Empty)
        {
            logger.LogError("Can not get post id and title for url '{TargetUrl}'", targetUrl);
            return (Guid.Empty, string.Empty);
        }

        logger.LogInformation("Post '{PostId}:{PostTitle}' is found for ping.", id, title);
        return (id, title);
    }

    private async Task<bool> IsDuplicateMentionAsync(Guid postId, string sourceUrl, string remoteIp, CancellationToken ct)
    {
        return await mentionRepo.AnyAsync(new MentionSpec(postId, sourceUrl, remoteIp), ct);
    }

    private async Task<MentionEntity> CreateMentionAsync(string sourceUrl, string sourceTitle, Guid postId, string postTitle, string remoteIp, CancellationToken ct)
    {
        logger.LogInformation("Adding received Webmention...");

        var uri = new Uri(sourceUrl);
        var mention = new MentionEntity
        {
            Id = Guid.NewGuid(),
            PingTimeUtc = DateTime.UtcNow,
            Domain = uri.Host,
            SourceUrl = sourceUrl,
            SourceTitle = sourceTitle,
            TargetPostId = postId,
            TargetPostTitle = postTitle,
            SourceIp = remoteIp
        };

        await mentionRepo.AddAsync(mention, ct);
        return mention;
    }
}