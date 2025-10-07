using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : ICommand<PostEntity>;
public class UpdatePostCommandHandler(
    MoongladeRepository<TagEntity> tagRepo,
    MoongladeRepository<PostEntity> postRepo,
    ICacheAside cache,
    IBlogConfig blogConfig,
    IConfiguration configuration,
    ILogger<UpdatePostCommandHandler> logger) : ICommandHandler<UpdatePostCommand, PostEntity>
{
    public async Task<PostEntity> HandleAsync(UpdatePostCommand request, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        var (postId, postEditModel) = request;
        var post = await postRepo.FirstOrDefaultAsync(new PostSpec(postId), ct) ?? throw new InvalidOperationException($"Post {postId} is not found.");

        UpdatePostDetails(post, postEditModel, utcNow);

        await UpdateTags(post, postEditModel.Tags, ct);
        UpdateCats(post, postEditModel.SelectedCatIds);

        await postRepo.UpdateAsync(post, ct);

        cache.Remove(BlogCachePartition.Post.ToString(), post.RouteLink);

        logger.LogInformation("Post updated with ID: {PostId}", post.Id);
        return post;
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
        post.ContentAbstract = string.IsNullOrEmpty(postEditModel.Abstract)
            ? ContentProcessor.GetPostAbstract(
                postEditModel.EditorContent,
                blogConfig.ContentSettings.PostAbstractWords,
                configuration.GetValue<EditorChoice>("Post:Editor") == EditorChoice.Markdown)
            : postEditModel.Abstract.Trim();

        // Only publish the post if it was not yet published
        // Otherwise, updating existing post will result in changing publish date and break the slug URL
        if (post.PostStatus != PostStatusConstants.Published &&
            postEditModel.PostStatus == PostStatusConstants.Published)
        {
            post.PostStatus = PostStatusConstants.Published;
            post.PubDateUtc = utcNow;
        }

        if (postEditModel.PostStatus == PostStatusConstants.Scheduled)
        {
            post.PostStatus = PostStatusConstants.Scheduled;
            post.ScheduledPublishTimeUtc = postEditModel.ScheduledPublishTime;
        }

        // Back to draft for unscheduled posts
        if (postEditModel.PostStatus == PostStatusConstants.Draft)
        {
            post.PostStatus = PostStatusConstants.Draft;
            post.PubDateUtc = null;
            post.ScheduledPublishTimeUtc = null;
            post.RouteLink = null;
        }

        // #325: Allow changing publish date for published posts
        if (postEditModel.ChangePublishDate && postEditModel.PublishDate is not null && post.PubDateUtc.HasValue)
        {
            var tod = post.PubDateUtc.Value.TimeOfDay;
            var adjustedDate = postEditModel.PublishDate.Value;
            post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
        }

        post.Author = postEditModel.Author?.Trim();
        post.Slug = postEditModel.Slug.ToLower().Trim();
        post.Title = postEditModel.Title.Trim();
        post.LastModifiedUtc = utcNow;
        post.IsFeedIncluded = postEditModel.FeedIncluded;
        post.ContentLanguageCode = postEditModel.LanguageCode;
        post.IsFeatured = postEditModel.Featured;
        post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : SecurityHelper.SterilizeLink(postEditModel.HeroImageUrl);
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
