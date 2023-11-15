namespace Moonglade.Core;

public record GetAssetQuery(Guid AssetId) : IRequest<string>;

public class GetAssetQueryHandler(IRepository<BlogAssetEntity> repo) : IRequestHandler<GetAssetQuery, string>
{
    public async Task<string> Handle(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await repo.GetAsync(request.AssetId, ct);
        return asset?.Base64Data;
    }
}