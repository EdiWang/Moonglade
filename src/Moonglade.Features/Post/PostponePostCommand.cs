using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Features.Post;

public record PostponePostCommand(Guid PostId, int Hours) : ICommand;

public class PostponePostCommandHandler(
    MoongladeRepository<PostEntity> postRepo,
    ILogger<PostponePostCommandHandler> logger) : ICommandHandler<PostponePostCommand>
{
    public async Task HandleAsync(PostponePostCommand request, CancellationToken ct)
    {
        var post = await postRepo.GetByIdAsync(request.PostId, ct);
        if (post == null)
        {
            logger.LogWarning("Post with ID {PostId} not found", request.PostId);
            return;
        }

        if (post.PostStatus == PostStatusConstants.Scheduled && post.ScheduledPublishTimeUtc.HasValue)
        {
            post.ScheduledPublishTimeUtc = post.ScheduledPublishTimeUtc.Value.AddHours(request.Hours);
            await postRepo.UpdateAsync(post, ct);

            logger.LogInformation("Post {PostId} postponed by {Hours} hour(s)", request.PostId, request.Hours);
        }
    }
}