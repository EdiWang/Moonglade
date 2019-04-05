using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class CommentReplySpec : BaseSpecification<CommentReply>
    {
        //public CommentReplySpec(Expression<Func<CommentReply, bool>> criteria) : base(criteria)
        //{
        //}

        public CommentReplySpec(Guid commentId) : base(cr => cr.CommentId == commentId)
        {

        }
    }
}
