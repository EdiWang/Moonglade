using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public class SaveAssetCommand : IRequest
{
    public SaveAssetCommand(Guid assetId, string assetBase64)
    {
        AssetId = assetId;
        AssetBase64 = assetBase64;
    }

    public Guid AssetId { get; set; }
    public string AssetBase64 { get; set; }
}

public class SaveAssetCommandHandler : IRequestHandler<SaveAssetCommand>
{
    private readonly IRepository<BlogAssetEntity> _repository;

    public SaveAssetCommandHandler(IRepository<BlogAssetEntity> repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(SaveAssetCommand request, CancellationToken cancellationToken)
    {
        if (request.AssetId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(request.AssetId));
        if (string.IsNullOrWhiteSpace(request.AssetBase64)) throw new ArgumentNullException(nameof(request.AssetBase64));

        var asset = await _repository.GetAsync(request.AssetId);
        if (null == asset)
        {
            var entity = new BlogAssetEntity
            {
                Id = request.AssetId,
                Base64Data = request.AssetBase64,
                LastModifiedTimeUtc = DateTime.UtcNow
            };
            await _repository.AddAsync(entity);
        }
        else
        {
            asset.Base64Data = request.AssetBase64;
            asset.LastModifiedTimeUtc = DateTime.UtcNow;
            await _repository.UpdateAsync(asset);
        }

        return Unit.Value;
    }
}