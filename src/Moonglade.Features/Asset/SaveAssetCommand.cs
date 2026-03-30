using LiteBus.Events.Abstractions;

namespace Moonglade.Features.Asset;

public record SaveAssetEvent(Guid AssetId, string AssetBase64) : IEvent;

public class SaveAssetEventHandler(BlogDbContext db) : IEventHandler<SaveAssetEvent>
{
    public async Task HandleAsync(SaveAssetEvent request, CancellationToken ct)
    {
        if (request.AssetId == Guid.Empty) throw new ArgumentOutOfRangeException(nameof(request.AssetId));
        if (string.IsNullOrWhiteSpace(request.AssetBase64)) throw new ArgumentNullException(nameof(request.AssetBase64));

        var entity = await db.BlogAsset.FirstOrDefaultAsync(a => a.Id == request.AssetId, ct);

        if (null == entity)
        {
            db.BlogAsset.Add(new()
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
        }

        await db.SaveChangesAsync(ct);
    }
}