using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : ICommand<PostCommandResult>;
public class UpdatePostCommandHandler(
    BlogDbContext db,
    ILogger<UpdatePostCommandHandler> logger,
    IEventMediator eventMediator) : ICommandHandler<UpdatePostCommand, PostCommandResult>
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

        if (await db.SaveChangesAsync(ct) > 0)
        {
            await eventMediator.PublishAsync(new PostChangedEvent(), cancellationToken: CancellationToken.None);
        }

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

        post.Author = postEditModel.Author?.Trim();
        post.Slug = postEditModel.Slug.ToLower().Trim();
        post.Title = postEditModel.Title.Trim();
        post.LastModifiedUtc = utcNow;
        post.IsFeedIncluded = postEditModel.FeedIncluded;
        post.ContentLanguageCode = postEditModel.LanguageCode;
        post.IsFeatured = postEditModel.Featured;
        post.IsOutdated = postEditModel.IsOutdated;
        post.ContainsAiAssistedContent = postEditModel.ContainsAiAssistedContent;
        post.ContentType = postEditModel.ContentType;

        // Preserve the publication date when editing a published post so its URL stays stable.
        var publishDateUtc = post.PostStatus == PostStatus.Published ? post.PubDateUtc : utcNow;
        PostEntityHelper.SetStatus(post, postEditModel.PostStatus, publishDateUtc, postEditModel.ScheduledPublishTimeUtc);
        post.Keywords = ContentProcessor.GetKeywords(postEditModel.Keywords);
    }
}
