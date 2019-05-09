using System;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class CommentReplySpec : BaseSpecification<CommentReply>
    {
        public CommentReplySpec(Guid commentId) : base(cr => cr.CommentId == commentId)
        {

        }
    }
}
