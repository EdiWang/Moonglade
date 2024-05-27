using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PingbackSpec : Specification<MentionEntity>
{
    public PingbackSpec(Guid postId, string sourceUrl, string sourceIp)
    {
        Query.Where(p =>
            p.TargetPostId == postId &&
            p.SourceUrl == sourceUrl &&
            p.SourceIp == sourceIp);
    }
}