using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments
{
    public class DeleteCommentsCommand : IRequest
    {
        public DeleteCommentsCommand(Guid[] ids)
        {
            Ids = ids;
        }

        public Guid[] Ids { get; set; }
    }

    public class DeleteCommentsCommandHandler : IRequestHandler<DeleteCommentsCommand>
    {
        private readonly IBlogAudit _audit;
        private readonly IRepository<CommentEntity> _commentRepo;
        private readonly IRepository<CommentReplyEntity> _commentReplyRepo;

        public DeleteCommentsCommandHandler(
            IBlogAudit audit, IRepository<CommentEntity> commentRepo, IRepository<CommentReplyEntity> commentReplyRepo)
        {
            _audit = audit;
            _commentRepo = commentRepo;
            _commentReplyRepo = commentReplyRepo;
        }

        public async Task<Unit> Handle(DeleteCommentsCommand request, CancellationToken cancellationToken)
        {
            if (request.Ids is null || !request.Ids.Any())
            {
                throw new ArgumentNullException(nameof(request.Ids));
            }

            var spec = new CommentSpec(request.Ids);
            var comments = await _commentRepo.GetAsync(spec);
            foreach (var cmt in comments)
            {
                // 1. Delete all replies
                var cReplies = await _commentReplyRepo.GetAsync(new CommentReplySpec(cmt.Id));
                if (cReplies.Any())
                {
                    await _commentReplyRepo.DeleteAsync(cReplies);
                }

                // 2. Delete comment itself
                await _commentRepo.DeleteAsync(cmt);
                await _audit.AddEntry(BlogEventType.Content, BlogEventId.CommentDeleted, $"Comment '{cmt.Id}' deleted.");
            }

            return Unit.Value;
        }
    }
}
