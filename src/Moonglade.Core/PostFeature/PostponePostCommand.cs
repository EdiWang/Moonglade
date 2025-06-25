using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Core.PostFeature;

public record PostponePostCommand(Guid PostId, int Hours) : IRequest;

public class PostponePostCommandHandler(
    MoongladeRepository<PostEntity> postRepo,
    ILogger<PostponePostCommandHandler> logger) : IRequestHandler<PostponePostCommand>
{
    public async Task Handle(PostponePostCommand request, CancellationToken ct)
    {
        var post = await postRepo.GetByIdAsync(request.PostId, ct);
        if (post == null) return;

        post.ScheduledPublishTimeUtc = DateTime.UtcNow.AddHours(request.Hours);
        await postRepo.UpdateAsync(post, ct);

        logger.LogInformation("Post {PostId} postponed by {Hours} hours", request.PostId, request.Hours);
    }
}