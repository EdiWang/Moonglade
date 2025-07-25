using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record CreatePostCommand(PostEditModel Payload) : ICommand<PostEntity>;

public class CreatePostCommandHandler(
        MoongladeRepository<PostEntity> postRepo,
        MoongladeRepository<TagEntity> tagRepo,
        ILogger<CreatePostCommandHandler> logger,
        IConfiguration configuration,
        IBlogConfig blogConfig)
    : ICommandHandler<CreatePostCommand, PostEntity>
{
    public async Task<PostEntity> HandleAsync(CreatePostCommand request, CancellationToken ct)
    {
        string abs;
        if (string.IsNullOrEmpty(request.Payload.Abstract))
        {
            abs = ContentProcessor.GetPostAbstract(
                request.Payload.EditorContent,
                blogConfig.ContentSettings.PostAbstractWords,
                configuration.GetValue<EditorChoice>("Post:Editor") == EditorChoice.Markdown);
        }
        else
        {
            abs = request.Payload.Abstract.Trim();
        }

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
            PubDateUtc = request.Payload.PostStatus == PostStatusConstants.Published ? utcNow : null,
            ScheduledPublishTimeUtc =
                request.Payload.PostStatus == PostStatusConstants.Scheduled ?
                request.Payload.ScheduledPublishTime :
                null,
            IsDeleted = false,
            PostStatus = request.Payload.PostStatus ?? PostStatusConstants.Draft,
            IsFeatured = request.Payload.Featured,
            HeroImageUrl = string.IsNullOrWhiteSpace(request.Payload.HeroImageUrl) ? null : Helper.SterilizeLink(request.Payload.HeroImageUrl),
            IsOutdated = request.Payload.IsOutdated,
        };

        post.RouteLink = Helper.GenerateRouteLink(post.PubDateUtc.GetValueOrDefault(), request.Payload.Slug);

        await CheckSlugConflict(post, ct);

        AddCategories(request.Payload.SelectedCatIds, post);

        await AddTags(request.Payload.Tags, post, ct);

        await postRepo.AddAsync(post, ct);

        logger.LogInformation($"Created post Id: {post.Id}, Title: '{post.Title}'");
        return post;
    }

    // check if exist same slug under the same day
    private async Task CheckSlugConflict(PostEntity post, CancellationToken ct)
    {
        var todayUtc = DateTime.UtcNow.Date;
        if (await postRepo.AnyAsync(new PostByDateAndSlugSpec(todayUtc, post.Slug, false), ct))
        {
            var uid = Guid.NewGuid();
            post.Slug += $"-{uid.ToString().ToLower()[..8]}";
            logger.LogInformation($"Found conflict for post slug, generated new slug: {post.Slug}");
        }
    }

    private static void AddCategories(Guid[] selectedCatIds, PostEntity post)
    {
        if (selectedCatIds is { Length: > 0 })
        {
            foreach (var id in selectedCatIds)
            {
                post.PostCategory.Add(new()
                {
                    CategoryId = id,
                    PostId = post.Id
                });
            }
        }
    }

    private async Task AddTags(string tagString, PostEntity post, CancellationToken ct)
    {
        var tags = string.IsNullOrWhiteSpace(tagString) ?
            [] :
            tagString.Split(',').ToArray();

        if (tags is { Length: > 0 })
        {
            foreach (var item in tags)
            {
                if (!Helper.IsValidTagName(item)) continue;

                var tag = await tagRepo.FirstOrDefaultAsync(new TagByDisplayNameSpec(item), ct) ?? await CreateTag(item);
                post.Tags.Add(tag);
            }
        }
    }

    private async Task<TagEntity> CreateTag(string item)
    {
        var newTag = new TagEntity
        {
            DisplayName = item,
            NormalizedName = Helper.NormalizeName(item, Helper.TagNormalizationDictionary)
        };

        var tag = await tagRepo.AddAsync(newTag);

        logger.LogInformation($"Created tag: {tag.DisplayName}");
        return tag;
    }
}