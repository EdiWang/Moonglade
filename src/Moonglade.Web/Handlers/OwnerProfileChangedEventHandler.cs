using LiteBus.Events.Abstractions;

namespace Moonglade.Web.Handlers;

public sealed class OwnerProfileChangedEventHandler(PostCacheInvalidator cache) : IEventHandler<OwnerProfileChangedEvent>
{
    public Task HandleAsync(OwnerProfileChangedEvent notification, CancellationToken ct)
    {
        cache.InvalidateFeeds();
        return Task.CompletedTask;
    }
}
