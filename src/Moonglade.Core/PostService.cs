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

namespace Moonglade.Core
{
    public class PostService : BlogService
    {
        private readonly IDateTimeResolver _dateTimeResolver;
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;

        #region Repository Objects

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<PostExtensionEntity> _postExtensionRepo;
        private readonly IRepository<TagEntity> _tagRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;

        #endregion

        public PostService(ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<PostExtensionEntity> postExtensionRepo,
            IRepository<TagEntity> tagRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<CategoryEntity> catRepo,
            IRepository<PostCategoryEntity> postCatRepo,
            IDateTimeResolver dateTimeResolver,
            IBlogAudit audit,
            IBlogCache cache) : base(logger, settings)
        {
            _postRepo = postRepo;
            _postExtensionRepo = postExtensionRepo;
            _tagRepo = tagRepo;
            _postTagRepo = postTagRepo;
            _catRepo = catRepo;
            _postCatRepo = postCatRepo;
            _dateTimeResolver = dateTimeResolver;
            _audit = audit;
            _cache = cache;
        }

        public int CountVisiblePosts() => _postRepo.Count(p => p.IsPublished && !p.IsDeleted);

        public int CountByCategoryId(Guid catId) =>
            _postCatRepo.Count(c => c.CategoryId == catId
                                          && c.Post.IsPublished
                                          && !c.Post.IsDeleted);


        public async Task UpdateStatisticAsync(Guid postId, int likes = 0)
        {
            var pp = await _postExtensionRepo.GetAsync(postId);
            if (pp == null) return;

            if (likes > 0)
            {
                pp.Likes += likes;
            }
            else
            {
                pp.Hits += 1;
            }

            await _postExtensionRepo.UpdateAsync(pp);
        }

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
                CreateOnUtc = p.CreateOnUtc,
                PubDateUtc = p.PubDateUtc,
                IsPublished = p.IsPublished,
                ExposedToSiteMap = p.ExposedToSiteMap,
                IsFeedIncluded = p.IsFeedIncluded,
                ContentLanguageCode = p.ContentLanguageCode,
                Tags = p.PostTag.Select(pt => new Tag
                {
                    Id = pt.TagId,
                    NormalizedName = pt.Tag.NormalizedName,
                    DisplayName = pt.Tag.DisplayName
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

        public async Task<PostSlug> GetDraftPreviewAsync(Guid postId)
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

                Tags = post.PostTag.Select(pt => pt.Tag)
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

            if (null != postSlugModel)
            {
                postSlugModel.RawPostContent = ContentProcessor.AddLazyLoadToImgTag(postSlugModel.RawPostContent);
            }

            return postSlugModel;
        }

        public Task<string> GetRawContentAsync(PostSlugInfo slugInfo)
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

                Tags = post.PostTag
                           .Select(pt => pt.Tag.DisplayName)
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
                entry.SlidingExpiration = TimeSpan.FromMinutes(AppSettings.CacheSlidingExpirationMinutes["Post"]);

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
                    Hits = post.PostExtension.Hits,
                    Likes = post.PostExtension.Likes,

                    Tags = post.PostTag.Select(pt => pt.Tag)
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
                    CommentCount = post.Comment.Count(c => c.IsApproved)
                });

                if (null != postSlugModel)
                {
                    postSlugModel.RawPostContent = ContentProcessor.AddLazyLoadToImgTag(postSlugModel.RawPostContent);
                }

