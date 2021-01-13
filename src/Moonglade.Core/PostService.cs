using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.DateTimeOps;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Utils;

namespace Moonglade.Core
{
    public interface IPostService
    {
        int CountVisiblePosts();
        int CountByCategoryId(Guid catId);
        int CountByTag(int tagId);
        Task<Post> GetAsync(Guid id);
        Task<PostSlug> GetDraftAsync(Guid postId);
        Task<string> GetContentAsync(PostSlugInfo slugInfo);
        Task<PostSlugSegment> GetSegmentAsync(PostSlugInfo slugInfo);
        Task<PostSlug> GetAsync(PostSlugInfo slugInfo);
        Task<IReadOnlyList<PostSegment>> ListSegmentAsync(PostStatus postStatus);
        Task<IReadOnlyList<PostSegment>> GetInsightsAsync(PostInsightsType insightsType);
        Task<IReadOnlyList<PostListEntry>> GetPagedPostsAsync(int pageSize, int pageIndex, Guid? categoryId = null);
        Task<IReadOnlyList<PostListEntry>> GetByTagAsync(int tagId, int pageSize, int pageIndex);
        Task<PostEntity> CreateAsync(CreatePostRequest request);
        Task<PostEntity> UpdateAsync(EditPostRequest request);
        Task RestoreAsync(Guid postId);
        Task DeleteAsync(Guid postId, bool softDelete = false);
        Task PurgeRecycledAsync();
    }

    public class PostService : IPostService
    {
        private readonly IDateTimeResolver _dateTimeResolver;
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;
        private readonly ILogger<PostService> _logger;
        private readonly AppSettings _settings;
        private readonly IOptions<List<TagNormalization>> _tagNormalization;

        #region Repository Objects

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;

        #endregion

        public PostService(
            ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<TagEntity> tagRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<PostCategoryEntity> postCatRepo,
            IDateTimeResolver dateTimeResolver,
            IBlogAudit audit,
            IBlogCache cache,
            IOptions<List<TagNormalization>> tagNormalization)
        {
            _logger = logger;
            _settings = settings.Value;
            _postRepo = postRepo;
            _tagRepo = tagRepo;
            _postTagRepo = postTagRepo;
            _postCatRepo = postCatRepo;
            _dateTimeResolver = dateTimeResolver;
            _audit = audit;
            _cache = cache;
            _tagNormalization = tagNormalization;
        }

        public int CountVisiblePosts() => _postRepo.Count(p => p.IsPublished && !p.IsDeleted);

        public int CountByCategoryId(Guid catId) =>
            _postCatRepo.Count(c => c.CategoryId == catId
                                          && c.Post.IsPublished
                                          && !c.Post.IsDeleted);
        public int CountByTag(int tagId) => _postTagRepo.Count(p => p.TagId == tagId && p.Post.IsPublished && !p.Post.IsDeleted);

        public Task<Post> GetAsync(Guid id)
        {
            var spec = new PostSpec(id);
            var post = _postRepo.SelectFirstOrDefaultAsync(spec, p => new Post
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                RawPostContent = p.PostContent,
                ContentAbstract = p.ContentAbstract,
                CommentEnabled = p.CommentEnabled,
                CreateTimeUtc = p.CreateTimeUtc,
                PubDateUtc = p.PubDateUtc,
                IsPublished = p.IsPublished,
                ExposedToSiteMap = p.ExposedToSiteMap,
                IsFeedIncluded = p.IsFeedIncluded,
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
            });
            return post;
        }

        public async Task<PostSlug> GetDraftAsync(Guid postId)
        {
            var spec = new PostSpec(postId);
            var postSlugModel = await _postRepo.SelectFirstOrDefaultAsync(spec, post => new PostSlug
            {
                Title = post.Title,
                ContentAbstract = post.ContentAbstract,
                PubDateUtc = DateTime.UtcNow,

                Categories = post.PostCategory.Select(pc => pc.Category).Select(p => new Category
                {
                    DisplayName = p.DisplayName,
                    RouteName = p.RouteName
                }).ToArray(),

                RawPostContent = post.PostContent,

                Tags = post.Tags
                    .Select(p => new Tag
                    {
                        NormalizedName = p.NormalizedName,
                        DisplayName = p.DisplayName
                    }).ToArray(),
                Id = post.Id,
                ExposedToSiteMap = post.ExposedToSiteMap,
                LastModifyOnUtc = post.LastModifiedUtc,
                ContentLanguageCode = post.ContentLanguageCode
            });

            return postSlugModel;
        }

