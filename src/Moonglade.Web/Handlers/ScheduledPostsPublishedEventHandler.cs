using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Services;

namespace Moonglade.Web.Handlers;

public class ScheduledPostsPublishedEventHandler(
    ICommandMediator commandMediator,
    ILogger<ScheduledPostsPublishedEventHandler> logger) : IEventHandler<ScheduledPostsPublishedEvent>
{
    public async Task HandleAsync(ScheduledPostsPublishedEvent request, CancellationToken ct)
    {
        foreach (var post in request.Posts)
        {
            await ActivityLogDispatcher.SendAsync(
                commandMediator,
                logger,
                ActivityEvents.PostAutomaticallyPublished,
                () => new CreateActivityLogCommand(
                    ActivityEvents.PostAutomaticallyPublished,
                    ActivityActorKind.System,
                    null,
                    post.PostId.ToString(),
                    post.Title,
                    ActivityLogDetails.Create(("PostId", post.PostId)),
                    EventTimeUtc: post.PublishedTimeUtc),
                ActivityLogDispatchSource.ScheduledPublication,
                ct: CancellationToken.None);
        }
    }
}
