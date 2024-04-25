using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class PostPagingSpec : BaseSpecification<PostEntity>
{
    public PostPagingSpec(int pageSize, int pageIndex, Guid? categoryId = null)
        : base(p => !p.IsDeleted && p.IsPublished &&
                    (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)))
    {
        var startRow = (pageIndex - 1) * pageSize;

        ApplyOrderByDescending(p => p.PubDateUtc);
        ApplyPaging(startRow, pageSize);
    }
}

public sealed class PostPagingByStatusSpec : Specification<PostEntity>
{
    public PostPagingByStatusSpec(PostStatus postStatus, string keyword, int pageSize, int offset)
    {
        Query.Where(p => null == keyword || p.Title.Contains(keyword));

        switch (postStatus)
        {
            case PostStatus.Draft:
                Query.Where(p => !p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Published:
                Query.Where(p => p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Deleted:
                Query.Where(p => p.IsDeleted);
                break;
            case PostStatus.Default:
                Query.Where(p => true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(postStatus), postStatus, null);
        }

        Query.Skip(offset).Take(pageSize);
        Query.OrderByDescending(p => p.PubDateUtc);
    }
}