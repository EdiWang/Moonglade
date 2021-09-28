using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;

namespace Moonglade.Data.Spec
{
    public class CommentReplySpec : BaseSpecification<CommentReplyEntity>
    {
        public CommentReplySpec(Guid commentId) : base(cr => cr.CommentId == commentId)
        {

        }
    }
}
