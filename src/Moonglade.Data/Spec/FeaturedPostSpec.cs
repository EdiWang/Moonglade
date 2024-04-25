using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class FeaturedPostSpec : Specification<PostEntity>
{
    public FeaturedPostSpec(int pageSize, int pageIndex)
    {
        Query.Where(p =>
            p.IsFeatured
            && !p.IsDeleted
            && p.IsPublished);

        var startRow = (pageIndex - 1) * pageSize;
        Query.Skip(startRow).Take(pageSize);
        Query.OrderByDescending(p => p.PubDateUtc);
    }
}