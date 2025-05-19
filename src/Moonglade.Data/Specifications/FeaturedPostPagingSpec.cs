using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class FeaturedPostPagingSpec : Specification<PostEntity>
{
    public FeaturedPostPagingSpec(int pageSize, int pageIndex)
    {
        Query.Where(p =>
            p.IsFeatured
            && !p.IsDeleted
            && p.PostStatus == PostStatusConstants.Published);

        var startRow = (pageIndex - 1) * pageSize;
        Query.Skip(startRow).Take(pageSize);
        Query.OrderByDescending(p => p.PubDateUtc);
    }
}