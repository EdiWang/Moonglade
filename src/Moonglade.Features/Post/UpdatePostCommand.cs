using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : ICommand<PostCommandResult>;
public class UpdatePostCommandHandler(
    IRepositoryBase<TagEntity> tagRepo,
    IRepositoryBase<PostEntity> postRepo,
    ILogger<UpdatePostCommandHandler> logger) : ICommandHandler<UpdatePostCommand, PostCommandResult>
{
    public async Task<PostCommandResult> HandleAsync(UpdatePostCommand request, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        var (postId, postEditModel) = request;
        var post = await postRepo.FirstOrDefaultAsync(new PostSpec(postId), ct) ?? throw new InvalidOperationException($"Post {postId} is not found.");

        UpdatePostDetails(post, postEditModel, utcNow);

        await UpdateTags(post, postEditModel.Tags, ct);
        UpdateCats(post, postEditModel.SelectedCatIds);

        await postRepo.UpdateAsync(post, ct);

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

    private async Task UpdateTags(PostEntity post, string tagString, CancellationToken ct)
    {
        // 1. Add new tags to tag lib
        var tags = string.IsNullOrWhiteSpace(tagString) ?
            [] :
            tagString.Split(',').ToArray();

        foreach (var item in tags)
        {
            if (!await tagRepo.AnyAsync(new TagByDisplayNameSpec(item), ct))
            {
                await tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = BlogTagHelper.NormalizeName(item, BlogTagHelper.TagNormalizationDictionary)
                }, ct);
            }
        }

        post.Tags.Clear();
        if (tags.Length != 0)
        {
            foreach (var tagName in tags)
            {
                if (!BlogTagHelper.IsValidTagName(tagName))
                {
                    continue;
                }

                var tag = await tagRepo.FirstOrDefaultAsync(new TagByDisplayNameSpec(tagName), ct);
                if (tag is not null) post.Tags.Add(tag);
            }
        }
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
            post.ScheduledPublishTimeUtc = postEditModel.ScheduledPublishTime;
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
        post.RouteLink = UrlHelper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), postEditModel.Slug);
        post.Keywords = ContentProcessor.GetKeywords(postEditModel.Keywords);
    }

    private static void UpdateCats(PostEntity post, Guid[] catIds)
    {
        post.PostCategory.Clear();
        if (catIds.Length != 0)
        {
            foreach (var cid in catIds)
            {
                post.PostCategory.Add(new()
                {
                    PostId = post.Id,
                    CategoryId = cid
                });
            }
        }
    }
}
