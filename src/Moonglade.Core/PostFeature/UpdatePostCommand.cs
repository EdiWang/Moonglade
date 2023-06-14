using Edi.CacheAside.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moonglade.Configuration;
using Moonglade.Core.TagFeature;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record UpdatePostCommand(Guid Id, PostEditModel Payload) : IRequest<PostEntity>;
public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostEntity>
{
    private readonly IRepository<PostCategoryEntity> _pcRepository;
    private readonly IRepository<PostTagEntity> _ptRepository;
    private readonly IRepository<TagEntity> _tagRepo;
    private readonly IRepository<PostEntity> _postRepo;
    private readonly ICacheAside _cache;
    private readonly IBlogConfig _blogConfig;
    private readonly IConfiguration _configuration;
    private readonly bool _useMySqlWorkaround;

    public UpdatePostCommandHandler(
        IRepository<PostCategoryEntity> pcRepository,
        IRepository<PostTagEntity> ptRepository,
        IRepository<TagEntity> tagRepo,
        IRepository<PostEntity> postRepo,
        ICacheAside cache,
        IBlogConfig blogConfig, IConfiguration configuration)
    {
        _ptRepository = ptRepository;
        _pcRepository = pcRepository;
        _tagRepo = tagRepo;
        _postRepo = postRepo;
        _cache = cache;
        _blogConfig = blogConfig;
        _configuration = configuration;

        string dbType = configuration.GetConnectionString("DatabaseType");
        _useMySqlWorkaround = dbType!.ToLower().Trim() == "mysql";
    }

    public async Task<PostEntity> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        var (guid, postEditModel) = request;
        var post = await _postRepo.GetAsync(guid, ct);
        if (null == post)
        {
            throw new InvalidOperationException($"Post {guid} is not found.");
        }

        post.CommentEnabled = postEditModel.EnableComment;
        post.PostContent = postEditModel.EditorContent;
        post.ContentAbstract = ContentProcessor.GetPostAbstract(
            string.IsNullOrEmpty(postEditModel.Abstract) ? postEditModel.EditorContent : postEditModel.Abstract.Trim(),
            _blogConfig.ContentSettings.PostAbstractWords,
            _configuration.GetSection("Editor").Get<EditorChoice>() == EditorChoice.Markdown);

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
        post.IsOriginal = string.IsNullOrWhiteSpace(request.Payload.OriginLink);
        post.OriginLink = string.IsNullOrWhiteSpace(postEditModel.OriginLink) ? null : Helper.SterilizeLink(postEditModel.OriginLink);
        post.HeroImageUrl = string.IsNullOrWhiteSpace(postEditModel.HeroImageUrl) ? null : Helper.SterilizeLink(postEditModel.HeroImageUrl);
        post.InlineCss = postEditModel.InlineCss;
        post.IsOutdated = postEditModel.IsOutdated;

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // 1. Add new tags to tag lib
        var tags = string.IsNullOrWhiteSpace(postEditModel.Tags) ?
            Array.Empty<string>() :
            postEditModel.Tags.Split(',').ToArray();

        foreach (var item in tags)
        {
            if (!await _tagRepo.AnyAsync(p => p.DisplayName == item, ct))
            {
                await _tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = Tag.NormalizeName(item, Helper.TagNormalizationDictionary)
                }, ct);
            }
        }

        // 2. update tags
        if (_useMySqlWorkaround)
        {
            var oldTags = await _ptRepository.AsQueryable().Where(pc => pc.PostId == post.Id).ToListAsync(cancellationToken: ct);
            await _ptRepository.DeleteAsync(oldTags, ct);
        }

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
        if (_useMySqlWorkaround)
        {
            var oldpcs = await _pcRepository.AsQueryable().Where(pc => pc.PostId == post.Id)
                .ToListAsync(cancellationToken: ct);
            await _pcRepository.DeleteAsync(oldpcs, ct);
        }

        post.PostCategory.Clear();
        if (postEditModel.SelectedCatIds.Any())
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

        await _postRepo.UpdateAsync(post, ct);

        _cache.Remove(BlogCachePartition.Post.ToString(), guid.ToString());
        return post;
    }
}
