using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class TagSpec : BaseSpecification<TagEntity>
{
    public TagSpec(int top) : base(t => true)
    {
        ApplyPaging(0, top);
        ApplyOrderByDescending(p => p.Posts.Count);
    }
}

public sealed class TagByNormalizedNameSpec : Specification<TagEntity>
{
    public TagByNormalizedNameSpec(string normalizedName)
    {
        Query.Where(t => t.NormalizedName == normalizedName);
    }
}