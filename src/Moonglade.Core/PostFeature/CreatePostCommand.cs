using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Core.TagFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class CreatePostCommand : IRequest<PostEntity>
{
    public CreatePostCommand(PostEditModel payload)
    {
        Payload = payload;
    }

    public PostEditModel Payload { get; set; }
}

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostEntity>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly ILogger<CreatePostCommandHandler> _logger;
    private readonly IRepository<TagEntity> _tagRepo;
    private readonly AppSettings _settings;
    private readonly IBlogConfig _blogConfig;

    private readonly IDictionary<string, string> _tagNormalizationDictionary;

    public CreatePostCommandHandler(
        IRepository<PostEntity> postRepo,
        ILogger<CreatePostCommandHandler> logger,
        IRepository<TagEntity> tagRepo,
        IOptions<AppSettings> settings,
        IConfiguration configuration,
        IBlogConfig blogConfig)
    {
        _postRepo = postRepo;
        _logger = logger;
        _tagRepo = tagRepo;
        _blogConfig = blogConfig;
        _settings = settings.Value;

        _tagNormalizationDictionary =
            configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
    }

    public async Task<PostEntity> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var abs = ContentProcessor.GetPostAbstract(
            string.IsNullOrEmpty(request.Payload.Abstract) ? request.Payload.EditorContent : request.Payload.Abstract.Trim(),
            _blogConfig.ContentSettings.PostAbstractWords,
            _settings.Editor == EditorChoice.Markdown);

        var post = new PostEntity
        {
            CommentEnabled = request.Payload.EnableComment,
            Id = Guid.NewGuid(),
            PostContent = request.Payload.EditorContent,
            ContentAbstract = abs,
            CreateTimeUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow, // Fix draft orders
            Slug = request.Payload.Slug.ToLower().Trim(),
            Author = request.Payload.Author?.Trim(),
            Title = request.Payload.Title.Trim(),
            ContentLanguageCode = request.Payload.LanguageCode,
            IsFeedIncluded = request.Payload.FeedIncluded,
            PubDateUtc = request.Payload.IsPublished ? DateTime.UtcNow : null,
            IsDeleted = false,
            IsPublished = request.Payload.IsPublished,
            IsFeatured = request.Payload.Featured,
            IsOriginal = request.Payload.IsOriginal,
            OriginLink = string.IsNullOrWhiteSpace(request.Payload.OriginLink) ? null : Helper.SterilizeLink(request.Payload.OriginLink),
            HeroImageUrl = string.IsNullOrWhiteSpace(request.Payload.HeroImageUrl) ? null : Helper.SterilizeLink(request.Payload.HeroImageUrl),
            InlineCss = request.Payload.InlineCss,
            PostExtension = new()
            {
                Hits = 0,
                Likes = 0
            }
        };

        // check if exist same slug under the same day
        var todayUtc = DateTime.UtcNow.Date;
        if (_postRepo.Any(new PostSpec(post.Slug, todayUtc)))
        {
            var uid = Guid.NewGuid();
            post.Slug += $"-{uid.ToString().ToLower()[..8]}";
            _logger.LogInformation($"Found conflict for post slug, generated new slug: {post.Slug}");
        }

        // compute hash
        var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
        var checkSum = Helper.ComputeCheckSum(input);
        post.HashCheckSum = checkSum;

        // add categories
        var catIds = request.Payload.CategoryList.Where(p => p.IsChecked).Select(p => p.Id).ToArray();
        if (catIds is { Length: > 0 })
        {
            foreach (var id in catIds)
            {
                post.PostCategory.Add(new()
                {
                    CategoryId = id,
                    PostId = post.Id
                });
            }
        }

        // add tags
        var tags = string.IsNullOrWhiteSpace(request.Payload.Tags) ?
            Array.Empty<string>() :
            request.Payload.Tags.Split(',').ToArray();

        if (tags is { Length: > 0 })
        {
            foreach (var item in tags)
            {
                if (!Tag.ValidateName(item)) continue;

                var tag = await _tagRepo.GetAsync(q => q.DisplayName == item) ?? await CreateTag(item);
                post.Tags.Add(tag);
            }
        }

        await _postRepo.AddAsync(post);

        return post;
    }

    private async Task<TagEntity> CreateTag(string item)
    {
        var newTag = new TagEntity
        {
            DisplayName = item,
            NormalizedName = Tag.NormalizeName(item, _tagNormalizationDictionary)
        };

        var tag = await _tagRepo.AddAsync(newTag);
        return tag;
    }
}