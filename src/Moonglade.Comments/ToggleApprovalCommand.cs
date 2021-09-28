using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Comments
{
    public class ToggleApprovalCommand : IRequest
    {
        public ToggleApprovalCommand(Guid[] commentIds)
        {
            CommentIds = commentIds;
        }

        public Guid[] CommentIds { get; set; }
    }

    public class ToggleApprovalCommandHandler : IRequestHandler<ToggleApprovalCommand>
    {
        private readonly IBlogAudit _audit;
        private readonly IRepository<CommentEntity> _commentRepo;

        public ToggleApprovalCommandHandler(IBlogAudit audit, IRepository<CommentEntity> commentRepo)
        {
            _audit = audit;
            _commentRepo = commentRepo;
        }

        public async Task<Unit> Handle(ToggleApprovalCommand request, CancellationToken cancellationToken)
        {
            if (request.CommentIds is null || !request.CommentIds.Any())
            {
                throw new ArgumentNullException(nameof(request.CommentIds));
            }

            var spec = new CommentSpec(request.CommentIds);
            var comments = await _commentRepo.GetAsync(spec);
            foreach (var cmt in comments)
            {
                cmt.IsApproved = !cmt.IsApproved;
                await _commentRepo.UpdateAsync(cmt);

                string logMessage = $"Updated comment approval status to '{cmt.IsApproved}' for comment id: '{cmt.Id}'";
                await _audit.AddEntry(
                    BlogEventType.Content, cmt.IsApproved ? BlogEventId.CommentApproval : BlogEventId.CommentDisapproval, logMessage);
            }

            return Unit.Value;
        }
    }
}
