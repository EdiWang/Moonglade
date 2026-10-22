using LiteBus.Events.Abstractions;
using Moonglade.Features.Post;

namespace Moonglade.Web.Handlers;

public class PostChangedEventHandler(PostCacheInvalidator invalidator) : IEventHandler<PostChangedEvent>
{
    public Task HandleAsync(PostChangedEvent request, CancellationToken ct)
    {
        invalidator.Invalidate();

        return Task.CompletedTask;
    }
}
