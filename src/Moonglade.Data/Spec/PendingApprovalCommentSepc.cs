using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class PendingApprovalCommentSepc : BaseSpecification<CommentEntity>
    {
        public PendingApprovalCommentSepc() : base(c => !c.IsApproved)
        {
        }
    }
}
