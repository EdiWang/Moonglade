using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.SiteVerification;

public record GetPublicSiteVerificationFileQuery(string FileName) : IQuery<PublicSiteVerificationFile>;

public class GetPublicSiteVerificationFileQueryHandler(BlogDbContext db)
    : IQueryHandler<GetPublicSiteVerificationFileQuery, PublicSiteVerificationFile>
{
    public async Task<PublicSiteVerificationFile> HandleAsync(GetPublicSiteVerificationFileQuery request, CancellationToken ct)
    {
        var validation = SiteVerificationFileConstants.ValidateFileName(request.FileName);
        if (!validation.Succeeded) return null;

        var normalizedFileName = SiteVerificationFileConstants.NormalizeFileName(request.FileName);
        var file = await db.SiteVerificationFile
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.NormalizedFileName == normalizedFileName && f.IsEnabled, ct);

        return file == null ? null : SiteVerificationFileMapper.ToPublicFile(file);
    }
}
