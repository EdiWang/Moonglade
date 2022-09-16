namespace Moonglade.Core;

public record GetAssetQuery(Guid AssetId) : IRequest<string>;

public class GetAssetQueryHandler : IRequestHandler<GetAssetQuery, string>
{
    private readonly IRepository<BlogAssetEntity> _repository;

    public GetAssetQueryHandler(IRepository<BlogAssetEntity> repository) => _repository = repository;

    public async Task<string> Handle(GetAssetQuery request, CancellationToken cancellationToken)
    {
        var asset = await _repository.GetAsync(request.AssetId);
        return asset?.Base64Data;
    }
}