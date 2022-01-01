using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.TagFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class UpdatePostCommand : IRequest<PostEntity>
{
    public UpdatePostCommand(Guid id, PostEditModel payload)
    {
        Id = id;
        Payload = payload;
    }

    public Guid Id { get; set; }

    public PostEditModel Payload { get; set; }
}

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostEntity>
{
    private readonly AppSettings _settings;
    private readonly IRepository<TagEntity> _tagRepo;
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IBlogCache _cache;
    private readonly IBlogConfig _blogConfig;

    private readonly IDictionary<string, string> _tagNormalizationDictionary;

    public UpdatePostCommandHandler(
        IConfiguration configuration,
        IOptions<AppSettings> settings,
        IRepository<TagEntity> tagRepo,
        IRepository<PostEntity> postRepo,
        IBlogCache cache,
        IBlogConfig blogConfig)
    {
        _tagRepo = tagRepo;
        _postRepo = postRepo;
        _cache = cache;
        _blogConfig = blogConfig;
        _settings = settings.Value;

        _tagNormalizationDictionary =
            configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
    }

    public async Task<PostEntity> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _postRepo.GetAsync(request.Id);
        if (null == post)
        {
            throw new InvalidOperationException($"Post {request.Id} is not found.");
        }

        post.CommentEnabled = request.Payload.EnableComment;
        post.PostContent = request.Payload.EditorContent;
        post.ContentAbstract = ContentProcessor.GetPostAbstract(
            string.IsNullOrEmpty(request.Payload.Abstract) ? request.Payload.EditorContent : request.Payload.Abstract.Trim(),
            _blogConfig.ContentSettings.PostAbstractWords,
            _settings.Editor == EditorChoice.Markdown);

        // Address #221: Do not allow published posts back to draft status
        // postModel.IsPublished = request.Request.IsPublished;
        // Edit draft -> save and publish, ignore false case because #221
        bool isNewPublish = false;
        if (request.Payload.IsPublished && !post.IsPublished)
        {
            post.IsPublished = true;
            post.PubDateUtc = DateTime.UtcNow;

            isNewPublish = true;
        }

        // #325: Allow changing publish date for published posts
        if (request.Payload.PublishDate is not null && post.PubDateUtc.HasValue)
        {
            var tod = post.PubDateUtc.Value.TimeOfDay;
            var adjustedDate = request.Payload.PublishDate.Value;
            post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
        }

        post.Author = request.Payload.Author?.Trim();
        post.Slug = request.Payload.Slug.ToLower().Trim();
        post.Title = request.Payload.Title.Trim();
        post.LastModifiedUtc = DateTime.UtcNow;
        post.IsFeedIncluded = request.Payload.FeedIncluded;
        post.ContentLanguageCode = request.Payload.LanguageCode;
        post.IsFeatured = request.Payload.Featured;
        post.IsOriginal = request.Payload.IsOriginal;
        post.OriginLink = string.IsNullOrWhiteSpace(request.Payload.OriginLink) ? null : Helper.SterilizeLink(request.Payload.OriginLink);
        post.HeroImageUrl = string.IsNullOrWhiteSpace(request.Payload.HeroImageUrl) ? null : Helper.SterilizeLink(request.Payload.HeroImageUrl);
        post.InlineCss = request.Payload.InlineCss;

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // 1. Add new tags to tag lib
        var tags = string.IsNullOrWhiteSpace(request.Payload.Tags) ?
            Array.Empty<string>() :
            request.Payload.Tags.Split(',').ToArray();

        foreach (var item in tags.Where(item => !_tagRepo.Any(p => p.DisplayName == item)))
        {
            await _tagRepo.AddAsync(new()
            {
                DisplayName = item,
                NormalizedName = Tag.NormalizeName(item, _tagNormalizationDictionary)
            });
        }

        // 2. update tags
        post.Tags.Clear();
        if (tags.Any())
        {
            foreach (var tagName in tags)
            {
                if (!Tag.ValidateName(tagName))
                {
                    continue;
                }

                var tag = await _tagRepo.GetAsync(t => t.DisplayName == tagName);
                if (tag is not null) post.Tags.Add(tag);
            }
        }

        // 3. update categories
        var catIds = request.Payload.CategoryList.Where(p => p.IsChecked).Select(p => p.Id).ToArray();
        post.PostCategory.Clear();

        if (catIds is { Length: > 0 })
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

        await _postRepo.UpdateAsync(post);

        _cache.Remove(CacheDivision.Post, request.Id.ToString());
        return post;
    }
}