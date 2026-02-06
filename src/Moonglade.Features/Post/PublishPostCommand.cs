using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record PublishPostCommand(Guid Id) : ICommand;

public class PublishPostCommandHandler(
    IRepositoryBase<PostEntity> repo,
    ILogger<PublishPostCommandHandler> logger
    ) : ICommandHandler<PublishPostCommand>
{
    public async Task HandleAsync(PublishPostCommand request, CancellationToken ct)
    {
        var post = await repo.GetByIdAsync(request.Id, ct);
        if (null == post) return;

        var utcNow = DateTime.UtcNow;

        post.PostStatus = PostStatus.Published;
        post.PubDateUtc = utcNow;
        post.ScheduledPublishTimeUtc = null;
        post.LastModifiedUtc = utcNow;
        post.RouteLink = UrlHelper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), post.Slug);

        await repo.UpdateAsync(post, ct);

        logger.LogInformation("Post [{PostId}] Published", request.Id);
    }
}