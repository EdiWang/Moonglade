using Microsoft.Extensions.Logging;
using Moonglade.Data;
using System.Collections.Concurrent;

namespace Moonglade.Core.PostFeature;

public record AddRequestCountCommand(Guid PostId) : IRequest<int>;

public class AddRequestCountCommandHandler(
    MoongladeRepository<PostViewEntity> postViewRepo,
    ILogger<AddRequestCountCommandHandler> logger) : IRequestHandler<AddRequestCountCommand, int>
{
    // Ugly code to prevent race condition, which will make Moonglade a single instance application, shit
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<int> Handle(AddRequestCountCommand request, CancellationToken cancellationToken)
    {
        var postLock = _locks.GetOrAdd(request.PostId, _ => new SemaphoreSlim(1, 1));
        await postLock.WaitAsync(cancellationToken);

        try
        {
            var entity = await postViewRepo.GetByIdAsync(request.PostId, cancellationToken);
            if (entity is null)
            {
                entity = new PostViewEntity
                {
                    PostId = request.PostId,
                    RequestCount = 1,
                    BeginTimeUtc = DateTime.UtcNow
                };

                await postViewRepo.AddAsync(entity, cancellationToken);

                logger.LogInformation("New request added for {PostId}", request.PostId);
                return 1;
            }

            entity.RequestCount++;
            await postViewRepo.UpdateAsync(entity, cancellationToken);

            logger.LogInformation("Request count updated for {PostId}, {RequestCount}", request.PostId, entity.RequestCount);

            return entity.RequestCount;
        }
        catch (Exception ex)
        {
            // Not fatal error, eat it and do not block application from running
            logger.LogError(ex, "Failed to add request count for {PostId}", request.PostId);
            return -1;
        }
        finally
        {
            postLock.Release();

            if (postLock.CurrentCount == 1)
            {
                _locks.TryRemove(request.PostId, out _);
            }
        }
    }
}
