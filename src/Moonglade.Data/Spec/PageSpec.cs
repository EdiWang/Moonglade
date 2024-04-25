using Moonglade.Data.Entities;

namespace Moonglade.Data.Spec;

public sealed class PageSpec : Specification<PageEntity>
{
    public PageSpec(int top)
    {
        Query.Where(p => p.IsPublished);

        Query.OrderByDescending(p => p.CreateTimeUtc);
        Query.Skip(0).Take(top);
    }
}