using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Comment;

public record ToggleApprovalCommand(Guid[] CommentIds) : ICommand;

public class ToggleApprovalCommandHandler(
    IRepositoryBase<CommentEntity> repo,
    ILogger<ToggleApprovalCommandHandler> logger) : ICommandHandler<ToggleApprovalCommand>
{
    public async Task HandleAsync(ToggleApprovalCommand request, CancellationToken ct)
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