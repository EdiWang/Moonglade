using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Moonglade.Features.Post;

public record AddViewCountCommand(Guid PostId) : ICommand<int>;

public class AddViewCountCommandHandler(
    BlogDbContext db,
    ILogger<AddViewCountCommandHandler> logger) : ICommandHandler<AddViewCountCommand, int>
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<int> HandleAsync(AddViewCountCommand request, CancellationToken cancellationToken)
    {
        var postLock = _locks.GetOrAdd(request.PostId, _ => new SemaphoreSlim(1, 1));
        await postLock.WaitAsync(cancellationToken);

        try
        {
            var entity = await db.PostView.FindAsync([request.PostId], cancellationToken);
            if (entity is null) return 0;

            entity.ViewCount++;
            await AddDailyViewCountAsync(request.PostId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            return entity.ViewCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add view count for {PostId}", request.PostId);
            return -1;
        }
        finally
        {
            postLock.Release();

            if (postLock.CurrentCount == 1)
            {
                _locks.TryRemove(new KeyValuePair<Guid, SemaphoreSlim>(request.PostId, postLock));
            }
        }
    }

    private async Task AddDailyViewCountAsync(Guid postId, CancellationToken cancellationToken)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var daily = await db.PostViewDaily.FindAsync([postId, todayUtc], cancellationToken);
        if (daily is null)
        {
            db.PostViewDaily.Add(new PostViewDailyEntity
            {
                PostId = postId,
                ViewDateUtc = todayUtc,
                ViewCount = 1
            });

            return;
        }

        daily.ViewCount++;
    }
}
