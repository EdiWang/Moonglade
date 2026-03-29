using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Tag;

public record DeleteTagCommand(int Id) : ICommand<OperationCode>;

public class DeleteTagCommandHandler(
    BlogDbContext db,
    ILogger<DeleteTagCommandHandler> logger)
    : ICommandHandler<DeleteTagCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await db.Tag.FindAsync([request.Id], ct);
        if (null == tag) return OperationCode.ObjectNotFound;

        // 1. Delete Post-Tag Association
        await db.PostTag.Where(pt => pt.TagId == request.Id).ExecuteDeleteAsync(ct);

        // 2. Delete Tag itself
        db.Tag.Remove(tag);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}