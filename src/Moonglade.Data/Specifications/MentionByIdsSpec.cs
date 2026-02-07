using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class MentionByIdsSpec : Specification<MentionEntity>
{
    public MentionByIdsSpec(List<Guid> ids)
    {
        Query.Where(m => ids.Contains(m.Id));
    }
}