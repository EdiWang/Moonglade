using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagCloudSpec : Specification<TagEntity, TagWithCount>
{
    public TagCloudSpec()
    {
        Query.Select(t => new TagWithCount
        {
            DisplayName = t.DisplayName,
            NormalizedName = t.NormalizedName,
            PostCount = t.Posts.Count
        });
    }
}
