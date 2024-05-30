using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class MentionReadOnlySpec : Specification<MentionEntity>
{
    public MentionReadOnlySpec()
    {
        Query.AsNoTracking();
    }
}