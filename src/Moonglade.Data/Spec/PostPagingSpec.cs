using System;
using System.Linq;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PostPagingSpec : BaseSpecification<PostEntity>
    {
        public PostPagingSpec(int pageSize, int pageIndex, Guid? categoryId = null)
            : base(p => !p.PostPublish.IsDeleted &&
                        p.PostPublish.IsPublished &&
                        (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)))
        {
            var startRow = (pageIndex - 1) * pageSize;

            //AddInclude(post => post
            //    .Include(p => p.PostPublish)
            //    .Include(p => p.PostExtension)
            //    .Include(p => p.PostTag)
            //    .ThenInclude(pt => pt.Tag));
            ApplyPaging(startRow, pageSize);
            ApplyOrderByDescending(p => p.PostPublish.PubDateUtc);
        }
    }
}
