using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.SiteVerification;

public record UpdateSiteVerificationFileCommand(Guid Id, string FileName, string Content, bool IsEnabled)
    : ICommand<SiteVerificationFileCommandResult>;

public class UpdateSiteVerificationFileCommandHandler(
    BlogDbContext db,
    ILogger<UpdateSiteVerificationFileCommandHandler> logger)
    : ICommandHandler<UpdateSiteVerificationFileCommand, SiteVerificationFileCommandResult>
{
    public async Task<SiteVerificationFileCommandResult> HandleAsync(UpdateSiteVerificationFileCommand request, CancellationToken ct)
    {
        var validation = SiteVerificationFileConstants.Validate(request.FileName, request.Content);
        if (!validation.Succeeded)
        {
            return SiteVerificationFileCommandResult.ValidationFailed(validation.ErrorMessage);
        }

        var entity = await db.SiteVerificationFile.FirstOrDefaultAsync(f => f.Id == request.Id, ct);
        if (entity == null) return SiteVerificationFileCommandResult.ObjectNotFound();

        var normalizedFileName = SiteVerificationFileConstants.NormalizeFileName(request.FileName);
        var duplicateExists = await db.SiteVerificationFile
            .AnyAsync(f => f.Id != request.Id && f.NormalizedFileName == normalizedFileName, ct);
        if (duplicateExists)
        {
            return SiteVerificationFileCommandResult.DuplicateFileName();
        }

        entity.FileName = request.FileName;
        entity.NormalizedFileName = normalizedFileName;
        entity.Content = request.Content;
        entity.ContentType = SiteVerificationFileConstants.GetContentType(request.FileName);
        entity.IsEnabled = request.IsEnabled;
        entity.LastModifiedTimeUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated site verification file: {FileName}", entity.FileName);
        return SiteVerificationFileCommandResult.Done(entity);
    }
}
