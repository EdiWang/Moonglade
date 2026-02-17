using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostByCatSpec : Specification<PostEntity>
{
    public PostByCatSpec(Guid? categoryId, int? top = null)
    {
        Query.Where(p =>
                    !p.IsDeleted &&
                    p.PostStatus == PostStatus.Published &&
                    p.IsFeedIncluded &&
                    (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)));

        Query.OrderByDescending(p => p.PubDateUtc);

        if (top.HasValue)
        {
            Query.Skip(0).Take(top.Value);
        }
    }
}
