using Edi.CacheAside.InMemory;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record PublishPostCommand(Guid Id) : IRequest;

public class PublishPostCommandHandler(
    MoongladeRepository<PostEntity> repo,
    ICacheAside cache,
    ILogger<PublishPostCommandHandler> logger
    ) : IRequestHandler<PublishPostCommand>
{
    public async Task Handle(PublishPostCommand request, CancellationToken ct)
    {
        var post = await repo.GetByIdAsync(request.Id, ct);
        if (null == post) return;

        var utcNow = DateTime.UtcNow;

        post.PostStatus = PostStatusConstants.Published;
        post.PubDateUtc = utcNow;
        post.ScheduledPublishTimeUtc = null;
        post.LastModifiedUtc = utcNow;
        post.RouteLink = Helper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), post.Slug);

        await repo.UpdateAsync(post, ct);

        cache.Remove(BlogCachePartition.Post.ToString(), request.Id.ToString());

        logger.LogInformation("Post [{PostId}] Published", request.Id);
    }
}