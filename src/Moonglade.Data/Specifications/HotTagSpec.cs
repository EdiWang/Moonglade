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