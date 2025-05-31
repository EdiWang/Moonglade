﻿using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostPagingSpec : Specification<PostEntity>
{
    public PostPagingSpec(int pageSize, int pageIndex, Guid? categoryId = null)
    {
        Query.Where(p => !p.IsDeleted && p.PostStatus == PostStatusConstants.Published &&
                         (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)));

        var startRow = (pageIndex - 1) * pageSize;

        Query.OrderByDescending(p => p.PubDateUtc);
        Query.Skip(startRow).Take(pageSize);
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
                Query.Where(p => p.PostStatus == PostStatusConstants.Draft && !p.IsDeleted);
                break;
            case PostStatus.Published:
                Query.Where(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
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