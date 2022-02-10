namespace Moonglade.Core;

public record SaveAssetCommand(Guid AssetId, string AssetBase64) : INotification;

public class SaveAssetCommandHandler : INotificationHandler<SaveAssetCommand>
{
    private readonly IRepository<BlogAssetEntity> _repository;

    public SaveAssetCommandHandler(IRepository<BlogAssetEntity> repository)
    {
        _repository = repository;
    }

    public async Task Handle(SaveAssetCommand request, CancellationToken cancellationToken)
    {
        if (request.AssetId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(request.AssetId));
        if (string.IsNullOrWhiteSpace(request.AssetBase64)) throw new ArgumentNullException(nameof(request.AssetBase64));

        var entity = await _repository.GetAsync(request.AssetId);

        if (null == entity)
        {
            await _repository.AddAsync(new()
            {
                Id = request.AssetId,
                Base64Data = request.AssetBase64,
                LastModifiedTimeUtc = DateTime.UtcNow
            });
        }
        else
        {
            entity.Base64Data = request.AssetBase64;
            entity.LastModifiedTimeUtc = DateTime.UtcNow;
            await _repository.UpdateAsync(entity);
        }
    }
}