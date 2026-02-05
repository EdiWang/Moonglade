using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record UnpublishPostCommand(Guid Id) : ICommand;

public class UnpublishPostCommandHandler(
    MoongladeRepository<PostEntity> repo,
    ILogger<UnpublishPostCommandHandler> logger
    ) : ICommandHandler<UnpublishPostCommand>
{
    public async Task HandleAsync(UnpublishPostCommand request, CancellationToken ct)
    {
        var post = await repo.GetByIdAsync(request.Id, ct);
        if (null == post) return;

        post.PostStatus = PostStatus.Draft;
        post.PubDateUtc = null;
        post.ScheduledPublishTimeUtc = null;
        post.RouteLink = null;
        post.LastModifiedUtc = DateTime.UtcNow;

        await repo.UpdateAsync(post, ct);

        logger.LogInformation("Post [{PostId}] unpublished", request.Id);
    }
}