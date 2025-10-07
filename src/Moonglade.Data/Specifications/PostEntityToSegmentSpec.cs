using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PostEntityToSegmentSpec : Specification<PostEntity, PostSegment>
{
    public PostEntityToSegmentSpec()
    {
        Query.Select(p => new()
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            PubDateUtc = p.PubDateUtc,
            PostStatus = p.PostStatus,
            IsFeatured = p.IsFeatured,
            IsDeleted = p.IsDeleted,
            IsOutdated = p.IsOutdated,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ScheduledPublishTimeUtc = p.ScheduledPublishTimeUtc,
            ContentAbstract = p.ContentAbstract
        });
    }
}