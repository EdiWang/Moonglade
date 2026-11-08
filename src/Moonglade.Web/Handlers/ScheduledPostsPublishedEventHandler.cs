using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;

namespace Moonglade.Web.Handlers;

public class ScheduledPostsPublishedEventHandler(
    ICommandMediator commandMediator,
    ILogger<ScheduledPostsPublishedEventHandler> logger) : IEventHandler<ScheduledPostsPublishedEvent>
{
    public async Task HandleAsync(ScheduledPostsPublishedEvent request, CancellationToken ct)
    {
        foreach (var post in request.Posts)
        {
            try
            {
                await commandMediator.SendAsync(new CreateActivityLogCommand(
                    ActivityEvents.PostAutomaticallyPublished,
                    ActivityActorKind.System,
                    null,
                    post.PostId.ToString(),
                    post.Title,
                    ActivityLogDetails.Create(("PostId", post.PostId)),
                    EventTimeUtc: post.PublishedTimeUtc),
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Scheduled publication activity logging failed for {EventKey}. Failure type: {FailureType}",
                    ActivityEvents.PostAutomaticallyPublished, exception.GetType().Name);
            }
        }
    }
}
