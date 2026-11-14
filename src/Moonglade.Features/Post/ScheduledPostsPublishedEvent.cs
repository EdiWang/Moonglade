using LiteBus.Events.Abstractions;

namespace Moonglade.Features.Post;

public sealed record ScheduledPostPublication(Guid PostId, string Title, DateTime PublishedTimeUtc);

public sealed record ScheduledPostsPublishedEvent(IReadOnlyList<ScheduledPostPublication> Posts) : IEvent;
