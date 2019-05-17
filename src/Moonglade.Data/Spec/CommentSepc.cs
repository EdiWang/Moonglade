using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class CommentSepc : BaseSpecification<Comment>
    {
        public CommentSepc(int pageSize, int pageIndex) : base(c => c.IsApproved)
        {
            var startRow = (pageIndex - 1) * pageSize;

            AddInclude(comment => comment
                .Include(c => c.Post)
                .Include(c => c.CommentReply));
            ApplyOrderByDescending(p => p.CreateOnUtc);
            ApplyPaging(startRow, pageSize);
        }

        public CommentSepc(Guid[] ids) : base(c => ids.Contains(c.Id))
        {

        }

        public CommentSepc(Guid postId) : base(c => c.PostId == postId &&
                                                          c.IsApproved)
        {
            AddInclude(comments => comments.Include(c => c.CommentReply));
        }
    }
}
