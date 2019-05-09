using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class PendingApprovalCommentSepc : BaseSpecification<Comment>
    {
        public PendingApprovalCommentSepc() : base(c => !c.IsApproved)
        {
        }
    }
}
