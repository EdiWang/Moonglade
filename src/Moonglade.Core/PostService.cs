using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core
{
    public interface IPostService
    {
        int CountPublic();
        int CountByCategory(Guid catId);
        int CountByTag(int tagId);
        int CountByFeatured();
        Task<Post> GetAsync(Guid id);
        Task<Post> GetAsync(PostSlug slug);
        Task<Post> GetDraft(Guid postId);
        Task<IReadOnlyList<PostSegment>> ListSegment(PostStatus postStatus);
        Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegment(PostStatus postStatus, int offset, int pageSize, string keyword = null);
        Task<IReadOnlyList<PostSegment>> ListInsights(PostInsightsType insightsType);
        Task<IReadOnlyList<PostDigest>> List(int pageSize, int pageIndex, Guid? categoryId = null);
        Task<IReadOnlyList<PostDigest>> ListByTag(int tagId, int pageSize, int pageIndex);
        Task<IReadOnlyList<PostDigest>> ListFeatured(int pageSize, int pageIndex);
        Task<PostEntity> CreateAsync(UpdatePostRequest request);
        Task<PostEntity> UpdateAsync(Guid id, UpdatePostRequest request);
        Task RestoreAsync(Guid id);
        Task DeleteAsync(Guid id, bool softDelete = false);
        Task PurgeRecycledAsync();
    }

    public class PostService : IPostService
    {
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;
        private readonly ILogger<PostService> _logger;
        private readonly AppSettings _settings;
        private readonly IOptions<IDictionary<string, string>> _tagNormalization;

        #region Repository Objects

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;

        #endregion

        #region Selectors

        private readonly Expression<Func<PostEntity, Post>> _postSelector = p => new()
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            RawPostContent = p.PostContent,
            ContentAbstract = p.ContentAbstract,
            CommentEnabled = p.CommentEnabled,
            CreateTimeUtc = p.CreateTimeUtc,
            PubDateUtc = p.PubDateUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            IsPublished = p.IsPublished,
            ExposedToSiteMap = p.ExposedToSiteMap,
            IsFeedIncluded = p.IsFeedIncluded,
            Featured = p.IsFeatured,
            ContentLanguageCode = p.ContentLanguageCode,
            Tags = p.Tags.Select(pt => new Tag
            {
                Id = pt.Id,
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            }).ToArray(),
            Categories = p.PostCategory.Select(pc => new Category
            {
                Id = pc.CategoryId,
                DisplayName = pc.Category.DisplayName,
                RouteName = pc.Category.RouteName,
                Note = pc.Category.Note
            }).ToArray()
        };

        private readonly Expression<Func<PostEntity, PostSegment>> _postSegmentSelector = p => new()
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            PubDateUtc = p.PubDateUtc,
            IsPublished = p.IsPublished,
            IsDeleted = p.IsDeleted,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ContentAbstract = p.ContentAbstract,
            Hits = p.PostExtension.Hits
        };

        private readonly Expression<Func<PostEntity, PostDigest>> _postDigestSelector = p => new()
        {
            Title = p.Title,
            Slug = p.Slug,
            ContentAbstract = p.ContentAbstract,
            PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
            LangCode = p.ContentLanguageCode,
            IsFeatured = p.IsFeatured,
            Tags = p.Tags.Select(pt => new Tag
            {
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            })
        };

        private readonly Expression<Func<PostTagEntity, PostDigest>> _postDigestSelectorByTag = p => new()
        {
            Title = p.Post.Title,
            Slug = p.Post.Slug,
            ContentAbstract = p.Post.ContentAbstract,
            PubDateUtc = p.Post.PubDateUtc.GetValueOrDefault(),
            LangCode = p.Post.ContentLanguageCode,
            IsFeatured = p.Post.IsFeatured,
            Tags = p.Post.Tags.Select(pt => new Tag
            {
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            })
        };

        #endregion

        public PostService(
            ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<TagEntity> tagRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<PostCategoryEntity> postCatRepo,
            IBlogAudit audit,
            IBlogCache cache,
            IOptions<IDictionary<string, string>> tagNormalization)
        {
            _logger = logger;
            _settings = settings.Value;
            _postRepo = postRepo;
            _tagRepo = tagRepo;
            _postTagRepo = postTagRepo;
            _postCatRepo = postCatRepo;
            _audit = audit;
            _cache = cache;
            _tagNormalization = tagNormalization;
        }

        #region Counts

        public int CountPublic() => _postRepo.Count(p => p.IsPublished && !p.IsDeleted);

        public int CountByCategory(Guid catId) => _postCatRepo.Count(c => c.CategoryId == catId
                                                                          && c.Post.IsPublished
                                                                          && !c.Post.IsDeleted);
        public int CountByTag(int tagId) => _postTagRepo.Count(p => p.TagId == tagId && p.Post.IsPublished && !p.Post.IsDeleted);

        public int CountByFeatured() => _postRepo.Count(p => p.IsFeatured && p.IsPublished && !p.IsDeleted);

        #endregion

        public Task<Post> GetAsync(Guid id)
        {
            var spec = new PostSpec(id);
            var post = _postRepo.SelectFirstOrDefaultAsync(spec, _postSelector);
            return post;
        }

        public async Task<Post> GetAsync(PostSlug slug)
        {
            var date = new DateTime(slug.Year, slug.Month, slug.Day);
            var spec = new PostSpec(date, slug.Slug);

            var pid = await _postRepo.SelectFirstOrDefaultAsync(spec, p => p.Id);
            if (pid == Guid.Empty) return null;

            var psm = await _cache.GetOrCreateAsync(CacheDivision.Post, $"{pid}", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Post"]);

                var post = await _postRepo.SelectFirstOrDefaultAsync(spec, _postSelector);
                return post;
            });

            return psm;
        }

        public Task<Post> GetDraft(Guid id)
        {
            var spec = new PostSpec(id);
            var post = _postRepo.SelectFirstOrDefaultAsync(spec, _postSelector);
            return post;
        }

        public Task<IReadOnlyList<PostSegment>> ListSegment(PostStatus postStatus)
        {
            var spec = new PostSpec(postStatus);
            return _postRepo.SelectAsync(spec, _postSegmentSelector);
        }

        public async Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegment(
            PostStatus postStatus, int offset, int pageSize, string keyword = null)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    $"{nameof(pageSize)} can not be less than 1, current value: {pageSize}.");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"{nameof(offset)} can not be less than 0, current value: {offset}.");
            }

            var spec = new PostPagingSpec(postStatus, keyword, pageSize, offset);
            var posts = await _postRepo.SelectAsync(spec, _postSegmentSelector);

            Expression<Func<PostEntity, bool>> countExpression = p => null == keyword || p.Title.Contains(keyword);

            switch (postStatus)
            {
                case PostStatus.Draft:
                    countExpression.AndAlso(p => !p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Published:
                    countExpression.AndAlso(p => p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Deleted:
                    countExpression.AndAlso(p => p.IsDeleted);
                    break;
                case PostStatus.NotSet:
                    countExpression.AndAlso(p => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(postStatus), postStatus, null);
            }

            var totalRows = _postRepo.Count(countExpression);
            return (posts, totalRows);
        }

        public Task<IReadOnlyList<PostSegment>> ListInsights(PostInsightsType insightsType)
        {
            var spec = new PostInsightsSpec(insightsType, 10);
            return _postRepo.SelectAsync(spec, _postSegmentSelector);
        }

        public Task<IReadOnlyList<PostDigest>> List(int pageSize, int pageIndex, Guid? categoryId = null)
        {
            ValidatePagingParameters(pageSize, pageIndex);

            var spec = new PostPagingSpec(pageSize, pageIndex, categoryId);
            return _postRepo.SelectAsync(spec, _postDigestSelector);
        }

        public Task<IReadOnlyList<PostDigest>> ListByTag(int tagId, int pageSize, int pageIndex)
        {
            if (tagId <= 0) throw new ArgumentOutOfRangeException(nameof(tagId));
            ValidatePagingParameters(pageSize, pageIndex);

            var posts = _postTagRepo.SelectAsync(new PostTagSpec(tagId, pageSize, pageIndex), _postDigestSelectorByTag);
            return posts;
        }

        public Task<IReadOnlyList<PostDigest>> ListFeatured(int pageSize, int pageIndex)
        {
            ValidatePagingParameters(pageSize, pageIndex);

            var posts = _postRepo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex), _postDigestSelector);
            return posts;
        }

        public async Task<PostEntity> CreateAsync(UpdatePostRequest request)
        {
            var abs = ContentProcessor.GetPostAbstract(
                request.EditorContent, _settings.PostAbstractWords,
                _settings.Editor == EditorChoice.Markdown);

            var post = new PostEntity
            {
                CommentEnabled = request.EnableComment,
                Id = Guid.NewGuid(),
                PostContent = request.EditorContent,
                ContentAbstract = abs,
                CreateTimeUtc = DateTime.UtcNow,
                Slug = request.Slug.ToLower().Trim(),
                Title = request.Title.Trim(),
                ContentLanguageCode = request.ContentLanguageCode,
                ExposedToSiteMap = request.ExposedToSiteMap,
                IsFeedIncluded = request.IsFeedIncluded,
                PubDateUtc = request.IsPublished ? DateTime.UtcNow : null,
                IsDeleted = false,
                IsPublished = request.IsPublished,
                IsFeatured = request.IsSelected,
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
                post.Slug += $"-{uid.ToString().ToLower().Substring(0, 8)}";
                _logger.LogInformation($"Found conflict for post slug, generated new slug: {post.Slug}");
            }

            // add categories
            if (request.CategoryIds is { Length: > 0 })
            {
                foreach (var id in request.CategoryIds)
                {
                    post.PostCategory.Add(new()
                    {
                        CategoryId = id,
                        PostId = post.Id
                    });
                }
            }

            // add tags
            if (request.Tags is { Length: > 0 })
            {
                foreach (var item in request.Tags)
                {
                    if (!TagService.ValidateTagName(item))
                    {
                        continue;
                    }

                    var tag = await _tagRepo.GetAsync(q => q.DisplayName == item) ?? await CreateTag(item);
                    post.Tags.Add(tag);
                }
            }

            await _postRepo.AddAsync(post);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostCreated, $"Post created, id: {post.Id}");

            return post;
        }

        private async Task<TagEntity> CreateTag(string item)
        {
            var newTag = new TagEntity
            {
                DisplayName = item,
                NormalizedName = TagService.NormalizeTagName(item, _tagNormalization.Value)
            };

            var tag = await _tagRepo.AddAsync(newTag);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagCreated,
                $"Tag '{tag.NormalizedName}' created.");
            return tag;
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
                                        request.EditorContent,
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

            post.Slug = request.Slug;
            post.Title = request.Title;
            post.ExposedToSiteMap = request.ExposedToSiteMap;
            post.LastModifiedUtc = DateTime.UtcNow;
            post.IsFeedIncluded = request.IsFeedIncluded;
            post.ContentLanguageCode = request.ContentLanguageCode;
            post.IsFeatured = request.IsSelected;

            // 1. Add new tags to tag lib
            foreach (var item in request.Tags.Where(item => !_tagRepo.Any(p => p.DisplayName == item)))
            {
                await _tagRepo.AddAsync(new()
                {
                    DisplayName = item,
                    NormalizedName = TagService.NormalizeTagName(item, _tagNormalization.Value)
                });

                await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagCreated,
                    $"Tag '{item}' created.");
            }

            // 2. update tags
            post.Tags.Clear();
            if (request.Tags.Any())
            {
                foreach (var tagName in request.Tags)
                {
                    if (!TagService.ValidateTagName(tagName))
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

            await _audit.AddAuditEntry(
                EventType.Content,
                isNewPublish ? AuditEventId.PostPublished : AuditEventId.PostUpdated,
                $"Post updated, id: {post.Id}");

            _cache.Remove(CacheDivision.Post, id.ToString());
            return post;
        }

        public async Task RestoreAsync(Guid id)
        {
            var pp = await _postRepo.GetAsync(id);
            if (null == pp) return;

            pp.IsDeleted = false;
            await _postRepo.UpdateAsync(pp);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostRestored, $"Post restored, id: {id}");

            _cache.Remove(CacheDivision.Post, id.ToString());
        }

        public async Task DeleteAsync(Guid id, bool softDelete = false)
        {
            var post = await _postRepo.GetAsync(id);
            if (null == post) return;

            if (softDelete)
            {
                post.IsDeleted = true;
                await _postRepo.UpdateAsync(post);
                await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostRecycled, $"Post '{id}' moved to Recycle Bin.");
            }
            else
            {
                await _postRepo.DeleteAsync(post);
                await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostDeleted, $"Post '{id}' deleted from Recycle Bin.");
            }

            _cache.Remove(CacheDivision.Post, id.ToString());
        }

        public async Task PurgeRecycledAsync()
        {
            var spec = new PostSpec(true);
            var posts = await _postRepo.GetAsync(spec);
            await _postRepo.DeleteAsync(posts);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.EmptyRecycleBin, "Emptied Recycle Bin.");

            foreach (var guid in posts.Select(p => p.Id))
            {
                _cache.Remove(CacheDivision.Post, guid.ToString());
            }
        }

        private static void ValidatePagingParameters(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    $"{nameof(pageSize)} can not be less than 1, current value: {pageSize}.");
            }

            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex),
                    $"{nameof(pageIndex)} can not be less than 1, current value: {pageIndex}.");
            }
        }
    }
}