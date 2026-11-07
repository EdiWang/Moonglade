using LiteBus.Events.Abstractions;

namespace Moonglade.Features.Post;

// Publish without request cancellation after commit so disconnected clients cannot skip invalidation.
public record PostChangedEvent : IEvent;
