using Moonglade.Data.Entities;

namespace Moonglade.Data.Spec;

public class CommentReplySpec : Specification<CommentReplyEntity>
{
    public CommentReplySpec(Guid commentId)
    {
        Query.Where(cr => cr.CommentId == commentId);
    }
}