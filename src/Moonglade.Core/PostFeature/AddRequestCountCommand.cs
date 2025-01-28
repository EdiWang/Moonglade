using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.PostFeature;

public record AddRequestCountCommand(Guid PostId) : IRequest<int>;

public class AddRequestCountCommandHandler(
    MoongladeRepository<PostViewEntity> postViewRepo,
    ILogger<AddRequestCountCommandHandler> logger) : IRequestHandler<AddRequestCountCommand, int>
{
    public async Task<int> Handle(AddRequestCountCommand request, CancellationToken cancellationToken)
    {
        var entity = await postViewRepo.GetByIdAsync(request.PostId);
        if (entity is null)
        {
            entity = new PostViewEntity
            {
                PostId = request.PostId,
                RequestCount = 1,
                BeginTimeUtc = DateTime.UtcNow
            };

            await postViewRepo.AddAsync(entity);

            logger.LogInformation("New request added for {PostId}", request.PostId);
            return 1;
        }

        entity.RequestCount++;
        await postViewRepo.UpdateAsync(entity);

        logger.LogInformation("Request count updated for {PostId}, {RequestCount}", request.PostId, entity.RequestCount);

        return entity.RequestCount;
    }
}
