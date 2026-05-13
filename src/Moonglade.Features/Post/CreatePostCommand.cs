using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record CreatePostCommand(PostEditModel Payload) : ICommand<PostCommandResult>;

public class CreatePostCommandHandler(
        BlogDbContext db,
        ILogger<CreatePostCommandHandler> logger)
    : ICommandHandler<CreatePostCommand, PostCommandResult>
{
    public async Task<PostCommandResult> HandleAsync(CreatePostCommand request, CancellationToken ct)
    {
        var abs = request.Payload.Abstract.Trim();

        var utcNow = DateTime.UtcNow;
        var post = new PostEntity
        {
            CommentEnabled = request.Payload.EnableComment,
            Id = Guid.NewGuid(),
            PostContent = request.Payload.EditorContent,
            ContentAbstract = abs,
            CreateTimeUtc = utcNow,
            LastModifiedUtc = utcNow, // Fix draft orders
            Slug = request.Payload.Slug.ToLower().Trim(),
            Author = request.Payload.Author?.Trim(),
            Title = request.Payload.Title.Trim(),
            ContentLanguageCode = request.Payload.LanguageCode,
            IsFeedIncluded = request.Payload.FeedIncluded,
            PubDateUtc = request.Payload.PostStatus == PostStatus.Published ? utcNow : null,
            ScheduledPublishTimeUtc =
                request.Payload.PostStatus == PostStatus.Scheduled ?
                request.Payload.ScheduledPublishTime :
                null,
            IsDeleted = false,
            PostStatus = request.Payload.PostStatus,
            IsFeatured = request.Payload.Featured,
            IsOutdated = request.Payload.IsOutdated,
            ContentType = request.Payload.ContentType,
        };

        post.Keywords = ContentProcessor.GetKeywords(request.Payload.Keywords);

        await CheckSlugConflict(post, ct);
        post.RouteLink = GetRouteLink(post);

        PostEntityHelper.SetCategories(post, request.Payload.SelectedCatIds);

        await PostEntityHelper.ResolveAndAssignTagsAsync(post, request.Payload.Tags, db, logger, ct);

        db.Post.Add(post);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created post Id: {PostId}, Title: '{PostTitle}'", post.Id, post.Title);
        return new PostCommandResult
        {
            Id = post.Id,
            RouteLink = post.RouteLink,
            PostContent = post.PostContent,
            PubDateUtc = post.PubDateUtc,
            LastModifiedUtc = post.LastModifiedUtc
        };
    }

    // check if exist same slug under the same day
    private async Task CheckSlugConflict(PostEntity post, CancellationToken ct)
    {
        if (post.PostStatus != PostStatus.Published || post.PubDateUtc is null)
        {
            return;
        }

        var publishDateUtc = post.PubDateUtc.Value.Date;
        var exists = await db.Post.AnyAsync(p =>
            p.Slug == post.Slug &&
            p.PostStatus == PostStatus.Published &&
            p.PubDateUtc.HasValue &&
            p.PubDateUtc.Value.Date == publishDateUtc &&
            !p.IsDeleted, ct);

        if (exists)
        {
            var uid = Guid.NewGuid();
            post.Slug += $"-{uid.ToString().ToLower()[..8]}";
            logger.LogInformation("Found conflict for post slug, generated new slug: {Slug}", post.Slug);
        }
    }

    private static string GetRouteLink(PostEntity post)
    {
        return post.PostStatus == PostStatus.Published && post.PubDateUtc.HasValue
            ? UrlHelper.GenerateRouteLink(post.PubDateUtc.Value, post.Slug)
            : null;
    }

}
