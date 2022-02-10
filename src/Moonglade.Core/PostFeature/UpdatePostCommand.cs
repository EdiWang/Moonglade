using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.TagFeature;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : IRequest<PostEntity>;
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
        var (guid, postEditModel) = request;
        var post = await _postRepo.GetAsync(guid);
        if (null == post)
        {
            throw new InvalidOperationException($"Post {guid} is not found.");
        }

        post.CommentEnabled = postEditModel.EnableComment;
        post.PostContent = postEditModel.EditorContent;
        post.ContentAbstract = ContentProcessor.GetPostAbstract(
            string.IsNullOrEmpty(postEditModel.Abstract) ? postEditModel.EditorContent : postEditModel.Abstract.Trim(),
            _blogConfig.ContentSettings.PostAbstractWords,
            _settings.Editor == EditorChoice.Markdown);

        if (postEditModel.IsPublished && !post.IsPublished)
        {
            post.IsPublished = true;
            post.PubDateUtc = DateTime.UtcNow;
        }

        // #325: Allow changing publish date for published posts
        if (postEditModel.PublishDate is not null && post.PubDateUtc.HasValue)
        {
            var tod = post.PubDateUtc.Value.TimeOfDay;
            var adjustedDate = postEditModel.PublishDate.Value;
            post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
        }

        post.Author = postEditModel.Author?.Trim();
        post.Slug = postEditModel.Slug.ToLower().Trim();
        post.Title = postEditModel.Title.Trim();
        post.LastModifiedUtc = DateTime.UtcNow;
        post.IsFeedIncluded = postEditModel.FeedIncluded;
        post.ContentLanguageCode = postEditModel.LanguageCode;
        post.IsFeatured = postEditModel.Featured;
        post.IsOriginal = postEditModel.IsOriginal;
        post.OriginLink = string.IsNullOrWhiteSpace(postEditModel.OriginLink) ? null : Helper.SterilizeLink(postEditModel.OriginLink);
        post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : Helper.SterilizeLink(postEditModel.HeroImageUrl);
        post.InlineCss = postEditModel.InlineCss;

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // 1. Add new tags to tag lib
        var tags = string.IsNullOrWhiteSpace(postEditModel.Tags) ?
            Array.Empty<string>() :
            postEditModel.Tags.Split(',').ToArray();

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
        post.PostCategory.Clear();
        if (postEditModel.SelectedCatIds is { Length: > 0 })
        {
            foreach (var cid in postEditModel.SelectedCatIds)
            {
                post.PostCategory.Add(new()
                {
                    PostId = post.Id,
                    CategoryId = cid
                });
            }
        }

        await _postRepo.UpdateAsync(post);

        _cache.Remove(CacheDivision.Post, guid.ToString());
        return post;
    }
}