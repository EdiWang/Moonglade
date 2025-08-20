using LiteBus.Commands.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record PublishScheduledPostCommand : ICommand<int>;

public class PublishScheduledPostCommandHandler(MoongladeRepository<PostEntity> postRepo) :
    ICommandHandler<PublishScheduledPostCommand, int>
{
    public async Task<int> HandleAsync(PublishScheduledPostCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var scheduledPosts = await postRepo.ListAsync(new ScheduledPostSpec(now), ct);
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
            post.RouteLink = UrlHelper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), post.Slug);

            await postRepo.UpdateAsync(post, ct);

            affectedRows++;
        }

        return affectedRows;
    }
}