using LiteBus.Queries.Abstractions;

namespace Moonglade.Features.Asset;

public record GetAssetQuery(Guid AssetId) : IQuery<string>;

public class GetAssetQueryHandler(IRepositoryBase<BlogAssetEntity> repo) : IQueryHandler<GetAssetQuery, string>
{
    public async Task<string> HandleAsync(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await repo.GetByIdAsync(request.AssetId, ct);
        return asset?.Base64Data;
    }
}