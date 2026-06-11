using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Data.DTO;
using Moonglade.Features.Post;
using Moonglade.IndexNow.Client;
using Moonglade.Webmention;

namespace Moonglade.Web.Commands;

public record PostOperationContext(
    string ActorId,
    string IpAddress,
    string UserAgent,
    string RootUrl);

public record PostOperationResult(bool Succeeded, Guid PostId, DateTime? LastModifiedUtc, string ErrorMessage)
{
    public static PostOperationResult Success(Guid postId, DateTime? lastModifiedUtc) => new(true, postId, lastModifiedUtc, string.Empty);

    public static PostOperationResult Conflict(string errorMessage) => new(false, Guid.Empty, null, errorMessage);
}

public record SavePostCommand(PostEditModel Payload, PostOperationContext Context) : ICommand<PostOperationResult>;

public class SavePostCommandHandler(
    ICacheAside cache,
    IConfiguration configuration,
    ICommandMediator commandMediator,
    IBlogConfig blogConfig,
    ScheduledPublishWakeUp wakeUp,
    ILogger<SavePostCommandHandler> logger,
    CannonService cannonService) : ICommandHandler<SavePostCommand, PostOperationResult>
{
    public async Task<PostOperationResult> HandleAsync(SavePostCommand request, CancellationToken ct)
    {
        try
        {
            var model = request.Payload;
            var isNewPost = model.PostId == Guid.Empty;

            if (!PrepareScheduledPost(model))
            {
                return PostOperationResult.Conflict("Client time zone ID is required for scheduled posts.");
            }

            var postEntity = isNewPost ?
                await commandMediator.SendAsync(new CreatePostCommand(model), ct) :
                await commandMediator.SendAsync(new UpdatePostCommand(model.PostId, model), ct);

            if (!string.IsNullOrWhiteSpace(postEntity.RouteLink))
            {
                cache.Remove(BlogCachePartition.Post.ToString(), postEntity.RouteLink);
            }

            await PostActivityLogger.LogAsync(
                commandMediator,
                request.Context,
                isNewPost ? EventType.PostCreated : EventType.PostUpdated,
                isNewPost ? "Create Post" : "Update Post",
                model.Title,
                new { PostId = postEntity.Id, Slug = postEntity.RouteLink, PostStatus = model.PostStatus },
                ct);

            if (model.PostStatus == PostStatus.Published)
            {
                ProcessPublishedPost(model.LastModifiedUtc, postEntity, request.Context);
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
        if (model.PostStatus != PostStatus.Scheduled || !model.ScheduledPublishTime.HasValue)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(model.ClientTimeZoneId))
        {
            return false;
        }

        var clientTimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.ClientTimeZoneId);
        var clientLocalTime = model.ScheduledPublishTime.Value;
        var clientUtcTime = TimeZoneInfo.ConvertTimeToUtc(clientLocalTime, clientTimeZone);

        model.ScheduledPublishTime = clientUtcTime;
        if (model.ScheduledPublishTime < DateTime.UtcNow)
        {
            model.PostStatus = PostStatus.Published;
            model.ScheduledPublishTime = null;
            return true;
        }

        logger.LogInformation("Post scheduled for publish at {ClientUtcTime} UTC.", clientUtcTime);

        wakeUp.WakeUp();

        logger.LogInformation("Scheduled publish wake-up triggered for post: {PostId}", model.PostId);
        return true;
    }

    private void ProcessPublishedPost(string lastModifiedUtc, PostCommandResult postEntity, PostOperationContext context)
    {
        logger.LogInformation("Trying to Ping URL for post: {PostId}", postEntity.Id);

        var link = GetPostUri(context, postEntity.RouteLink);

        NotifyExternalServices(postEntity.PostContent, link);
        ProcessIndexing(lastModifiedUtc, postEntity.LastModifiedUtc == postEntity.PubDateUtc, link);
    }

    private void NotifyExternalServices(string postContent, Uri link)
    {
        if (blogConfig.AdvancedSettings.EnableWebmention)
        {
            cannonService.FireAsync<IWebmentionSender>(async sender => await sender.SendWebmentionAsync(link.ToString(), postContent));
        }
    }

    private void ProcessIndexing(string lastModifiedUtc, bool isNewPublish, Uri link)
    {
        bool indexCoolDown = true;
        var minimalIntervalMinutes = int.Parse(configuration["IndexNow:MinimalIntervalMinutes"]!);
        if (!string.IsNullOrWhiteSpace(lastModifiedUtc))
        {
            var lastSavedInterval = DateTime.UtcNow - DateTime.Parse(lastModifiedUtc);
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

public record RecyclePostCommand(Guid PostId, PostOperationContext Context) : ICommand;

public class RecyclePostCommandHandler(
    ICommandMediator commandMediator) : ICommandHandler<RecyclePostCommand>
{
    public async Task HandleAsync(RecyclePostCommand request, CancellationToken ct)
    {
        await commandMediator.SendAsync(new DeletePostCommand(request.PostId, true), ct);

        await PostActivityLogger.LogAsync(
            commandMediator,
            request.Context,
            EventType.PostDeleted,
            "Delete Post (Move to Recycle Bin)",
            $"Post #{request.PostId}",
            new { request.PostId },
            ct);
    }

}

public record PublishPostWorkflowCommand(Guid PostId, PostOperationContext Context) : ICommand;

public class PublishPostWorkflowCommandHandler(
    ICacheAside cache,
    ICommandMediator commandMediator) : ICommandHandler<PublishPostWorkflowCommand>
{
    public async Task HandleAsync(PublishPostWorkflowCommand request, CancellationToken ct)
    {
        await commandMediator.SendAsync(new PublishPostCommand(request.PostId), ct);
        cache.Remove(BlogCachePartition.Post.ToString(), request.PostId.ToString());

        await PostActivityLogger.LogAsync(
            commandMediator,
            request.Context,
            EventType.PostPublished,
            "Publish Post",
            $"Post #{request.PostId}",
            new { request.PostId },
            ct);
    }

}

public record UnpublishPostWorkflowCommand(Guid PostId, PostOperationContext Context) : ICommand;

public class UnpublishPostWorkflowCommandHandler(
    ICacheAside cache,
    ICommandMediator commandMediator) : ICommandHandler<UnpublishPostWorkflowCommand>
{
    public async Task HandleAsync(UnpublishPostWorkflowCommand request, CancellationToken ct)
    {
        await commandMediator.SendAsync(new UnpublishPostCommand(request.PostId), ct);
        cache.Remove(BlogCachePartition.Post.ToString(), request.PostId.ToString());

        await PostActivityLogger.LogAsync(
            commandMediator,
            request.Context,
            EventType.PostUnpublished,
            "Unpublish Post",
            $"Post #{request.PostId}",
            new { request.PostId },
            ct);
    }

}

file static class PostActivityLogger
{
    public static async Task LogAsync(
        ICommandMediator commandMediator,
        PostOperationContext context,
        EventType eventType,
        string operation,
        string targetName,
        object metaData,
        CancellationToken ct)
    {
        await commandMediator.SendAsync(new CreateActivityLogCommand(
            eventType,
            context.ActorId,
            operation,
            targetName,
            metaData,
            context.IpAddress,
            context.UserAgent), ct);
    }
}
