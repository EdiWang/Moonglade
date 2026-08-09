using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.SiteVerification;

public record GetSiteVerificationFileQuery(Guid Id) : IQuery<SiteVerificationFileDetail>;

public class GetSiteVerificationFileQueryHandler(BlogDbContext db)
    : IQueryHandler<GetSiteVerificationFileQuery, SiteVerificationFileDetail>
{
    public async Task<SiteVerificationFileDetail> HandleAsync(GetSiteVerificationFileQuery request, CancellationToken ct)
    {
        var file = await db.SiteVerificationFile
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct);

        return file == null ? null : SiteVerificationFileMapper.ToDetail(file);
    }
}
