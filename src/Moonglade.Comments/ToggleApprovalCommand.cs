using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record ToggleApprovalCommand(Guid[] CommentIds) : IRequest;

public class ToggleApprovalCommandHandler(
    MoongladeRepository<CommentEntity> repo,
    ILogger<ToggleApprovalCommandHandler> logger) : IRequestHandler<ToggleApprovalCommand>
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

        logger.LogInformation("Toggled approval status for {Count} comment(s)", comments.Count);
    }
}