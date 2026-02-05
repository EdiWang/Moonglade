using Moonglade.Data.Specifications;

namespace Moonglade.Data.DTO;

public record PostSegment
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string Slug { get; init; }
    public string ContentAbstract { get; init; }
    public DateTime? PubDateUtc { get; init; }
    public DateTime CreateTimeUtc { get; init; }
    public DateTime? LastModifiedUtc { get; init; }
    public DateTime? ScheduledPublishTimeUtc { get; init; }
    public PostStatus PostStatus { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsOutdated { get; init; }
}