using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagCloudSpec : Specification<TagEntity, TagWithCount>
{
    public TagCloudSpec()
    {
        Query.Select(t => new TagWithCount
        {
            Tag = t,
            PostCount = t.Posts.Count
        });
    }
}

public class TagWithCount
{
    public TagEntity Tag { get; set; }
    public int PostCount { get; set; }
}