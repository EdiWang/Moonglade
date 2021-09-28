using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Settings;
using Moonglade.Core.TagFeature;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.PostFeature
{
    public class CreatePostCommand : IRequest<PostEntity>
    {
        public CreatePostCommand(UpdatePostRequest payload)
        {
            Payload = payload;
        }

        public UpdatePostRequest Payload { get; set; }
    }

    public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostEntity>
    {
        private readonly IRepository<PostEntity> _postRepo;
        private readonly IBlogAudit _audit;
        private readonly ILogger<CreatePostCommandHandler> _logger;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly AppSettings _settings;
        private readonly IDictionary<string, string> _tagNormalizationDictionary;

        public CreatePostCommandHandler(
            IRepository<PostEntity> postRepo,
            IBlogAudit audit,
            ILogger<CreatePostCommandHandler> logger,
            IRepository<TagEntity> tagRepo,
            IOptions<AppSettings> settings,
            IConfiguration configuration)
        {
            _postRepo = postRepo;
            _audit = audit;
            _logger = logger;
            _tagRepo = tagRepo;
            _settings = settings.Value;

            _tagNormalizationDictionary =
                configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
        }

        public async Task<PostEntity> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            var abs = ContentProcessor.GetPostAbstract(
                   string.IsNullOrEmpty(request.Payload.Abstract) ? request.Payload.EditorContent : request.Payload.Abstract.Trim(),
                   _settings.PostAbstractWords,
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
                ContentLanguageCode = request.Payload.ContentLanguageCode,
                ExposedToSiteMap = request.Payload.ExposedToSiteMap,
                IsFeedIncluded = request.Payload.IsFeedIncluded,
                PubDateUtc = request.Payload.IsPublished ? DateTime.UtcNow : null,
                IsDeleted = false,
                IsPublished = request.Payload.IsPublished,
                IsFeatured = request.Payload.IsFeatured,
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
            if (request.Payload.CategoryIds is { Length: > 0 })
            {
                foreach (var id in request.Payload.CategoryIds)
                {
                    post.PostCategory.Add(new()
                    {
                        CategoryId = id,
                        PostId = post.Id
                    });
                }
            }

            // add tags
            if (request.Payload.Tags is { Length: > 0 })
            {
                foreach (var item in request.Payload.Tags)
                {
                    if (!Tag.ValidateName(item))
                    {
                        continue;
                    }

                    var tag = await _tagRepo.GetAsync(q => q.DisplayName == item) ?? await CreateTag(item);
                    post.Tags.Add(tag);
                }
            }

            await _postRepo.AddAsync(post);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.PostCreated, $"Post created, id: {post.Id}");

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
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagCreated,
                $"Tag '{tag.NormalizedName}' created.");
            return tag;
        }
    }
}
