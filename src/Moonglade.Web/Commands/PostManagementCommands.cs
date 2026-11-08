#nullable enable annotations

using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.IndexNow.Client;
using Moonglade.Web.Services;
using Moonglade.Webmention;
using System.Text.Json.Serialization;

namespace Moonglade.Web.Commands;

public record PostOperationContext(
    string ActorId,
    string IpAddress,
    string UserAgent,
    string RootUrl)
{
    [JsonIgnore]
    public string? TraceIdentifier { get; init; }
}

public record PostOperationResult(
    bool Succeeded,
    Guid PostId,
    DateTime? LastModifiedUtc,
    string ErrorMessage)
{
    public static PostOperationResult Success(Guid postId, DateTime? lastModifiedUtc) =>
        new(true, postId, lastModifiedUtc, string.Empty);

    public static PostOperationResult Conflict(string errorMessage) =>
        new(false, Guid.Empty, null, errorMessage);
}

public record SavePostCommand(
    PostEditModel Payload,
    DateTimeOffset? PreviousLastModifiedUtc,
    PostOperationContext Context) : ICommand<PostOperationResult>;

public class SavePostCommandHandler(
    IConfiguration configuration,
    ICommandMediator commandMediator,
    IBlogConfig blogConfig,
    ScheduledPublishWakeUp wakeUp,
    ILogger<SavePostCommandHandler> logger,
    CannonService cannonService,
    TimeProvider timeProvider) : ICommandHandler<SavePostCommand, PostOperationResult>
{
    public async Task<PostOperationResult> HandleAsync(SavePostCommand request, CancellationToken ct)
    {
        var settings = blogConfig.Capture();
        try
        {
            var model = request.Payload;
            var isNewPost = model.PostId == Guid.Empty;
            var postTitle = model.Title;

            var wakeScheduledPublisher = PrepareScheduledPost(model);

            var postEntity = isNewPost ?
                await commandMediator.SendAsync(new CreatePostCommand(model), ct) :
                await commandMediator.SendAsync(new UpdatePostCommand(model.PostId, model), ct);

            if (wakeScheduledPublisher)
            {
                wakeUp.WakeUp();
                logger.LogInformation("Scheduled publish wake-up triggered for post: {PostId}", postEntity.Id);
            }

            var activityEventKey = isNewPost ? ActivityEvents.PostCreated : ActivityEvents.PostUpdated;
            var activityDetails = ActivityLogDetails.Create(
                ("PostId", postEntity.Id),
                ("Slug", postEntity.RouteLink),
                ("PostStatus", model.PostStatus));
            await ActivityLogDispatcher.SendAsync(
                commandMediator,
                logger,
                activityEventKey,
                () => new CreateActivityLogCommand(
                    activityEventKey,
                    ActivityActorKind.Administrator,
                    request.Context.ActorId,
                    TargetId: postEntity.Id.ToString(),
                    TargetDisplayName: postTitle,
                    Details: activityDetails,
                    IpAddress: request.Context.IpAddress,
                    UserAgent: request.Context.UserAgent),
                ActivityLogDispatchSource.PostSave,
                traceIdentifier: request.Context.TraceIdentifier,
                ct: ct);

            if (model.PostStatus == PostStatus.Published)
            {
                ProcessPublishedPost(request.PreviousLastModifiedUtc, postEntity, request.Context, settings.AdvancedSettings);
            }

            return PostOperationResult.Success(postEntity.Id, postEntity.LastModifiedUtc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating post.");
            return PostOperationResult.Conflict("Error updating post.");
        }
    }

    private bool PrepareScheduledPost(PostEditModel model)
    {
        if (model.PostStatus != PostStatus.Scheduled)
        {
            model.ScheduledPublishTimeUtc = null;
            return false;
        }

        if (!model.ScheduledPublishTimeUtc.HasValue)
        {
            throw new InvalidOperationException("A scheduled post must have a UTC publish time.");
        }

        if (model.ScheduledPublishTimeUtc < timeProvider.GetUtcNow().UtcDateTime)
        {
            model.PostStatus = PostStatus.Published;
            model.ScheduledPublishTimeUtc = null;
            return false;
        }

        logger.LogInformation("Post scheduled for publish at {ScheduledPublishTimeUtc} UTC.", model.ScheduledPublishTimeUtc);
        return true;
    }

    private void ProcessPublishedPost(DateTimeOffset? lastModifiedUtc, PostCommandResult postEntity, PostOperationContext context, AdvancedSettings settings)
    {
        logger.LogInformation("Trying to Ping URL for post: {PostId}", postEntity.Id);

        var link = GetPostUri(context, postEntity.RouteLink);

        NotifyExternalServices(postEntity.PostContent, link, settings);
        ProcessIndexing(lastModifiedUtc, postEntity.LastModifiedUtc == postEntity.PubDateUtc, link);
    }

    private void NotifyExternalServices(string postContent, Uri link, AdvancedSettings advancedSettings)
    {
        if (advancedSettings.EnableWebmention)
        {
            cannonService.FireAsync<IWebmentionSender>(async sender => await sender.SendWebmentionAsync(link.ToString(), postContent));
        }
    }

    private void ProcessIndexing(DateTimeOffset? lastModifiedUtc, bool isNewPublish, Uri link)
    {
        bool indexCoolDown = true;
        var minimalIntervalMinutes = int.Parse(configuration["IndexNow:MinimalIntervalMinutes"]!);
        if (lastModifiedUtc.HasValue)
        {
            var lastSavedInterval = timeProvider.GetUtcNow() - lastModifiedUtc.Value;
            indexCoolDown = lastSavedInterval.TotalMinutes > minimalIntervalMinutes;
        }

        if (isNewPublish || indexCoolDown)
        {
            cannonService.FireAsync<IIndexNowClient>(async sender => await sender.SendRequestAsync(link));
        }
    }

    private static Uri GetPostUri(PostOperationContext context, string routeLink)
    {
        var rootUrl = context.RootUrl.EndsWith('/') ? context.RootUrl : $"{context.RootUrl}/";
        return new Uri(new Uri(rootUrl), $"post/{routeLink.ToLower()}");
    }

}
