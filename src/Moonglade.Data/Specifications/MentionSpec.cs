using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class MentionSpec : Specification<MentionEntity>
{
    public MentionSpec(Guid postId, string sourceUrl, string sourceIp)
    {
        Query.Where(p =>
            p.TargetPostId == postId &&
            p.SourceUrl == sourceUrl &&
            p.SourceIp == sourceIp);
    }
}