                return postSlugModel;
            });

            return psm;
        }

        public Task<IReadOnlyList<PostSegment>> ListSegmentAsync(PostPublishStatus postPublishStatus)
        {
            var spec = new PostSpec(postPublishStatus);
            return _postRepo.SelectAsync(spec, p => new PostSegment
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                PubDateUtc = p.PubDateUtc,
                IsPublished = p.IsPublished,
                IsDeleted = p.IsDeleted,
                CreateOnUtc = p.CreateOnUtc,
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
                CreateOnUtc = p.CreateOnUtc,
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
                Tags = p.PostTag.Select(pt => new Tag
                {
                    NormalizedName = pt.Tag.NormalizedName,
                    DisplayName = pt.Tag.DisplayName
                })
            });
        }

        public Task<IReadOnlyList<PostListEntry>> GetByTagAsync(int tagId)
        {
            if (tagId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tagId));
            }

            var posts = _postTagRepo.SelectAsync(new PostTagSpec(tagId),
                p => new PostListEntry
                {
                    Title = p.Post.Title,
                    Slug = p.Post.Slug,
                    ContentAbstract = p.Post.ContentAbstract,
                    PubDateUtc = p.Post.PubDateUtc.GetValueOrDefault(),
                    LangCode = p.Post.ContentLanguageCode
                });

            return posts;
        }

        public async Task<PostEntity> CreateAsync(CreatePostRequest request)
        {
            var abs = ContentProcessor.GetPostAbstract(
                request.EditorContent, AppSettings.PostAbstractWords,
                AppSettings.Editor == EditorChoice.Markdown);

            var post = new PostEntity
            {
                CommentEnabled = request.EnableComment,
                Id = Guid.NewGuid(),
                PostContent = request.EditorContent,
                ContentAbstract = abs,
                CreateOnUtc = DateTime.UtcNow,
                Slug = request.Slug.ToLower().Trim(),
                Title = request.Title.Trim(),
                ContentLanguageCode = request.ContentLanguageCode,
                ExposedToSiteMap = request.ExposedToSiteMap,
                IsFeedIncluded = request.IsFeedIncluded,
                PubDateUtc = request.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                IsDeleted = false,
                IsPublished = request.IsPublished,
                PostExtension = new PostExtensionEntity
                {
                    Hits = 0,
                    Likes = 0
                }
            };

            // check if exist same slug under the same day
            // linq to sql fix:
            // cannot write "p.PubDateUtc.GetValueOrDefault().Date == DateTime.UtcNow.Date"
            // it will not blow up, but can result in select ENTIRE posts and evaluated in memory!!!
            // - The LINQ expression 'where (Convert([p]?.PubDateUtc?.GetValueOrDefault(), DateTime).Date == DateTime.UtcNow.Date)' could not be translated and will be evaluated locally
            // Why EF Core this diao yang?
            if (_postRepo.Any(p =>
                p.Slug == post.Slug &&
                p.PubDateUtc != null &&
                p.PubDateUtc.Value.Year == DateTime.UtcNow.Date.Year &&
                p.PubDateUtc.Value.Month == DateTime.UtcNow.Date.Month &&
                p.PubDateUtc.Value.Day == DateTime.UtcNow.Date.Day))
            {
                var uid = Guid.NewGuid();
                post.Slug += $"-{uid.ToString().ToLower().Substring(0, 8)}";
                Logger.LogInformation($"Found conflict for post slug, generated new slug: {post.Slug}");
            }

            // add categories
            if (null != request.CategoryIds && request.CategoryIds.Length > 0)
            {
                foreach (var cid in request.CategoryIds)
                {
                    if (_catRepo.Any(c => c.Id == cid))
                    {
                        post.PostCategory.Add(new PostCategoryEntity
                        {
                            CategoryId = cid,
                            PostId = post.Id
                        });
                    }
                }
            }

            // add tags
            if (null != request.Tags && request.Tags.Length > 0)
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
                            NormalizedName = TagService.NormalizeTagName(item)
                        };

                        tag = await _tagRepo.AddAsync(newTag);
                        await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagCreated,
                            $"Tag '{tag.NormalizedName}' created.");
                    }

                    post.PostTag.Add(new PostTagEntity
                    {
                        TagId = tag.Id,
                        PostId = post.Id
                    });
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
                                        AppSettings.PostAbstractWords,
                                        AppSettings.Editor == EditorChoice.Markdown);

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
            if (request.PublishDate != null && post.PubDateUtc.HasValue)
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
                await _tagRepo.AddAsync(new TagEntity
                {
                    DisplayName = item,
                    NormalizedName = TagService.NormalizeTagName(item)
                });

                await _audit.AddAuditEntry(EventType.Content, AuditEventId.TagCreated,
                    $"Tag '{item}' created.");
            }

            // 2. update tags
            post.PostTag.Clear();
            if (request.Tags.Any())
            {
                foreach (var tagName in request.Tags)
                {
                    if (!TagService.ValidateTagName(tagName))
                    {
                        continue;
                    }

                    var tag = await _tagRepo.GetAsync(t => t.DisplayName == tagName);
                    if (tag != null) post.PostTag.Add(new PostTagEntity
                    {
                        PostId = post.Id,
                        TagId = tag.Id
                    });
                }
            }

            // 3. update categories
            post.PostCategory.Clear();
            if (null != request.CategoryIds && request.CategoryIds.Length > 0)
            {
                foreach (var cid in request.CategoryIds)
                {
                    if (_catRepo.Any(c => c.Id == cid))
                    {
                        post.PostCategory.Add(new PostCategoryEntity
                        {
                            PostId = post.Id,
                            CategoryId = cid
                        });
                    }
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

        public async Task RestoreDeletedAsync(Guid postId)
        {
            var pp = await _postRepo.GetAsync(postId);
            if (null == pp) return;

            pp.IsDeleted = false;
            await _postRepo.UpdateAsync(pp);
            await _audit.AddAuditEntry(EventType.Content, AuditEventId.PostRestored, $"Post restored, id: {postId}");

            _cache.Remove(CacheDivision.Post, postId.ToString());
        }

        public async Task DeleteAsync(Guid postId, bool isRecycle = false)
        {
            var post = await _postRepo.GetAsync(postId);
            if (null == post) return;

            if (isRecycle)
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

        public async Task DeleteRecycledAsync()
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