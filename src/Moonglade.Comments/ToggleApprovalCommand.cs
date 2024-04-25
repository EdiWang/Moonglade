using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record ToggleApprovalCommand(Guid[] CommentIds) : IRequest;

public class ToggleApprovalCommandHandler(MoongladeRepository<CommentEntity> repo) : IRequestHandler<ToggleApprovalCommand>
{
    public async Task Handle(ToggleApprovalCommand request, CancellationToken ct)
    {
        var spec = new CommentByIdsSepc(request.CommentIds);
        var comments = await repo.ListAsync(spec, ct);
        foreach (var cmt in comments)
        {
            cmt.IsApproved = !cmt.IsApproved;
            await repo.UpdateAsync(cmt, ct);
        }
    }
}