using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.ActivityLog;

public record DeleteActivityLogCommand(long Id) : ICommand<OperationCode>;

public class DeleteActivityLogCommandHandler(
    BlogDbContext db,
    ILogger<DeleteActivityLogCommandHandler> logger) : ICommandHandler<DeleteActivityLogCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteActivityLogCommand request, CancellationToken ct)
    {
        var entity = await db.ActivityLog.FindAsync([request.Id], ct);
        if (entity == null)
        {
            logger.LogWarning("Activity log not found: {Id}", request.Id);
            return OperationCode.ObjectNotFound;
        }

        db.ActivityLog.Remove(entity);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Activity log deleted: {Id}", request.Id);
        return OperationCode.Done;
    }
}
