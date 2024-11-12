using Edi.CacheAside.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;
using System.Globalization;

namespace Moonglade.Core.PostFeature;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : IRequest<PostEntity>;
public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostEntity>
{
    private readonly MoongladeRepository<PostCategoryEntity> _pcRepository;
    private readonly MoongladeRepository<PostTagEntity> _ptRepository;
    private readonly MoongladeRepository<TagEntity> _tagRepo;
    private readonly MoongladeRepository<PostEntity> _postRepo;
    private readonly ICacheAside _cache;
    private readonly IBlogConfig _blogConfig;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UpdatePostCommandHandler> _logger;
    private readonly bool _useMySqlWorkaround;

    public UpdatePostCommandHandler(
        MoongladeRepository<PostCategoryEntity> pcRepository,
        MoongladeRepository<PostTagEntity> ptRepository,
        MoongladeRepository<TagEntity> tagRepo,
        MoongladeRepository<PostEntity> postRepo,
        ICacheAside cache,
        IBlogConfig blogConfig,
        IConfiguration configuration,
        ILogger<UpdatePostCommandHandler> logger)
    {
        _ptRepository = ptRepository;
        _pcRepository = pcRepository;
        _tagRepo = tagRepo;
        _postRepo = postRepo;
        _cache = cache;
        _blogConfig = blogConfig;
        _configuration = configuration;
        _logger = logger;

        var connStr = configuration.GetConnectionString("MoongladeDatabase");
        var dbType = DatabaseTypeHelper.DetermineDatabaseType(connStr!);
        _useMySqlWorkaround = dbType == DatabaseType.MySQL;
    }

    public async Task<PostEntity> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        var utcNow = DateTime.UtcNow;
        var (guid, postEditModel) = request;
        var post = await _postRepo.GetByIdAsync(guid, ct) ?? throw new InvalidOperationException($"Post {guid} is not found.");

        UpdatePostDetails(post, postEditModel, utcNow);

        await UpdateTags(post, postEditModel.Tags, ct);
        await UpdateCats(post, postEditModel.SelectedCatIds, ct);

        await _postRepo.UpdateAsync(post, ct);

        _cache.Remove(BlogCachePartition.Post.ToString(), post.RouteLink);

        _logger.LogInformation($"Post updated: {post.Id}");
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
            if (!await _tagRepo.AnyAsync(new TagByDisplayNameSpec(item), ct))
            {
                await _tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = Helper.NormalizeName(item, Helper.TagNormalizationDictionary)
                }, ct);
            }
        }

        // 2. update tags
        if (_useMySqlWorkaround)
        {
            var oldTags = await _ptRepository.AsQueryable().Where(pc => pc.PostId == post.Id).ToListAsync(cancellationToken: ct);
            await _ptRepository.DeleteRangeAsync(oldTags, ct);
        }

        post.Tags.Clear();
        if (tags.Any())
        {
            foreach (var tagName in tags)
            {
                if (!Helper.IsValidTagName(tagName))
                {
                    continue;
                }

                var tag = await _tagRepo.FirstOrDefaultAsync(new TagByDisplayNameSpec(tagName), ct);
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
                _blogConfig.ContentSettings.PostAbstractWords,
                _configuration.GetSection("Post:Editor").Get<EditorChoice>() == EditorChoice.Markdown)
            : postEditModel.Abstract.Trim();

        if (postEditModel.IsPublished && !post.IsPublished)
        {
            post.IsPublished = true;
            post.PubDateUtc = utcNow;
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
        post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : Helper.SterilizeLink(postEditModel.HeroImageUrl);
        post.IsOutdated = postEditModel.IsOutdated;
        post.RouteLink = $"{post.PubDateUtc.GetValueOrDefault().ToString("yyyy/M/d", CultureInfo.InvariantCulture)}/{postEditModel.Slug}";
    }

    private async Task UpdateCats(PostEntity post, Guid[] catIds, CancellationToken ct)
    {
        // 3. update categories
        if (_useMySqlWorkaround)
        {
            var oldpcs = await _pcRepository.AsQueryable().Where(pc => pc.PostId == post.Id)
                .ToListAsync(cancellationToken: ct);
            await _pcRepository.DeleteRangeAsync(oldpcs, ct);
        }

        post.PostCategory.Clear();
        if (catIds.Any())
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