        public Task<string> GetContentAsync(PostSlugInfo slugInfo)
        {
            var date = new DateTime(slugInfo.Year, slugInfo.Month, slugInfo.Day);
            var spec = new PostSpec(date, slugInfo.Slug);

            return _postRepo.SelectFirstOrDefaultAsync(spec,
                post => post.PostContent);
        }

        public Task<PostSlugSegment> GetSegmentAsync(PostSlugInfo slugInfo)
        {
            var date = new DateTime(slugInfo.Year, slugInfo.Month, slugInfo.Day);
            var spec = new PostSpec(date, slugInfo.Slug);

            var model = _postRepo.SelectFirstOrDefaultAsync(spec, post => new PostSlugSegment
            {
                Title = post.Title,
                PubDateUtc = post.PubDateUtc.GetValueOrDefault(),
                LastModifyOnUtc = post.LastModifiedUtc,

                Categories = post.PostCategory
                                 .Select(pc => pc.Category.DisplayName)
                                 .ToArray(),

                Tags = post.Tags
                           .Select(pt => pt.DisplayName)
                           .ToArray()
            });

            return model;
        }

        public async Task<PostSlug> GetAsync(PostSlugInfo slugInfo)
        {
            var date = new DateTime(slugInfo.Year, slugInfo.Month, slugInfo.Day);
            var spec = new PostSpec(date, slugInfo.Slug);

            var pid = await _postRepo.SelectFirstOrDefaultAsync(spec, p => p.Id);
            if (pid == Guid.Empty) return null;

            var psm = await _cache.GetOrCreateAsync(CacheDivision.Post, $"{pid}", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Post"]);

                var postSlugModel = await _postRepo.SelectFirstOrDefaultAsync(spec, post => new PostSlug
                {
                    Title = post.Title,
                    ContentAbstract = post.ContentAbstract,
                    PubDateUtc = post.PubDateUtc.GetValueOrDefault(),

                    Categories = post.PostCategory.Select(pc => pc.Category).Select(p => new Category
                    {
                        DisplayName = p.DisplayName,
                        RouteName = p.RouteName
                    }).ToArray(),

                    RawPostContent = post.PostContent,

                    Tags = post.Tags
                        .Select(p => new Tag
                        {
                            NormalizedName = p.NormalizedName,
                            DisplayName = p.DisplayName
                        }).ToArray(),
                    Id = post.Id,
                    CommentEnabled = post.CommentEnabled,
                    ExposedToSiteMap = post.ExposedToSiteMap,
                    LastModifyOnUtc = post.LastModifiedUtc,
                    ContentLanguageCode = post.ContentLanguageCode,
                    CommentCount = post.Comments.Count(c => c.IsApproved)
                });

                return postSlugModel;
            });

