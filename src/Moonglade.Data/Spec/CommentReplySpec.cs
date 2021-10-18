using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class CommentReplySpec : BaseSpecification<CommentReplyEntity>
{
    public CommentReplySpec(Guid commentId) : base(cr => cr.CommentId == commentId)
    {

    }
}