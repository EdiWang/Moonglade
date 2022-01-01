using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

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
    private readonly IRepository<CommentEntity> _commentRepo;

    public ToggleApprovalCommandHandler(IRepository<CommentEntity> commentRepo)
    {
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
        }

        return Unit.Value;
    }
}