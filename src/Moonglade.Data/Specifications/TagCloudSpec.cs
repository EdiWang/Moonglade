using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagCloudSpec : Specification<TagEntity, (TagEntity Tag, int PostCount)>
{
    public TagCloudSpec()
    {
        Query.Select(t => new ValueTuple<TagEntity, int>(t, t.Posts.Count));
    }
}