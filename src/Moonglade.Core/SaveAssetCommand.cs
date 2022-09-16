namespace Moonglade.Core;

public record SaveAssetCommand(Guid AssetId, string AssetBase64) : INotification;

public class SaveAssetCommandHandler : INotificationHandler<SaveAssetCommand>
{
    private readonly IRepository<BlogAssetEntity> _repo;

    public SaveAssetCommandHandler(IRepository<BlogAssetEntity> repo) => _repo = repo;

    public async Task Handle(SaveAssetCommand request, CancellationToken ct)
    {
        if (request.AssetId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(request.AssetId));
        if (string.IsNullOrWhiteSpace(request.AssetBase64)) throw new ArgumentNullException(nameof(request.AssetBase64));

        var entity = await _repo.GetAsync(request.AssetId, ct);

        if (null == entity)
        {
            await _repo.AddAsync(new()
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
            await _repo.UpdateAsync(entity, ct);
        }
    }
}