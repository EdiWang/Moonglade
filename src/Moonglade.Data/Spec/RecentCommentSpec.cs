using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class RecentCommentSpec : BaseSpecification<Comment>
    {
        public RecentCommentSpec(Expression<Func<Comment, bool>> criteria) : base(criteria)
        {
        }

        public RecentCommentSpec(int top) : base(c => c.IsApproved)
        {
            AddInclude(comment => comment
                .Include(c => c.Post)
                .ThenInclude(p => p.PostPublish));
            ApplyOrderByDescending(c => c.CreateOnUtc);
            ApplyPaging(0, top);
        }
    }
}
