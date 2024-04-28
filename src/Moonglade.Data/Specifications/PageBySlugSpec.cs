using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PageBySlugSpec : SingleResultSpecification<PageEntity>
{
    public PageBySlugSpec(string slug)
    {
        Query.Where(p => p.Slug == slug);
    }
}