namespace Moonglade.Core;

public record GetAssetDataQuery(Guid AssetId) : IRequest<string>;

public class GetAssetDataQueryHandler : IRequestHandler<GetAssetDataQuery, string>
{
    private readonly IRepository<BlogAssetEntity> _repository;

    public GetAssetDataQueryHandler(IRepository<BlogAssetEntity> repository)
    {
        _repository = repository;
    }

    public async Task<string> Handle(GetAssetDataQuery request, CancellationToken cancellationToken)
    {
        var asset = await _repository.GetAsync(request.AssetId);
        return asset?.Base64Data;
    }
}