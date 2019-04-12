using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PagedCommentSepc : BaseSpecification<Comment>
    {
        public PagedCommentSepc(int pageSize, int pageIndex) : base(c => c.IsApproved)
        {
            var startRow = (pageIndex - 1) * pageSize;

            AddInclude(comment => comment
                .Include(c => c.Post)
                .Include(c => c.CommentReply));
            ApplyOrderByDescending(p => p.CreateOnUtc);
            ApplyPaging(startRow, pageSize);
        }
    }
}
