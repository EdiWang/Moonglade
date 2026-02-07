using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagDisplayNameSpec : Specification<TagEntity, string>
{
    public TagDisplayNameSpec()
    {
        Query.Select(t => t.DisplayName);
    }
}