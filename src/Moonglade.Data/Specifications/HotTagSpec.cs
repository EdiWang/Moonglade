using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class HotTagSpec : Specification<TagEntity, (TagEntity Tag, int PostCount)>
{
    public HotTagSpec(int top)
    {
        Query.Skip(0).Take(top);
        Query.OrderByDescending(p => p.Posts.Count);

        Query.Select(t => new ValueTuple<TagEntity, int>(t, t.Posts.Count));
    }
}

public sealed class TagByNormalizedNameSpec : SingleResultSpecification<TagEntity>
{
    public TagByNormalizedNameSpec(string normalizedName)
    {
        Query.Where(t => t.NormalizedName == normalizedName);
    }
}

public sealed class TagByDisplayNameSpec : SingleResultSpecification<TagEntity>
{
    public TagByDisplayNameSpec(string displayName)
    {
        Query.Where(t => t.DisplayName == displayName);
    }
}

public sealed class TagDisplayNameNameSpec : Specification<TagEntity, string>
{
    public TagDisplayNameNameSpec()
    {
        Query.Select(t => t.DisplayName);
    }
}