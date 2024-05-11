using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class CategoryBySlugSpec : SingleResultSpecification<CategoryEntity>
{
    public CategoryBySlugSpec(string slug)
    {
        Query.Where(p => p.Slug == slug);
    }
}