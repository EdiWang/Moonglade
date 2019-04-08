using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class GetPostSpec : BaseSpecification<Post>
    {
        public GetPostSpec(DateTime date, string slug)
            : base(p => p.Slug == slug &&
             p.PostPublish.IsPublished &&
             p.PostPublish.PubDateUtc.Value.Date == date &&
             !p.PostPublish.IsDeleted)
        {
            AddInclude(post => post
                .Include(p => p.PostPublish)
                .Include(p => p.PostExtension)
                .Include(p => p.Comment)
                .Include(p => p.PostTag).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategory).ThenInclude(pc => pc.Category));
        }

        public GetPostSpec(int pageSize, int pageIndex, Guid? categoryId = null)
            : base(p => !p.PostPublish.IsDeleted &&
                        p.PostPublish.IsPublished &&
                        (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)))
        {
            var startRow = (pageIndex - 1) * pageSize;

            AddInclude(post => post
                .Include(p => p.PostPublish)
                .Include(p => p.PostExtension)
                .Include(p => p.PostTag)
                .ThenInclude(pt => pt.Tag));
            ApplyPaging(startRow, pageSize);
            ApplyOrderByDescending(p => p.PostPublish.PubDateUtc);
        }

        public GetPostSpec(Guid id) : base(p => p.Id == id)
        {
            AddInclude(post => post
                .Include(p => p.PostPublish)
                .Include(p => p.PostTag)
                .ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category));
        }
    }
}
