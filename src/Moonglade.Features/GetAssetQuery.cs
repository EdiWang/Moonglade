using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.Features;

public record GetAssetQuery(Guid AssetId) : IQuery<string>;

public class GetAssetQueryHandler(MoongladeRepository<BlogAssetEntity> repo) : IQueryHandler<GetAssetQuery, string>
{
    public async Task<string> HandleAsync(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await repo.GetByIdAsync(request.AssetId, ct);
        return asset?.Base64Data;
    }
}