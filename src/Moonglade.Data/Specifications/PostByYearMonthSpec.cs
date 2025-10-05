using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostByYearMonthSpec : Specification<PostEntity>
{
    public PostByYearMonthSpec(int year, int month = 0)
    {
        Query.Where(p => p.PubDateUtc.Value.Year == year &&
                         (month == 0 || p.PubDateUtc.Value.Month == month));

        // Fix #313: Filter out unpublished posts
        Query.Where(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);

        Query.OrderByDescending(p => p.PubDateUtc);
        Query.AsNoTracking();
    }
}
