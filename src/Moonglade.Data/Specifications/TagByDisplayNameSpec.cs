using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class TagByDisplayNameSpec : SingleResultSpecification<TagEntity>
{
    public TagByDisplayNameSpec(string displayName)
    {
        Query.Where(t => t.DisplayName == displayName);
    }
}