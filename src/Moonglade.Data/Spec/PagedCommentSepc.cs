using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PagedCommentSepc : BaseSpecification<Comment>
    {
        public PagedCommentSepc(int pageSize, int pageIndex) : base(c => c.IsApproved.Value)
        {
            var startRow = (pageIndex - 1) * pageSize;

            AddInclude(c => c.Post);
            AddInclude(c => c.CommentReply);
            ApplyOrderByDescending(p => p.CreateOnUtc);
            ApplyPaging(startRow, pageSize);
        }
    }
}
