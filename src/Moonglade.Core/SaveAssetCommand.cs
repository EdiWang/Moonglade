namespace Moonglade.Core;

public record SaveAssetCommand(Guid AssetId, string AssetBase64) : INotification;

public class SaveAssetCommandHandler(IRepository<BlogAssetEntity> repo) : INotificationHandler<SaveAssetCommand>
{
    public async Task Handle(SaveAssetCommand request, CancellationToken ct)
    {
        if (request.AssetId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(request.AssetId));
        if (string.IsNullOrWhiteSpace(request.AssetBase64)) throw new ArgumentNullException(nameof(request.AssetBase64));

        var entity = await repo.GetAsync(request.AssetId, ct);

        if (null == entity)
        {
            await repo.AddAsync(new()
            {
                Id = request.AssetId,
                Base64Data = request.AssetBase64,
                LastModifiedTimeUtc = DateTime.UtcNow
            }, ct);
        }
        else
        {
            entity.Base64Data = request.AssetBase64;
            entity.LastModifiedTimeUtc = DateTime.UtcNow;
            await repo.UpdateAsync(entity, ct);
        }
    }
}