using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class ScheduledPostSpec : Specification<PostEntity>
{
    public ScheduledPostSpec(DateTime utcNow)
    {
        Query.Where(p => p.PostStatus == PostStatusConstants.Scheduled && !p.IsDeleted && p.ScheduledPublishTimeUtc <= utcNow);
    }
}
