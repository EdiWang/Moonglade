using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagDisplayNameNameSpec : Specification<TagEntity, string>
{
    public TagDisplayNameNameSpec()
    {
        Query.Select(t => t.DisplayName);
    }
}