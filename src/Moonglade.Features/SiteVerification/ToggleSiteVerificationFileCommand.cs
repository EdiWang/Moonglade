using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.SiteVerification;

public record ToggleSiteVerificationFileCommand(Guid Id, bool IsEnabled) : ICommand<OperationCode>;

public class ToggleSiteVerificationFileCommandHandler(
    BlogDbContext db,
    ILogger<ToggleSiteVerificationFileCommandHandler> logger)
    : ICommandHandler<ToggleSiteVerificationFileCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(ToggleSiteVerificationFileCommand request, CancellationToken ct)
    {
        var entity = await db.SiteVerificationFile.FirstOrDefaultAsync(f => f.Id == request.Id, ct);
        if (entity == null) return OperationCode.ObjectNotFound;

        entity.IsEnabled = request.IsEnabled;
        entity.LastModifiedTimeUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated site verification file enabled state: {FileId}, {IsEnabled}",
            request.Id,
            request.IsEnabled);
        return OperationCode.Done;
    }
}
