namespace Moonglade.Core;

public record GetAssetQuery(Guid AssetId) : IRequest<string>;

public class GetAssetQueryHandler : IRequestHandler<GetAssetQuery, string>
{
    private readonly IRepository<BlogAssetEntity> _repo;

    public GetAssetQueryHandler(IRepository<BlogAssetEntity> repo) => _repo = repo;

    public async Task<string> Handle(GetAssetQuery request, CancellationToken ct)
    {
        var asset = await _repo.GetAsync(request.AssetId, ct);
        return asset?.Base64Data;
    }
}