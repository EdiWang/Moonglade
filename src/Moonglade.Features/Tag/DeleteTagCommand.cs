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
        if (!await db.Tag.AnyAsync(t => t.Id == request.Id, ct))
            return OperationCode.ObjectNotFound;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.PostTag.Where(pt => pt.TagId == request.Id).ExecuteDeleteAsync(ct);
        await db.Tag.Where(t => t.Id == request.Id).ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);

        logger.LogInformation("Deleted tag: {TagId}", request.Id);
        return OperationCode.Done;
    }
}