using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class GetPostSpec : BaseSpecification<Post>
    {
        public GetPostSpec(Guid? categoryId, int? top = null) :
            base(p => !p.PostPublish.IsDeleted &&
                      p.PostPublish.IsPublished &&
                      p.PostPublish.IsFeedIncluded &&
                      (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)))
        {
            // AddInclude(p => p.PostPublish);
            ApplyOrderByDescending(p => p.PostPublish.PubDateUtc);

            if (top.HasValue)
            {
                ApplyPaging(0, top.Value);
            }
        }

        public GetPostSpec(int year, int month = 0) :
            base(p => p.PostPublish.PubDateUtc.Value.Year == year &&
                      (month == 0 || p.PostPublish.PubDateUtc.Value.Month == month))
        {
            AddInclude(post => post.Include(p => p.PostPublish));
            ApplyOrderByDescending(p => p.PostPublish.PubDateUtc);
        }

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

        public GetPostSpec(Guid id, bool includeRelatedData = true) : base(p => p.Id == id)
        {
            if (includeRelatedData)
            {
                AddInclude(post => post
                    .Include(p => p.PostPublish)
                    .Include(p => p.PostTag)
                    .ThenInclude(pt => pt.Tag)
                    .Include(p => p.PostCategory)
                    .ThenInclude(pc => pc.Category));
            }
        }

        public GetPostSpec(bool isDeleted, bool isPublished) :
            base(p => p.PostPublish.IsDeleted == isDeleted && p.PostPublish.IsPublished == isPublished)
        {

        }

        public GetPostSpec(bool isDeleted) :
            base(p => p.PostPublish.IsDeleted == isDeleted)
        {

        }

        public GetPostSpec() :
            base(p => p.PostPublish.IsDeleted)
        {

        }
    }
}
