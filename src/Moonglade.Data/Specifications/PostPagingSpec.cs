using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostPagingSpec : Specification<PostEntity>
{
    public PostPagingSpec(int pageSize, int pageIndex, Guid? categoryId = null)
    {
        Query.Where(p => !p.IsDeleted && p.PostStatus == PostStatus.Published &&
                         (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)));

        var startRow = (pageIndex - 1) * pageSize;

        Query.OrderByDescending(p => p.PubDateUtc);
        Query.Skip(startRow).Take(pageSize);
    }
}
