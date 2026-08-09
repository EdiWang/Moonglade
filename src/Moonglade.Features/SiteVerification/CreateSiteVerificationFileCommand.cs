using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.SiteVerification;

public record CreateSiteVerificationFileCommand(string FileName, string Content, bool IsEnabled)
    : ICommand<SiteVerificationFileCommandResult>;

public class CreateSiteVerificationFileCommandHandler(
    BlogDbContext db,
    ILogger<CreateSiteVerificationFileCommandHandler> logger)
    : ICommandHandler<CreateSiteVerificationFileCommand, SiteVerificationFileCommandResult>
{
    public async Task<SiteVerificationFileCommandResult> HandleAsync(CreateSiteVerificationFileCommand request, CancellationToken ct)
    {
        var validation = SiteVerificationFileConstants.Validate(request.FileName, request.Content);
        if (!validation.Succeeded)
        {
            return SiteVerificationFileCommandResult.ValidationFailed(validation.ErrorMessage);
        }

        var normalizedFileName = SiteVerificationFileConstants.NormalizeFileName(request.FileName);
        if (await db.SiteVerificationFile.AnyAsync(f => f.NormalizedFileName == normalizedFileName, ct))
        {
            return SiteVerificationFileCommandResult.DuplicateFileName();
        }

        var now = DateTime.UtcNow;
        var entity = new SiteVerificationFileEntity
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            NormalizedFileName = normalizedFileName,
            Content = request.Content,
            ContentType = SiteVerificationFileConstants.GetContentType(request.FileName),
            IsEnabled = request.IsEnabled,
            CreatedTimeUtc = now,
            LastModifiedTimeUtc = now
        };

        await db.SiteVerificationFile.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created site verification file: {FileName}", entity.FileName);
        return SiteVerificationFileCommandResult.Done(entity);
    }
}
