using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.ActivityLog;

public record ClearActivityLogsCommand : ICommand<int>;

public class ClearActivityLogsCommandHandler(
    BlogDbContext db,
    ILogger<ClearActivityLogsCommandHandler> logger) : ICommandHandler<ClearActivityLogsCommand, int>
{
    public async Task<int> HandleAsync(ClearActivityLogsCommand request, CancellationToken ct)
    {
        var deletedCount = await db.ActivityLog.ExecuteDeleteAsync(ct);

        logger.LogInformation("Activity logs cleared: {DeletedCount}", deletedCount);
        return deletedCount;
    }
}
