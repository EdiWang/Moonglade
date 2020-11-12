using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PostSpec : BaseSpecification<PostEntity>
    {
        public PostSpec(Guid? categoryId, int? top = null) :
            base(p => !p.IsDeleted &&
                      p.IsPublished &&
                      p.IsFeedIncluded &&
                      (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)))
        {
            ApplyOrderByDescending(p => p.PubDateUtc);

            if (top.HasValue)
            {
                ApplyPaging(0, top.Value);
            }
        }

        public PostSpec(int year, int month = 0) :
            base(p => p.PubDateUtc.Value.Year == year &&
                      (month == 0 || p.PubDateUtc.Value.Month == month))
        {
            // Fix #313: Filter out unpublished posts
            AddCriteria(p => p.IsPublished && !p.IsDeleted);

            ApplyOrderByDescending(p => p.PubDateUtc);
        }

        public PostSpec(DateTime date, string slug)
            : base(p => p.Slug == slug &&
             p.IsPublished &&
             p.PubDateUtc.Value.Date == date &&
             !p.IsDeleted)
        {
            AddInclude(post => post
                .Include(p => p.PostExtension)
                .Include(p => p.Comment)
                .Include(p => p.PostTag).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategory).ThenInclude(pc => pc.Category));
        }

        public PostSpec(Guid id, bool includeRelatedData = true) : base(p => p.Id == id)
        {
            if (includeRelatedData)
            {
                AddInclude(post => post
                    .Include(p => p.PostTag)
                    .ThenInclude(pt => pt.Tag)
                    .Include(p => p.PostCategory)
                    .ThenInclude(pc => pc.Category));
            }
        }

        public PostSpec(PostPublishStatus status)
        {
            switch (status)
            {
                case PostPublishStatus.Draft:
                    AddCriteria(p => !p.IsPublished && !p.IsDeleted);
                    break;
                case PostPublishStatus.Published:
                    AddCriteria(p => p.IsPublished && !p.IsDeleted);
                    break;
                case PostPublishStatus.Deleted:
                    AddCriteria(p => p.IsDeleted);
                    break;
                case PostPublishStatus.NotSet:
                    AddCriteria(p => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        public PostSpec(bool isDeleted) :
            base(p => p.IsDeleted == isDeleted)
        {

        }

        public PostSpec() :
            base(p => p.IsDeleted)
        {

        }
    }
}
