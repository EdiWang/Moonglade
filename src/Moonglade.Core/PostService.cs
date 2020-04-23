using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.DateTimeOps;
using Moonglade.HtmlEncoding;
using Moonglade.Model;
using Moonglade.Model.Settings;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Core
{
    public class PostService : MoongladeService
    {
        private readonly IHtmlCodec _htmlCodec;
        private readonly IBlogConfig _blogConfig;
        private readonly IDateTimeResolver _dateTimeResolver;
        private readonly IMoongladeAudit _moongladeAudit;

        #region Repository Objects

        private readonly IRepository<PostEntity> _postRepository;
        private readonly IRepository<PostExtensionEntity> _postExtensionRepository;
        private readonly IRepository<PostPublishEntity> _postPublishRepository;
        private readonly IRepository<TagEntity> _tagRepository;
        private readonly IRepository<PostTagEntity> _postTagRepository;
        private readonly IRepository<CategoryEntity> _categoryRepository;
        private readonly IRepository<PostCategoryEntity> _postCategoryRepository;

        #endregion

        public PostService(ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepository,
            IRepository<PostExtensionEntity> postExtensionRepository,
            IRepository<TagEntity> tagRepository,
            IRepository<PostTagEntity> postTagRepository,
            IRepository<PostPublishEntity> postPublishRepository,
            IRepository<CategoryEntity> categoryRepository,
            IRepository<PostCategoryEntity> postCategoryRepository,
            IHtmlCodec htmlCodec,
            IBlogConfig blogConfig,
            IDateTimeResolver dateTimeResolver,
            IMoongladeAudit moongladeAudit) : base(logger, settings)
        {
            _postRepository = postRepository;
            _postExtensionRepository = postExtensionRepository;
            _tagRepository = tagRepository;
            _postTagRepository = postTagRepository;
            _postPublishRepository = postPublishRepository;
            _categoryRepository = categoryRepository;
            _postCategoryRepository = postCategoryRepository;
            _htmlCodec = htmlCodec;
            _blogConfig = blogConfig;
            _dateTimeResolver = dateTimeResolver;
            _moongladeAudit = moongladeAudit;
        }

        public Response<int> CountVisiblePosts()
        {
            return TryExecute(() =>
            {
                var count = _postPublishRepository.Count(p => p.IsPublished && !p.IsDeleted);
                return new SuccessResponse<int>(count);
            });
        }

        public Response<int> CountByCategoryId(Guid catId)
        {
            return TryExecute(() =>
            {
                var count = _postCategoryRepository.Count(c => c.CategoryId == catId);
                return new SuccessResponse<int>(count);
            });
        }

        public Task<Response<IReadOnlyList<Archive>>> GetArchiveListAsync()
        {
            return TryExecuteAsync<IReadOnlyList<Archive>>(async () =>
            {
                if (!_postRepository.Any(p =>
                    p.PostPublish.IsPublished && !p.PostPublish.IsDeleted))
                    return new SuccessResponse<IReadOnlyList<Archive>>();

                var spec = new PostSpec(PostPublishStatus.Published);
                var list = await _postRepository.SelectAsync(spec, post => new
                {
                    post.PostPublish.PubDateUtc.Value.Year,
                    post.PostPublish.PubDateUtc.Value.Month
                }, monthList => new Archive(
                    monthList.Key.Year,
                    monthList.Key.Month,
                    monthList.Count()));

                return new SuccessResponse<IReadOnlyList<Archive>>(list);
            });
        }

        public Task<Response> UpdateStatisticAsync(Guid postId, StatisticTypes statisticTypes)
        {
            return TryExecuteAsync(async () =>
            {
                var pp = _postExtensionRepository.Get(postId);
                if (pp == null) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                switch (statisticTypes)
                {
                    case StatisticTypes.Hits:
                        pp.Hits += 1;
                        break;
                    case StatisticTypes.Likes:
                        pp.Likes += 1;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(statisticTypes), statisticTypes, null);
                }

                await _postExtensionRepository.UpdateAsync(pp);
                return new SuccessResponse();
            }, keyParameter: postId);
        }

        public Task<Response<Post>> GetPostAsync(Guid id)
        {
            return TryExecuteAsync<Post>(async () =>
            {
                var spec = new PostSpec(id);
                var post = await _postRepository.SelectFirstOrDefaultAsync(spec, p => new Post
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    RawPostContent = p.PostContent,
                    ContentAbstract = p.ContentAbstract,
                    CommentEnabled = p.CommentEnabled,
                    CreateOnUtc = p.CreateOnUtc,
                    PubDateUtc = p.PostPublish.PubDateUtc,
                    IsPublished = p.PostPublish.IsPublished,
                    ExposedToSiteMap = p.PostPublish.ExposedToSiteMap,
                    FeedIncluded = p.PostPublish.IsFeedIncluded,
                    ContentLanguageCode = p.PostPublish.ContentLanguageCode,
                    Tags = p.PostTag.Select(pt => new Tag
                    {
                        Id = pt.TagId,
                        NormalizedTagName = pt.Tag.NormalizedName,
                        TagName = pt.Tag.DisplayName
                    }).ToList(),
                    Categories = p.PostCategory.Select(pc => new Category
                    {
                        Id = pc.CategoryId,
                        DisplayName = pc.Category.DisplayName,
                        Name = pc.Category.Title,
                        Note = pc.Category.Note
                    }).ToList()
                });
                return new SuccessResponse<Post>(post);
            });
        }

        public Task<Response<PostSlugModel>> GetDraftPreviewAsync(Guid postId)
        {
            return TryExecuteAsync<PostSlugModel>(async () =>
            {
                var spec = new PostSpec(postId);
                var postSlugModel = await _postRepository.SelectFirstOrDefaultAsync(spec, post => new PostSlugModel
                {
                    Title = post.Title,
                    Abstract = post.ContentAbstract,
                    PubDateUtc = DateTime.UtcNow,

                    Categories = post.PostCategory.Select(pc => pc.Category).Select(p => new Category
                    {
                        DisplayName = p.DisplayName,
                        Name = p.Title
                    }).ToList(),

                    Content = _htmlCodec.HtmlDecode(post.PostContent),

                    Tags = post.PostTag.Select(pt => pt.Tag)
                        .Select(p => new Tag
                        {
                            NormalizedTagName = p.NormalizedName,
                            TagName = p.DisplayName
                        }).ToList(),
                    PostId = post.Id,
                    IsExposedToSiteMap = post.PostPublish.ExposedToSiteMap,
                    LastModifyOnUtc = post.PostPublish.LastModifiedUtc
                });

                if (null != postSlugModel)
                {
                    postSlugModel.Content = Utils.AddLazyLoadToImgTag(postSlugModel.Content);
                }

                return new SuccessResponse<PostSlugModel>(postSlugModel);
            });
        }

        public Task<Response<string>> GetRawContentAsync(int year, int month, int day, string slug)
        {
            return TryExecuteAsync<string>(async () =>
            {
                var date = new DateTime(year, month, day);
                var spec = new PostSpec(date, slug);

                var model = await _postRepository.SelectFirstOrDefaultAsync(spec,
                    post => _htmlCodec.HtmlDecode(post.PostContent));
                return new SuccessResponse<string>(model);
            });
        }

        public Task<Response<PostSlugMetaModel>> GetMetaAsync(int year, int month, int day, string slug)
        {
            return TryExecuteAsync<PostSlugMetaModel>(async () =>
            {
                var date = new DateTime(year, month, day);
                var spec = new PostSpec(date, slug);

                var model = await _postRepository.SelectFirstOrDefaultAsync(spec, post => new PostSlugMetaModel
                {
                    Title = post.Title,
                    PubDateUtc = post.PostPublish.PubDateUtc.GetValueOrDefault(),
                    LastModifyOnUtc = post.PostPublish.LastModifiedUtc,

                    Categories = post.PostCategory
                                     .Select(pc => pc.Category.DisplayName)
                                     .ToArray(),

                    Tags = post.PostTag
                               .Select(pt => pt.Tag.DisplayName)
                               .ToArray()
                });

                return new SuccessResponse<PostSlugMetaModel>(model);
            });
        }

        public Task<Response<PostSlugModel>> GetPostAsync(int year, int month, int day, string slug)
        {
            return TryExecuteAsync<PostSlugModel>(async () =>
            {
                var date = new DateTime(year, month, day);
                var spec = new PostSpec(date, slug);
                var postSlugModel = await _postRepository.SelectFirstOrDefaultAsync(spec, post => new PostSlugModel
                {
                    Title = post.Title,
                    Abstract = post.ContentAbstract,
                    PubDateUtc = post.PostPublish.PubDateUtc.GetValueOrDefault(),

                    Categories = post.PostCategory.Select(pc => pc.Category).Select(p => new Category
                    {
                        DisplayName = p.DisplayName,
                        Name = p.Title
                    }).ToList(),

                    Content = _htmlCodec.HtmlDecode(post.PostContent),
                    Hits = post.PostExtension.Hits,
                    Likes = post.PostExtension.Likes,

                    Tags = post.PostTag.Select(pt => pt.Tag)
                        .Select(p => new Tag
                        {
                            NormalizedTagName = p.NormalizedName,
                            TagName = p.DisplayName
                        }).ToList(),
                    PostId = post.Id,
                    CommentEnabled = post.CommentEnabled,
                    IsExposedToSiteMap = post.PostPublish.ExposedToSiteMap,
                    LastModifyOnUtc = post.PostPublish.LastModifiedUtc,
                    CommentCount = post.Comment.Count(c => c.IsApproved)
                });

                if (null != postSlugModel)
                {
                    postSlugModel.Content = Utils.AddLazyLoadToImgTag(postSlugModel.Content);
                }

                return new SuccessResponse<PostSlugModel>(postSlugModel);
            });
        }

        public Task<IReadOnlyList<PostMetaData>> GetMetaListAsync(PostPublishStatus postPublishStatus)
        {
            var spec = new PostSpec(postPublishStatus);
            return _postRepository.SelectAsync(spec, p => new PostMetaData
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                PubDateUtc = p.PostPublish.PubDateUtc,
                IsPublished = p.PostPublish.IsPublished,
                IsDeleted = p.PostPublish.IsDeleted,
                Revision = p.PostPublish.Revision,
                CreateOnUtc = p.CreateOnUtc,
                Hits = p.PostExtension.Hits
            });
        }

        public Task<IReadOnlyList<PostMetaData>> GetInsightsAsync(PostInsightsType insightsType)
        {
            var spec = new PostInsightsSpec(insightsType, 10);
            return _postRepository.SelectAsync(spec, p => new PostMetaData
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                PubDateUtc = p.PostPublish.PubDateUtc,
                IsPublished = p.PostPublish.IsPublished,
                IsDeleted = p.PostPublish.IsDeleted,
                Revision = p.PostPublish.Revision,
                CreateOnUtc = p.CreateOnUtc,
                Hits = p.PostExtension.Hits
            });
        }

        public Task<IReadOnlyList<PostListItem>> GetPagedPostsAsync(int pageSize, int pageIndex, Guid? categoryId = null)
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
            return _postRepository.SelectAsync(spec, p => new PostListItem
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                Tags = p.PostTag.Select(pt => new Tag
                {
                    NormalizedTagName = pt.Tag.NormalizedName,
                    TagName = pt.Tag.DisplayName
                }).ToList()
            });
        }

        public async Task<IReadOnlyList<PostListItem>> GetArchivedPostsAsync(int year, int month = 0)
        {
            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                Logger.LogError($"parameter '{nameof(year)}:{year}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            if (month > 12 || month < 0)
            {
                Logger.LogError($"parameter '{nameof(month)}:{month}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            var spec = new PostSpec(year, month);
            var list = await _postRepository.SelectAsync(spec, p => new PostListItem
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault()
            });
            return list;
        }

        public Task<Response<IReadOnlyList<PostListItem>>> GetPostsByTagAsync(int tagId)
        {
            return TryExecuteAsync<IReadOnlyList<PostListItem>>(async () =>
            {
                if (tagId == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(tagId));
                }

                var posts = await _postTagRepository.SelectAsync(new PostTagSpec(tagId),
                    p => new PostListItem
                    {
                        Title = p.Post.Title,
                        Slug = p.Post.Slug,
                        ContentAbstract = p.Post.ContentAbstract,
                        PubDateUtc = p.Post.PostPublish.PubDateUtc.GetValueOrDefault()
                    });

                return new SuccessResponse<IReadOnlyList<PostListItem>>(posts);
            });
        }

        public async Task<Response<PostEntity>> CreateNewPost(CreatePostRequest request)
        {
            return await TryExecuteAsync<PostEntity>(async () =>
            {
                var postModel = new PostEntity
                {
                    CommentEnabled = request.EnableComment,
                    Id = Guid.NewGuid(),
                    PostContent = AppSettings.Editor == EditorChoice.Markdown ?
                                    request.EditorContent :
                                    _htmlCodec.HtmlEncode(request.EditorContent),
                    ContentAbstract = Utils.GetPostAbstract(
                                            request.EditorContent,
                                            AppSettings.PostAbstractWords,
                                            AppSettings.Editor == EditorChoice.Markdown),
                    CreateOnUtc = DateTime.UtcNow,
                    Slug = request.Slug.ToLower().Trim(),
                    Title = request.Title.Trim(),
                    PostPublish = new PostPublishEntity
                    {
                        IsDeleted = false,
                        IsPublished = request.IsPublished,
                        PubDateUtc = request.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                        ExposedToSiteMap = request.ExposedToSiteMap,
                        IsFeedIncluded = request.IsFeedIncluded,
                        Revision = 0,
                        ContentLanguageCode = request.ContentLanguageCode,
                        PublisherIp = request.RequestIp
                    },
                    PostExtension = new PostExtensionEntity
                    {
                        Hits = 0,
                        Likes = 0
                    }
                };

                // check if exist same slug under the same day
                // linq to sql fix:
                // cannot write "p.PostPublish.PubDateUtc.GetValueOrDefault().Date == DateTime.UtcNow.Date"
                // it will not blow up, but can result in select ENTIRE posts and evaluated in memory!!!
                // - The LINQ expression 'where (Convert([p.PostPublish]?.PubDateUtc?.GetValueOrDefault(), DateTime).Date == DateTime.UtcNow.Date)' could not be translated and will be evaluated locally
                // Why EF Core this diao yang?
                if (_postRepository.Any(p =>
                    p.Slug == postModel.Slug &&
                    p.PostPublish.PubDateUtc != null &&
                    p.PostPublish.PubDateUtc.Value.Year == DateTime.UtcNow.Date.Year &&
                    p.PostPublish.PubDateUtc.Value.Month == DateTime.UtcNow.Date.Month &&
                    p.PostPublish.PubDateUtc.Value.Day == DateTime.UtcNow.Date.Day))
                {
                    var uid = Guid.NewGuid();
                    postModel.Slug += $"-{uid.ToString().ToLower().Substring(0, 8)}";
                    Logger.LogInformation($"Found conflict for post slug, generated new slug: {postModel.Slug}");
                }

                // add categories
                if (null != request.CategoryIds && request.CategoryIds.Length > 0)
                {
                    foreach (var cid in request.CategoryIds)
                    {
                        if (_categoryRepository.Any(c => c.Id == cid))
                        {
                            postModel.PostCategory.Add(new PostCategoryEntity
                            {
                                CategoryId = cid,
                                PostId = postModel.Id
                            });
                        }
                    }
                }

                // add tags
                if (null != request.Tags && request.Tags.Length > 0)
                {
                    foreach (var item in request.Tags)
                    {
                        if (!Utils.ValidateTagName(item))
                        {
                            continue;
                        }

                        var tag = _tagRepository.Get(q => q.DisplayName == item);
                        if (null == tag)
                        {
                            var newTag = new TagEntity
                            {
                                DisplayName = item,
                                NormalizedName = Utils.NormalizeTagName(item)
                            };

                            tag = _tagRepository.Add(newTag);
                            await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.TagCreated,
                                $"Tag '{tag.NormalizedName}' created.");
                        }

                        postModel.PostTag.Add(new PostTagEntity
                        {
                            TagId = tag.Id,
                            PostId = postModel.Id
                        });
                    }
                }

                await _postRepository.AddAsync(postModel);

                Logger.LogInformation($"New Post Created Successfully. PostId: {postModel.Id}");
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.PostCreated, $"Post created, id: {postModel.Id}");

                return new SuccessResponse<PostEntity>(postModel);
            });
        }

        public async Task<Response<PostEntity>> EditPost(EditPostRequest request)
        {
            return await TryExecuteAsync<PostEntity>(async () =>
            {
                var postModel = _postRepository.Get(request.Id);
                if (null == postModel)
                {
                    return new FailedResponse<PostEntity>((int)ResponseFailureCode.PostNotFound);
                }

                postModel.CommentEnabled = request.EnableComment;
                postModel.PostContent = AppSettings.Editor == EditorChoice.Markdown ?
                                        request.EditorContent :
                                        _htmlCodec.HtmlEncode(request.EditorContent);
                postModel.ContentAbstract = Utils.GetPostAbstract(
                                            request.EditorContent,
                                            AppSettings.PostAbstractWords,
                                            AppSettings.Editor == EditorChoice.Markdown);

                // Address #221: Do not allow published posts back to draft status
                // postModel.PostPublish.IsPublished = request.IsPublished;
                // Edit draft -> save and publish, ignore false case because #221
                bool isNewPublish = false;
                if (request.IsPublished && !postModel.PostPublish.IsPublished)
                {
                    postModel.PostPublish.IsPublished = true;
                    postModel.PostPublish.PublisherIp = request.RequestIp;
                    postModel.PostPublish.PubDateUtc = DateTime.UtcNow;

                    isNewPublish = true;
                }

                // #325: Allow changing publish date for published posts
                if (request.PublishDate != null && postModel.PostPublish.PubDateUtc.HasValue)
                {
                    var tod = postModel.PostPublish.PubDateUtc.Value.TimeOfDay;
                    var adjustedDate = _dateTimeResolver.GetUtcTimeFromUserTZone(request.PublishDate.Value);
                    postModel.PostPublish.PubDateUtc = adjustedDate.AddTicks(tod.Ticks);
                }

                postModel.Slug = request.Slug;
                postModel.Title = request.Title;
                postModel.PostPublish.ExposedToSiteMap = request.ExposedToSiteMap;
                postModel.PostPublish.LastModifiedUtc = DateTime.UtcNow;
                postModel.PostPublish.IsFeedIncluded = request.IsFeedIncluded;
                postModel.PostPublish.ContentLanguageCode = request.ContentLanguageCode;

                ++postModel.PostPublish.Revision;

                // 1. Add new tags to tag lib
                foreach (var item in request.Tags.Where(item => !_tagRepository.Any(p => p.DisplayName == item)))
                {
                    _tagRepository.Add(new TagEntity
                    {
                        DisplayName = item,
                        NormalizedName = Utils.NormalizeTagName(item)
                    });

                    await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.TagCreated,
                        $"Tag '{item}' created.");
                }

                // 2. update tags
                postModel.PostTag.Clear();
                if (request.Tags.Any())
                {
                    foreach (var tagName in request.Tags)
                    {
                        if (!Utils.ValidateTagName(tagName))
                        {
                            continue;
                        }

                        var tag = _tagRepository.Get(t => t.DisplayName == tagName);
                        if (tag != null) postModel.PostTag.Add(new PostTagEntity
                        {
                            PostId = postModel.Id,
                            TagId = tag.Id
                        });
                    }
                }

                // 3. update categories
                postModel.PostCategory.Clear();
                if (null != request.CategoryIds && request.CategoryIds.Length > 0)
                {
                    foreach (var cid in request.CategoryIds)
                    {
                        if (_categoryRepository.Any(c => c.Id == cid))
                        {
                            postModel.PostCategory.Add(new PostCategoryEntity
                            {
                                PostId = postModel.Id,
                                CategoryId = cid
                            });
                        }
                    }
                }

                await _postRepository.UpdateAsync(postModel);

                await _moongladeAudit.AddAuditEntry(
                    EventType.Content,
                    isNewPublish ? EventId.PostPublished : EventId.PostUpdated,
                    $"Post updated, id: {postModel.Id}");

                return new SuccessResponse<PostEntity>(postModel);
            });
        }

        public Task<Response> RestoreDeletedPostAsync(Guid postId)
        {
            return TryExecuteAsync(async () =>
            {
                var pp = await _postPublishRepository.GetAsync(postId);
                if (null == pp) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                pp.IsDeleted = false;
                await _postPublishRepository.UpdateAsync(pp);
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.PostRestored, $"Post restored, id: {postId}");

                return new SuccessResponse();
            }, keyParameter: postId);
        }

        public Task<Response> DeleteAsync(Guid postId, bool isRecycle = false)
        {
            return TryExecuteAsync(async () =>
            {
                var post = await _postRepository.GetAsync(postId);
                if (null == post) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                if (isRecycle)
                {
                    post.PostPublish.IsDeleted = true;
                    await _postRepository.UpdateAsync(post);
                    await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.PostRecycled, $"Post '{postId}' moved to Recycle Bin.");
                }
                else
                {
                    await _postRepository.DeleteAsync(post);
                    await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.PostDeleted, $"Post '{postId}' deleted from Recycle Bin.");
                }

                return new SuccessResponse();
            }, keyParameter: postId);
        }

        public Task<Response> DeleteRecycledPostsAsync()
        {
            return TryExecuteAsync(async () =>
            {
                var spec = new PostSpec(true);
                var posts = await _postRepository.GetAsync(spec);
                await _postRepository.DeleteAsync(posts);
                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.EmptyRecycleBin, "Emptied Recycle Bin.");

                return new SuccessResponse();
            });
        }
    }
}