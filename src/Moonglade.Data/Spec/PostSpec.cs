using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PostSpec : BaseSpecification<Post>
    {
        //public PostSpec(Expression<Func<Post, bool>> criteria) : base(criteria)
        //{

        //}

        public PostSpec(Guid? categoryId, int? top = null) : 
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
    }
}
