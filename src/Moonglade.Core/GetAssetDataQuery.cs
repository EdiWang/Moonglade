using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core;

public class GetAssetDataQuery : IRequest<string>
{
    public GetAssetDataQuery(Guid assetId)
    {
        AssetId = assetId;
    }

    public Guid AssetId { get; set; }
}

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