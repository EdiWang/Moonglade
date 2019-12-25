using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.HtmlCodec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class PostService : MoongladeService
    {
        private readonly IHtmlCodec _htmlCodec;
        private readonly IBlogConfig _blogConfig;

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
            IBlogConfig blogConfig) : base(logger, settings)
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

        public Task<Response> UpdatePostStatisticAsync(Guid postId, StatisticTypes statisticTypes)
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

                if (null != postSlugModel && _blogConfig.ContentSettings.EnableImageLazyLoad)
                {
                    postSlugModel.Content = Utils.ReplaceImgSrc(postSlugModel.Content);
                }

                return new SuccessResponse<PostSlugModel>(postSlugModel);
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

                if (null != postSlugModel && _blogConfig.ContentSettings.EnableImageLazyLoad)
                {
                    postSlugModel.Content = Utils.ReplaceImgSrc(postSlugModel.Content);
                }

                return new SuccessResponse<PostSlugModel>(postSlugModel);
            });
        }

        public Task<IReadOnlyList<PostMetaData>> GetPostMetaListAsync(PostPublishStatus postPublishStatus)
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

        public Task<IReadOnlyList<PostMetaData>> GetMPostInsightsMetaListAsync(PostInsightsType insightsType)
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

        public Response<PostEntity> CreateNewPost(CreatePostRequest request)
        {
            return TryExecute(() =>
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
                                            AppSettings.PostSummaryWords, 
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
                        }

                        postModel.PostTag.Add(new PostTagEntity
                        {
                            TagId = tag.Id,
                            PostId = postModel.Id
                        });
                    }
                }

                _postRepository.Add(postModel);
                Logger.LogInformation($"New Post Created Successfully. PostId: {postModel.Id}");
                return new SuccessResponse<PostEntity>(postModel);
            });
        }

        public Response<PostEntity> EditPost(EditPostRequest request)
        {
            return TryExecute<PostEntity>(() =>
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
                                            AppSettings.PostSummaryWords, 
                                            AppSettings.Editor == EditorChoice.Markdown);

                // Address #221: Do not allow published posts back to draft status
                // postModel.PostPublish.IsPublished = request.IsPublished;
                // Edit draft -> save and publish, ignore false case because #221
                if (request.IsPublished && !postModel.PostPublish.IsPublished)
                {
                    postModel.PostPublish.IsPublished = true;
                    postModel.PostPublish.PublisherIp = request.RequestIp;
                    postModel.PostPublish.PubDateUtc = DateTime.UtcNow;
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

                _postRepository.Update(postModel);
                return new SuccessResponse<PostEntity>(postModel);
            });
        }

        public Response RestoreDeletedPost(Guid postId)
        {
            return TryExecute(() =>
            {
                var pp = _postPublishRepository.Get(postId);
                if (null == pp) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                pp.IsDeleted = false;
                var rows = _postPublishRepository.Update(pp);
                return new Response(rows > 0);
            }, keyParameter: postId);
        }

        public Response Delete(Guid postId, bool isRecycle = false)
        {
            return TryExecute(() =>
            {
                var post = _postRepository.Get(postId);
                if (null == post) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                int rows;
                if (isRecycle)
                {
                    post.PostPublish.IsDeleted = true;
                    rows = _postRepository.Update(post);
                }
                else
                {
                    rows = _postRepository.Delete(post);
                }

                return new Response(rows > 0);
            }, keyParameter: postId);
        }

        public Task<Response> DeleteRecycledPostsAsync()
        {
            return TryExecuteAsync(async () =>
            {
                var spec = new PostSpec(true);
                var posts = await _postRepository.GetAsync(spec);
                await _postRepository.DeleteAsync(posts);

                return new SuccessResponse();
            });
        }
    }
}