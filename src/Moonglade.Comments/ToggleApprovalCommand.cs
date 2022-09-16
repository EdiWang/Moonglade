using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record ToggleApprovalCommand(Guid[] CommentIds) : IRequest;

public class ToggleApprovalCommandHandler : AsyncRequestHandler<ToggleApprovalCommand>
{
    private readonly IRepository<CommentEntity> _commentRepo;
    public ToggleApprovalCommandHandler(IRepository<CommentEntity> commentRepo) => _commentRepo = commentRepo;

    protected override async Task Handle(ToggleApprovalCommand request, CancellationToken ct)
    {
        var spec = new CommentSpec(request.CommentIds);
        var comments = await _commentRepo.ListAsync(spec);
        foreach (var cmt in comments)
        {
            cmt.IsApproved = !cmt.IsApproved;
            await _commentRepo.UpdateAsync(cmt, ct);
        }
    }
}