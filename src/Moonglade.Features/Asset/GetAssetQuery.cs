using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Asset;

public record GetAssetQuery(Guid AssetId) : IQuery<string>;

public class GetAssetQueryHandler(BlogDbContext db) : IQueryHandler<GetAssetQuery, string>
{
    public async Task<string> HandleAsync(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await db.BlogAsset.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AssetId, ct);
        return asset?.Base64Data;
    }
}