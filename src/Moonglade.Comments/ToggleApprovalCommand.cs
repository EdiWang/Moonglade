using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record ToggleApprovalCommand(Guid[] CommentIds) : IRequest;

public class ToggleApprovalCommandHandler : IRequestHandler<ToggleApprovalCommand>
{
    private readonly IRepository<CommentEntity> _repo;
    public ToggleApprovalCommandHandler(IRepository<CommentEntity> repo) => _repo = repo;

    public async Task Handle(ToggleApprovalCommand request, CancellationToken ct)
    {
        var spec = new CommentSpec(request.CommentIds);
        var comments = await _repo.ListAsync(spec);
        foreach (var cmt in comments)
        {
            cmt.IsApproved = !cmt.IsApproved;
            await _repo.UpdateAsync(cmt, ct);
        }
    }
}