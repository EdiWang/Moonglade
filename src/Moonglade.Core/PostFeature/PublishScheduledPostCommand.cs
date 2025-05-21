using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record PublishScheduledPostCommand : IRequest<int>;

public class PublishScheduledPostCommandHandler(MoongladeRepository<PostEntity> postRepo, ILogger<PublishScheduledPostCommandHandler> logger) : IRequestHandler<PublishScheduledPostCommand, int>
{
    private readonly MoongladeRepository<PostEntity> _postRepo = postRepo;
    private readonly ILogger<PublishScheduledPostCommandHandler> _logger = logger;

    public async Task<int> Handle(PublishScheduledPostCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var scheduledPosts = await _postRepo.ListAsync(new ScheduledPostSpec(now), ct);
        if (scheduledPosts.Count == 0)
        {
            return 0;
        }

        int affectedRows = 0;
        foreach (var post in scheduledPosts)
        {
            post.PostStatus = PostStatusConstants.Published;
            post.PubDateUtc = now;
            post.ScheduledPublishTimeUtc = null;
            post.RouteLink = Helper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), post.Slug);

            await _postRepo.UpdateAsync(post, ct);

            affectedRows++;
        }

        _logger.LogInformation("Published {Count} scheduled posts", affectedRows);
        return affectedRows;
    }
}