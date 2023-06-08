using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public enum PostsSortBy
{
    Recent,
    Featured
}

public sealed class PostPagingSpec : BaseSpecification<PostEntity>
{
    public PostPagingSpec(int pageSize, int pageIndex, Guid? categoryId = null, PostsSortBy postsSortBy = PostsSortBy.Recent)
        : base(p => !p.IsDeleted && p.IsPublished &&
                    (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)))
    {
        var startRow = (pageIndex - 1) * pageSize;

        switch (postsSortBy)
        {
            case PostsSortBy.Recent:
                ApplyOrderByDescending(p => p.PubDateUtc);
                break;
            case PostsSortBy.Featured:
                ApplyOrderByDescending(p => p.IsFeatured);
                break;
        }
        ApplyPaging(startRow, pageSize);
    }

    public PostPagingSpec(PostStatus postStatus, string keyword, int pageSize, int offset)
        : base(p => null == keyword || p.Title.Contains(keyword))
    {
        switch (postStatus)
        {
            case PostStatus.Draft:
                AddCriteria(p => !p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Published:
                AddCriteria(p => p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Deleted:
                AddCriteria(p => p.IsDeleted);
                break;
            case PostStatus.Default:
                AddCriteria(p => true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(postStatus), postStatus, null);
        }

        ApplyPaging(offset, pageSize);
        ApplyOrderByDescending(p => p.PubDateUtc);
    }
}