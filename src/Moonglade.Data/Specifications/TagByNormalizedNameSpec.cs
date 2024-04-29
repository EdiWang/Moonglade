using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagByNormalizedNameSpec : SingleResultSpecification<TagEntity>
{
    public TagByNormalizedNameSpec(string normalizedName)
    {
        Query.Where(t => t.NormalizedName == normalizedName);
    }
}