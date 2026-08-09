using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.SiteVerification;

public record DeleteSiteVerificationFileCommand(Guid Id) : ICommand<OperationCode>;

public class DeleteSiteVerificationFileCommandHandler(
    BlogDbContext db,
    ILogger<DeleteSiteVerificationFileCommandHandler> logger)
    : ICommandHandler<DeleteSiteVerificationFileCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(DeleteSiteVerificationFileCommand request, CancellationToken ct)
    {
        if (!await db.SiteVerificationFile.AnyAsync(f => f.Id == request.Id, ct))
        {
            return OperationCode.ObjectNotFound;
        }

        await db.SiteVerificationFile.Where(f => f.Id == request.Id).ExecuteDeleteAsync(ct);

        logger.LogInformation("Deleted site verification file: {FileId}", request.Id);
        return OperationCode.Done;
    }
}
