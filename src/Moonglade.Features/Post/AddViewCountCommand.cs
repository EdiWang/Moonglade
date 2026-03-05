using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Moonglade.Features.Post;

public record AddViewCountCommand(Guid PostId) : ICommand<int>;

public class AddViewCountCommandHandler(
    IRepositoryBase<PostViewEntity> postViewRepo,
    ILogger<AddViewCountCommandHandler> logger) : ICommandHandler<AddViewCountCommand, int>
{
    public async Task<int> HandleAsync(AddViewCountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await postViewRepo.GetByIdAsync(request.PostId, cancellationToken);
            if (entity is null) return 0;

            entity.ViewCount++;
            await postViewRepo.UpdateAsync(entity, cancellationToken);

            return entity.ViewCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add view count for {PostId}", request.PostId);
            return -1;
        }
    }
}
