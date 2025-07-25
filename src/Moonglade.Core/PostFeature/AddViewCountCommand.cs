using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using System.Collections.Concurrent;

namespace Moonglade.Core.PostFeature;

public record AddViewCountCommand(Guid PostId, string Ip) : ICommand<int>;

public class AddViewCountCommandHandler(
    MoongladeRepository<PostViewEntity> postViewRepo,
    ILogger<AddViewCountCommandHandler> logger) : ICommandHandler<AddViewCountCommand, int>
{
    // Ugly code to prevent race condition, which will make Moonglade a single instance application, shit
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public async Task<int> HandleAsync(AddViewCountCommand request, CancellationToken cancellationToken)
    {
        var postLock = _locks.GetOrAdd(request.PostId, _ => new SemaphoreSlim(1, 1));
        await postLock.WaitAsync(cancellationToken);

        try
        {
            var entity = await postViewRepo.GetByIdAsync(request.PostId, cancellationToken);
            if (entity is null) return 0;

            entity.ViewCount++;
            await postViewRepo.UpdateAsync(entity, cancellationToken);

            logger.LogInformation("View count updated for {PostId}, {ViewCount}", request.PostId, entity.ViewCount);

            return entity.ViewCount;
        }
        catch (Exception ex)
        {
            // Not fatal error, eat it and do not block application from running
            logger.LogError(ex, "Failed to add view count for {PostId}", request.PostId);
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