            return psm;
        }

        public Task<IReadOnlyList<PostSegment>> ListSegmentAsync(PostStatus postStatus)
        {
            var spec = new PostSpec(postStatus);
            return _postRepo.SelectAsync(spec, p => new PostSegment
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                PubDateUtc = p.PubDateUtc,
                IsPublished = p.IsPublished,
                IsDeleted = p.IsDeleted,
                CreateTimeUtc = p.CreateTimeUtc,
                Hits = p.PostExtension.Hits
            });
        }

        public Task<IReadOnlyList<PostSegment>> GetInsightsAsync(PostInsightsType insightsType)
        {
            var spec = new PostInsightsSpec(insightsType, 10);
            return _postRepo.SelectAsync(spec, p => new PostSegment
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                PubDateUtc = p.PubDateUtc,
                IsPublished = p.IsPublished,
                IsDeleted = p.IsDeleted,
                CreateTimeUtc = p.CreateTimeUtc,
                Hits = p.PostExtension.Hits
            });
        }

        public Task<IReadOnlyList<PostListEntry>> GetPagedPostsAsync(int pageSize, int pageIndex, Guid? categoryId = null)
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

            var spec = new PostPagingSpec(pageSize, pageIndex, categoryId);
            return _postRepo.SelectAsync(spec, p => new PostListEntry
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
                LangCode = p.ContentLanguageCode,
                Tags = p.Tags.Select(pt => new Tag
                {
                    NormalizedName = pt.NormalizedName,
                    DisplayName = pt.DisplayName
                })
            });
        }

        public Task<IReadOnlyList<PostListEntry>> GetByTagAsync(int tagId, int pageSize, int pageIndex)
        {
            if (tagId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tagId));
            }

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

            var posts = _postTagRepo.SelectAsync(new PostTagSpec(tagId, pageSize, pageIndex),
                p => new PostListEntry
                {
                    Title = p.Post.Title,
                    Slug = p.Post.Slug,
                    ContentAbstract = p.Post.ContentAbstract,
                    PubDateUtc = p.Post.PubDateUtc.GetValueOrDefault(),
                    LangCode = p.Post.ContentLanguageCode,
                    Tags = p.Post.Tags.Select(pt => new Tag
                    {
                        NormalizedName = pt.NormalizedName,
                        DisplayName = pt.DisplayName
                    })
                });

            return posts;
        }

        public async Task<PostEntity> CreateAsync(CreatePostRequest request)
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
                PubDateUtc = request.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                IsDeleted = false,
                IsPublished = request.IsPublished,
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
            if (request.CategoryIds is not null and { Length: > 0 })
            {
                foreach (var cid in request.CategoryIds)
                {
                    post.PostCategory.Add(new()
                    {
                        CategoryId = cid,
                        PostId = post.Id
                    });
                }
            }

            // add tags
            if (request.Tags is not null and { Length: > 0 })
            {
                foreach (var item in request.Tags)
                {
                    if (!TagService.ValidateTagName(item))
                    {
                        continue;
                    }

                    var tag = await _tagRepo.GetAsync(q => q.DisplayName == item);
                    if (null == tag)
                    {
                        var newTag = new TagEntity
                        {
                            DisplayName = item,
                            NormalizedName = TagService.NormalizeTagName(item, _tagNormalization.Value)
                        };

                        tag = await _tagRepo.AddAsync(newTag);
                        await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagCreated,
                            $"Tag '{tag.NormalizedName}' created.");
                    }

                    post.Tags.Add(tag);
                }
            }

            await _postRepo.AddAsync(post);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostCreated, $"Post created, id: {post.Id}");

            return post;
        }

        public async Task<PostEntity> UpdateAsync(EditPostRequest request)
        {
            var post = await _postRepo.GetAsync(request.Id);
            if (null == post)
            {
                throw new InvalidOperationException($"Post {request.Id} is not found.");
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
                var adjustedDate = _dateTimeResolver.ToUtc(request.PublishDate.Value);
                post.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
            }

            post.Slug = request.Slug;
            post.Title = request.Title;
            post.ExposedToSiteMap = request.ExposedToSiteMap;
            post.LastModifiedUtc = DateTime.UtcNow;
            post.IsFeedIncluded = request.IsFeedIncluded;
            post.ContentLanguageCode = request.ContentLanguageCode;

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
            if (request.CategoryIds is not null and { Length: > 0 })
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

            _cache.Remove(CacheDivision.Post, request.Id.ToString());
            return post;
        }

        public async Task RestoreAsync(Guid postId)
        {
            var pp = await _postRepo.GetAsync(postId);
            if (null == pp) return;

            pp.IsDeleted = false;
            await _postRepo.UpdateAsync(pp);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostRestored, $"Post restored, id: {postId}");

            _cache.Remove(CacheDivision.Post, postId.ToString());
        }

        public async Task DeleteAsync(Guid postId, bool softDelete = false)
        {
            var post = await _postRepo.GetAsync(postId);
            if (null == post) return;

            if (softDelete)
            {
                post.IsDeleted = true;
                await _postRepo.UpdateAsync(post);
                await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostRecycled, $"Post '{postId}' moved to Recycle Bin.");
            }
            else
            {
                await _postRepo.DeleteAsync(post);
                await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostDeleted, $"Post '{postId}' deleted from Recycle Bin.");
            }

            _cache.Remove(CacheDivision.Post, postId.ToString());
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
    }
}