using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Utils;

namespace Moonglade.Webmention;

public record ReceiveWebmentionCommand(string Source, string Target, string RemoteIp) : ICommand<WebmentionResponse>;

public class ReceiveWebmentionCommandHandler(
    ILogger<ReceiveWebmentionCommandHandler> logger,
    IMentionSourceInspector sourceInspector,
    IWebmentionSourceRateLimiter sourceRateLimiter,
    IWebmentionUrlSafetyValidator urlSafetyValidator,
    BlogDbContext db
    ) : ICommandHandler<ReceiveWebmentionCommand, WebmentionResponse>
{
    public async Task<WebmentionResponse> HandleAsync(ReceiveWebmentionCommand request, CancellationToken ct)
    {
        try
        {
            var (isValid, sourceUri, sourceUrl, targetUrl) = ValidateUrls(request.Source, request.Target);
            if (!isValid)
            {
                return WebmentionResponse.InvalidWebmentionRequest;
            }

            if (!await urlSafetyValidator.IsSafeSourceAsync(sourceUri!, ct))
            {
                logger.LogWarning("Blocked webmention from unsafe source URI: {SourceUri}", sourceUri);
                return WebmentionResponse.InvalidWebmentionRequest;
            }

            logger.LogInformation("Processing Webmention from: {SourceUrl} ({RemoteIp}) to {TargetUrl}", sourceUrl, request.RemoteIp, targetUrl);

            if (!sourceRateLimiter.TryAcquire(sourceUri!))
            {
                return WebmentionResponse.SourceRateLimitExceeded;
            }

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
            logger.LogError(e, "Error processing Webmention request.");
            return WebmentionResponse.GenericError;
        }
    }

    private (bool IsValid, Uri? SourceUri, string SourceUrl, string TargetUrl) ValidateUrls(string source, string target)
    {
        if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri) ||
            !Uri.TryCreate(target, UriKind.Absolute, out var targetUri))
        {
            logger.LogWarning("Invalid webmention request: source or target URL is invalid.");
            return (false, null, string.Empty, string.Empty);
        }

        if (sourceUri.Scheme is not "http" and not "https")
        {
            logger.LogWarning("Blocked webmention from disallowed source URI scheme: {SourceUri}", sourceUri);
            return (false, null, string.Empty, string.Empty);
        }

        if (targetUri.Scheme is not "http" and not "https")
        {
            logger.LogWarning("Invalid webmention request: target URL has a disallowed scheme: {TargetUri}", targetUri);
            return (false, null, string.Empty, string.Empty);
        }

        return (true, sourceUri, sourceUri.ToString(), targetUri.ToString());
    }

    private WebmentionResponse? ValidateMentionRequest(MentionRequest mentionRequest)
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
        var result = await db.Post
            .AsNoTracking()
            .Where(p => p.RouteLink == routeLink && p.PostStatus == PostStatus.Published && !p.IsDeleted)
            .Select(p => new WebmentionTargetPost(p.Id, p.Title))
            .FirstOrDefaultAsync(ct);

        if (result is null)
        {
            logger.LogError("Can not get post id and title for url '{TargetUrl}'", targetUrl);
            return (Guid.Empty, string.Empty);
        }

        logger.LogInformation("Post '{PostId}:{PostTitle}' is found for ping.", result.Id, result.Title);
        return (result.Id, result.Title);
    }

    private async Task<bool> IsDuplicateMentionAsync(Guid postId, string sourceUrl, string remoteIp, CancellationToken ct)
    {
        return await db.Mention.AnyAsync(
            p => p.TargetPostId == postId && p.SourceUrl == sourceUrl && p.SourceIp == remoteIp, ct);
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

        await db.Mention.AddAsync(mention, ct);
        await db.SaveChangesAsync(ct);
        return mention;
    }
}

file sealed record WebmentionTargetPost(Guid Id, string Title);
