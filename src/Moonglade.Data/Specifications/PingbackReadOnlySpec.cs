using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PingbackReadOnlySpec : Specification<MentionEntity>
{
    public PingbackReadOnlySpec()
    {
        Query.AsNoTracking();
    }
}