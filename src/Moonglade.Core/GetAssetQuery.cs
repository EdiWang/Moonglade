using Moonglade.Data;

namespace Moonglade.Core;

public record GetAssetQuery(Guid AssetId) : IRequest<string>;

public class GetAssetQueryHandler(MoongladeRepository<BlogAssetEntity> repo) : IRequestHandler<GetAssetQuery, string>
{
    public async Task<string> Handle(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await repo.GetByIdAsync(request.AssetId, ct);
        return asset?.Base64Data;
    }
}