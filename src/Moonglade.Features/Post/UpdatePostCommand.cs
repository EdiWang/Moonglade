using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : ICommand<PostCommandResult>;
public class UpdatePostCommandHandler(
    BlogDbContext db,
    ILogger<UpdatePostCommandHandler> logger) : ICommandHandler<UpdatePostCommand, PostCommandResult>
{
    public async Task<PostCommandResult> HandleAsync(UpdatePostCommand request, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        var (postId, postEditModel) = request;
        var post = await db.Post
            .Include(p => p.Tags)
            .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Id == postId, ct) ?? throw new InvalidOperationException($"Post {postId} is not found.");

        UpdatePostDetails(post, postEditModel, utcNow);

        await PostEntityHelper.ResolveAndAssignTagsAsync(post, postEditModel.Tags, db, logger, ct);
        PostEntityHelper.SetCategories(post, postEditModel.SelectedCatIds);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Post updated with ID: {PostId}", post.Id);
        return new PostCommandResult
        {
            Id = post.Id,
            RouteLink = post.RouteLink,
            PostContent = post.PostContent,
            PubDateUtc = post.PubDateUtc,
            LastModifiedUtc = post.LastModifiedUtc
        };
    }

    private void UpdatePostDetails(PostEntity post, PostEditModel postEditModel, DateTime utcNow)
    {
        post.CommentEnabled = postEditModel.EnableComment;
        post.PostContent = postEditModel.EditorContent;
        post.ContentAbstract = postEditModel.Abstract.Trim();

        // Only publish the post if it was not yet published
        // Otherwise, updating existing post will result in changing publish date and break the slug URL
        if (post.PostStatus != PostStatus.Published &&
            postEditModel.PostStatus == PostStatus.Published)
        {
            post.PostStatus = PostStatus.Published;
            post.PubDateUtc = utcNow;
        }

        if (postEditModel.PostStatus == PostStatus.Scheduled)
        {
            post.PostStatus = PostStatus.Scheduled;
            post.PubDateUtc = null;
            post.ScheduledPublishTimeUtc = postEditModel.ScheduledPublishTime;
            post.RouteLink = null;
        }

        // Back to draft for unscheduled posts
        if (postEditModel.PostStatus == PostStatus.Draft)
        {
            post.PostStatus = PostStatus.Draft;
            post.PubDateUtc = null;
            post.ScheduledPublishTimeUtc = null;
            post.RouteLink = null;
        }

        post.Author = postEditModel.Author?.Trim();
        post.Slug = postEditModel.Slug.ToLower().Trim();
        post.Title = postEditModel.Title.Trim();
        post.LastModifiedUtc = utcNow;
        post.IsFeedIncluded = postEditModel.FeedIncluded;
        post.ContentLanguageCode = postEditModel.LanguageCode;
        post.IsFeatured = postEditModel.Featured;
        post.IsOutdated = postEditModel.IsOutdated;
        post.ContentType = postEditModel.ContentType;
        post.RouteLink = GetRouteLink(post);
        post.Keywords = ContentProcessor.GetKeywords(postEditModel.Keywords);
    }

    private static string GetRouteLink(PostEntity post)
    {
        return post.PostStatus == PostStatus.Published && post.PubDateUtc.HasValue
            ? UrlHelper.GenerateRouteLink(post.PubDateUtc.Value, post.Slug)
            : null;
    }
}
