using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.SiteVerification;

public record ListSiteVerificationFilesQuery : IQuery<List<SiteVerificationFileSummary>>;

public class ListSiteVerificationFilesQueryHandler(BlogDbContext db)
    : IQueryHandler<ListSiteVerificationFilesQuery, List<SiteVerificationFileSummary>>
{
    public async Task<List<SiteVerificationFileSummary>> HandleAsync(ListSiteVerificationFilesQuery request, CancellationToken ct)
    {
        var files = await db.SiteVerificationFile
            .AsNoTracking()
            .OrderBy(f => f.FileName)
            .ToListAsync(ct);

        return files.Select(SiteVerificationFileMapper.ToSummary).ToList();
    }
}
