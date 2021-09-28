using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Linq;

namespace Moonglade.Data.Spec
{
    public sealed class CommentSpec : BaseSpecification<CommentEntity>
    {
        public CommentSpec(int pageSize, int pageIndex) : base(c => true)
        {
            var startRow = (pageIndex - 1) * pageSize;

            AddInclude(comment => comment
                .Include(c => c.Post)
                .Include(c => c.Replies));
            ApplyOrderByDescending(p => p.CreateTimeUtc);
            ApplyPaging(startRow, pageSize);
        }

        public CommentSpec(Guid[] ids) : base(c => ids.Contains(c.Id))
        {

        }

        public CommentSpec(Guid postId) : base(c => c.PostId == postId &&
                                                          c.IsApproved)
        {
            AddInclude(comments => comments.Include(c => c.Replies));
        }
    }
}
