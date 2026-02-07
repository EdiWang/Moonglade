using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public record CancelScheduleCommand(Guid Id) : ICommand;

public class CancelScheduleCommandHandler(
    IRepositoryBase<PostEntity> repo,
    ILogger<CancelScheduleCommandHandler> logger
    ) : ICommandHandler<CancelScheduleCommand>
{
    public async Task HandleAsync(CancelScheduleCommand request, CancellationToken ct)
    {
        var post = await repo.GetByIdAsync(request.Id, ct);
        if (null == post) return;

        post.PostStatus = PostStatus.Draft;
        post.ScheduledPublishTimeUtc = null;
        post.LastModifiedUtc = DateTime.UtcNow;

        await repo.UpdateAsync(post, ct);

        logger.LogInformation("Post [{PostId}] schedule canceled", request.Id);
    }
}
