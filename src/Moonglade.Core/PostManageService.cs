using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface IPostManageService
    {
        Task<PostEntity> UpdateAsync(Guid id, UpdatePostRequest request);
    }

    public class PostManageService : IPostManageService
    {
        private readonly IBlogAudit _audit;
        private readonly AppSettings _settings;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostEntity> _postRepo;
        private readonly IBlogCache _cache;

        private readonly IDictionary<string, string> _tagNormalizationDictionary;

        public PostManageService(
            IBlogAudit audit,
            IConfiguration configuration,
            IOptions<AppSettings> settings,
            IRepository<TagEntity> tagRepo,
            IRepository<PostEntity> postRepo,
            IBlogCache cache)
        {
            _audit = audit;
            _tagRepo = tagRepo;
            _postRepo = postRepo;
            _cache = cache;
            _settings = settings.Value;

            _tagNormalizationDictionary =
                configuration.GetSection("TagNormalization").Get<Dictionary<string, string>>();
        }

        public async Task<PostEntity> UpdateAsync(Guid id, UpdatePostRequest request)
        {
            var post = await _postRepo.GetAsync(id);
            if (null == post)
            {
                throw new InvalidOperationException($"Post {id} is not found.");
            }

            post.CommentEnabled = request.EnableComment;
            post.PostContent = request.EditorContent;
            post.ContentAbstract = ContentProcessor.GetPostAbstract(
                string.IsNullOrEmpty(request.Abstract) ? request.EditorContent : request.Abstract.Trim(),
                _settings.PostAbstractWords,
                _settings.Editor == EditorChoice.Markdown);

            // Address #221: Do not allow published posts back to draft status
            // postModel.IsPublished = request.IsPublished;
            // Edit draft -> save and publish, ignore false case because #221
            bool isNewPublish = false;
            if (request.IsPublished && !post.IsPublished)
            {
                post.IsPublished = true;
                post.PubDateUtc = DateTime.UtcNow;

                isNewPublish = true;
            }

            // #325: Allow changing publish date for published posts
            if (request.PublishDate is not null && post.PubDateUtc.HasValue)
            {
                var tod = post.PubDateUtc.Value.TimeOfDay;
                var adjustedDate = request.PublishDate.Value;
                post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
            }

            post.Author = request.Author?.Trim();
            post.Slug = request.Slug.ToLower().Trim();
            post.Title = request.Title;
            post.ExposedToSiteMap = request.ExposedToSiteMap;
            post.LastModifiedUtc = DateTime.UtcNow;
            post.IsFeedIncluded = request.IsFeedIncluded;
            post.ContentLanguageCode = request.ContentLanguageCode;
            post.IsFeatured = request.IsFeatured;
            post.IsOriginal = request.IsOriginal;
            post.OriginLink = string.IsNullOrWhiteSpace(request.OriginLink) ? null : Helper.SterilizeLink(request.OriginLink);
            post.HeroImageUrl = string.IsNullOrWhiteSpace(request.HeroImageUrl) ? null : Helper.SterilizeLink(request.HeroImageUrl);

            // compute hash
            var input = $"{post.Slug}#{post.PubDateUtc.GetValueOrDefault():yyyyMMdd}";
            var checkSum = Helper.ComputeCheckSum(input);
            post.HashCheckSum = checkSum;

            // 1. Add new tags to tag lib
            foreach (var item in request.Tags.Where(item => !_tagRepo.Any(p => p.DisplayName == item)))
            {
                await _tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = Tag.NormalizeName(item, _tagNormalizationDictionary)
                });

                await _audit.AddEntry(BlogEventType.Content, BlogEventId.TagCreated,
                    $"Tag '{item}' created.");
            }

            // 2. update tags
            post.Tags.Clear();
            if (request.Tags.Any())
            {
                foreach (var tagName in request.Tags)
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
            if (request.CategoryIds is { Length: > 0 })
            {
                foreach (var cid in request.CategoryIds)
                {
                    post.PostCategory.Add(new()
                    {
                        PostId = post.Id,
                        CategoryId = cid
                    });
                }
            }

            await _postRepo.UpdateAsync(post);

            await _audit.AddEntry(
                BlogEventType.Content,
                isNewPublish ? BlogEventId.PostPublished : BlogEventId.PostUpdated,
                $"Post updated, id: {post.Id}");

            _cache.Remove(CacheDivision.Post, id.ToString());
            return post;
        }
    }
}
