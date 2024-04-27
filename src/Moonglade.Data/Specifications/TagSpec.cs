using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagSpec : Specification<TagEntity>
{
    public TagSpec(int top)
    {
        Query.Skip(0).Take(top);
        Query.OrderByDescending(p => p.Posts.Count);
    }
}

public sealed class TagByNormalizedNameSpec : Specification<TagEntity>
{
    public TagByNormalizedNameSpec(string normalizedName)
    {
        Query.Where(t => t.NormalizedName == normalizedName);
    }
}