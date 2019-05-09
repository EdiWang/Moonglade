using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Edi.Practice.RequestResponseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class PostService : MoongladeService
    {
        private readonly IRepository<Post> _postRepository;

        private readonly IRepository<PostExtension> _postExtensionRepository;

        private readonly IRepository<PostPublish> _postPublishRepository;

        private readonly IRepository<Tag> _tagRepository;

        private readonly IRepository<Category> _categoryRepository;

        public PostService(ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<Post> postRepository,
            IRepository<PostExtension> postExtensionRepository,
            IRepository<Tag> tagRepository,
            IRepository<PostPublish> postPublishRepository,
            IRepository<Category> categoryRepository) : base(logger, settings)
        {
            _postRepository = postRepository;
            _postExtensionRepository = postExtensionRepository;
            _tagRepository = tagRepository;
            _postPublishRepository = postPublishRepository;
            _categoryRepository = categoryRepository;
        }

        public int CountForPublic => _postPublishRepository.Count(p => p.IsPublished && !p.IsDeleted);

        public async Task<Response> UpdatePostStatisticAsync(Guid postId, StatisticTypes statisticTypes)
        {
            try
            {
                var pp = _postExtensionRepository.Get(postId);
                if (pp == null) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                if (statisticTypes == StatisticTypes.Hits)
                {
                    pp.Hits += 1;
                }
                if (statisticTypes == StatisticTypes.Likes)
                {
                    pp.Likes += 1;
                }

                await _postExtensionRepository.UpdateAsync(pp);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(UpdatePostStatisticAsync)}(postId: {postId}, statisticTypes: {statisticTypes})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public Response<Post> GetPost(Guid id)
        {
            try
            {
                var spec = new GetPostSpec(id);
                var post = _postRepository.GetFirstOrDefault(spec);
                return new SuccessResponse<Post>(post);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetPost)}(id: {id})");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public Response<(Guid Id, string Title)> GetPostIdTitle(string url)
        {
            try
            {
                var response = Utils.GetSlugInfoFromPostUrl(url);
                if (!response.IsSuccess)
                {
                    return null;
                }

                var post = _postRepository.Get(p => p.Slug == response.Item.Slug &&
                                               p.PostPublish.PubDateUtc.GetValueOrDefault().Date == response.Item.PubDate.Date &&
                                               p.PostPublish.IsPublished &&
                                               !p.PostPublish.IsDeleted);

                return null == post ?
                    new FailedResponse<(Guid, string)>((int)ResponseFailureCode.PostNotFound) :
                    new Response<(Guid, string)>((post.Id, post.Title));
            }
            catch (Exception ex)
            {
                return new FailedResponse<(Guid, string)>((int)ResponseFailureCode.GeneralException, ex.Message, ex);
            }
        }

        public async Task<Response<Post>> GetPostAsync(int year, int month, int day, string slug)
        {
            try
            {
                var date = new DateTime(year, month, day);
                var spec = new GetPostSpec(date, slug);
                var post = await _postRepository.GetFirstOrDefaultAsync(spec, false);

                return new SuccessResponse<Post>(post);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error {nameof(GetPost)}(year: {year}, month: {month}, day: {day}, slug: {slug})");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, ex.Message, ex);
            }
        }

        public Task<IReadOnlyList<PostMetaData>> GetPostMetaListAsync(bool isDeleted = false, bool? isPublished = true)
        {
            var spec = null != isPublished ? new GetPostSpec(isDeleted, isPublished.Value) : new GetPostSpec();
            return _postRepository.SelectAsync(spec, p => new PostMetaData
            {
                Id = p.Id,
                Title = p.Title,
                PubDateUtc = p.PostPublish.PubDateUtc,
                IsPublished = p.PostPublish.IsPublished,
                IsDeleted = p.PostPublish.IsDeleted,
                Revision = p.PostPublish.Revision,
                CreateOnUtc = p.CreateOnUtc.Value,
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

            var spec = new GetPostSpec(pageSize, pageIndex, categoryId);
            return _postRepository.SelectAsync(spec, p => new PostListItem
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                Tags = p.PostTag.Select(pt => new TagInfo
                {
                    NormalizedTagName = pt.Tag.NormalizedName,
                    TagName = pt.Tag.DisplayName
                }).ToList()
            });
        }

        public async Task<IReadOnlyList<PostArchiveItem>> GetArchivedPostsAsync(int year, int month = 0)
        {
            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            if (month > 12 || month < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            var spec = new GetPostSpec(year, month);
            var list = await _postRepository.SelectAsync(spec, p => new PostArchiveItem
            {
                PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                Slug = p.Slug,
                Title = p.Title
            });
            return list;
        }

        public async Task<Response<IReadOnlyList<Post>>> GetPostsByTagAsync(string normalizedName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(normalizedName))
                {
                    throw new ArgumentNullException(nameof(normalizedName));
                }

                var posts = await _tagRepository.GetAsQueryable()
                                                .Where(t => t.NormalizedName == normalizedName)
                                                .SelectMany(p => p.PostTag)
                                                .Select(p => p.Post)
                                                .Include(p => p.PostPublish).ToListAsync();

                return new SuccessResponse<IReadOnlyList<Post>>(posts);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetPostsByTagAsync)}(normalizedName: {normalizedName})");
                return new FailedResponse<IReadOnlyList<Post>>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        #region Search

        public async Task<Response<IReadOnlyList<SearchResult>>> SearchPostAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    throw new ArgumentNullException(keyword);
                }

                var postList = SearchPostByKeyword(keyword);

                var resultList = await postList.Select(p => p.PostPublish.PubDateUtc != null ? new SearchResult
                {
                    Slug = p.Slug,
                    PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                    Summary = p.ContentAbstract,
                    Title = p.Title
                } : null).ToListAsync();

                return new SuccessResponse<IReadOnlyList<SearchResult>>(resultList);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(SearchPostAsync)}(keyword: {keyword})");
                return new FailedResponse<IReadOnlyList<SearchResult>>((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        private IQueryable<Post> SearchPostByKeyword(string keyword)
        {
            var query = _postRepository.GetAsQueryable()
                                       .Include(p => p.PostPublish)
                                       .Include(p => p.PostTag)
                                       .ThenInclude(pt => pt.Tag)
                                       .Where(p => !p.PostPublish.IsDeleted && p.PostPublish.IsPublished).AsNoTracking();

            var str = Regex.Replace(keyword, @"\s+", " ");
            var rst = str.Split(' ');
            if (rst.Length > 1)
            {
                // keyword: "dot  net rocks"
                // search for post where Title containing "dot && net && rocks"
                var result = rst.Aggregate(query, (current, s) => current.Where(p => p.Title.Contains(s)));
                return result;
            }
            else
            {
                // keyword: "dotnetrocks"
                var k = rst.First();
                var result = query.Where(p => p.Title.Contains(k) ||
                                              p.PostTag.Select(pt => pt.Tag).Select(t => t.DisplayName).Contains(k));
                return result;
            }
        }

        #endregion

        public string GetPostTitle(Guid postId)
        {
            var spec = new GetPostSpec(postId, false);
            return _postRepository.SelectFirstOrDefault(spec, p => p.Title);
        }

        public Response<Post> CreateNewPost(CreateEditPostRequest request)
        {
            void ApplyDefaultValuesOnPost(Post postModel)
            {
                if (postModel.Id == Guid.Empty)
                {
                    postModel.Id = Guid.NewGuid();
                }
                if (string.IsNullOrWhiteSpace(postModel.Slug))
                {
                    postModel.Slug = postModel.Id.ToString();
                }

                if (null == postModel.PostExtension)
                {
                    postModel.PostExtension = new PostExtension
                    {
                        Hits = 0,
                        Likes = 0
                    };
                }
            }

            try
            {
                var postModel = new Post
                {
                    CommentEnabled = request.EnableComment,
                    Id = request.PostId,
                    PostContent = HttpUtility.HtmlEncode(request.HtmlContent),
                    ContentAbstract = Utils.GetPostAbstract(request.HtmlContent, AppSettings.PostSummaryWords),
                    CreateOnUtc = DateTime.UtcNow,
                    Slug = request.Slug.ToLower().Trim(),
                    Title = request.Title.Trim(),
                    PostPublish = new PostPublish
                    {
                        IsDeleted = false,
                        IsPublished = request.IsPublished,
                        PubDateUtc = request.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                        ExposedToSiteMap = request.ExposedToSiteMap,
                        IsFeedIncluded = request.IsFeedIncluded,
                        Revision = 0,
                        ContentLanguageCode = request.ContentLanguageCode
                    }
                };

                // add default values if fields are not assigned
                ApplyDefaultValuesOnPost(postModel);

                // check if exist same slug under the same day
                if (_postRepository.Any(p =>
                    p.Slug == postModel.Slug &&
                    p.PostPublish.PubDateUtc.GetValueOrDefault().Date == DateTime.UtcNow.Date))
                {
                    var uid = Guid.NewGuid();
                    postModel.Slug += $"-{uid.ToString().ToLower().Substring(0, 8)}";
                    Logger.LogInformation($"Found conflict for post slug, generated new slug: {postModel.Slug}");
                }

                // add categories
                if (null != request.CategoryIds && request.CategoryIds.Count > 0)
                {
                    foreach (var cid in request.CategoryIds)
                    {
                        if (_categoryRepository.Any(c => c.Id == cid))
                        {
                            postModel.PostCategory.Add(new PostCategory
                            {
                                CategoryId = cid,
                                PostId = postModel.Id
                            });
                        }
                    }
                }

                // add tags
                if (null != request.Tags && request.Tags.Count > 0)
                {
                    var tagsList = new List<Tag>();
                    foreach (var item in request.Tags)
                    {
                        var tag = _tagRepository.Get(q => q.DisplayName == item);
                        if (null == tag)
                        {
                            // for new tags
                            var newTag = new Tag
                            {
                                DisplayName = item,
                                NormalizedName = Utils.NormalizeTagName(item)
                            };

                            tagsList.Add(newTag);
                            _tagRepository.Add(newTag);
                        }
                        else
                        {
                            // existing tags
                            tagsList.Add(tag);
                        }
                    }

                    tagsList.ForEach(t => postModel.PostTag.Add(new PostTag
                    {
                        TagId = t.Id,
                        PostId = postModel.Id
                    }));
                }

                _postRepository.Add(postModel);
                Logger.LogInformation($"New Post Created Successfully. PostId: {postModel.Id}");
                return new SuccessResponse<Post>(postModel);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error in {nameof(CreateNewPost)}");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public Response<Post> EditPost(CreateEditPostRequest request)
        {
            try
            {
                var postModel = _postRepository.Get(request.PostId);
                if (null == postModel)
                {
                    return new FailedResponse<Post>((int)ResponseFailureCode.PostNotFound);
                }

                postModel.CommentEnabled = request.EnableComment;
                postModel.PostContent = HttpUtility.HtmlEncode(request.HtmlContent);
                postModel.ContentAbstract = Utils.GetPostAbstract(request.HtmlContent, AppSettings.PostSummaryWords);
                postModel.PostPublish.IsPublished = request.IsPublished;
                postModel.Slug = request.Slug;
                postModel.Title = request.Title;
                postModel.PostPublish.ExposedToSiteMap = request.ExposedToSiteMap;
                postModel.PostPublish.LastModifiedUtc = DateTime.UtcNow;
                postModel.PostPublish.IsFeedIncluded = request.IsFeedIncluded;
                postModel.PostPublish.ContentLanguageCode = request.ContentLanguageCode;

                ++postModel.PostPublish.Revision;

                // from draft
                if (!postModel.PostPublish.PubDateUtc.HasValue)
                {
                    postModel.PostPublish.PubDateUtc = DateTime.UtcNow;
                }

                // 1. Add new tags to tag lib
                foreach (var item in request.Tags.Where(item => !_tagRepository.Any(p => p.DisplayName == item)))
                {
                    _tagRepository.Add(new Tag
                    {
                        DisplayName = item,
                        NormalizedName = Utils.NormalizeTagName(item)
                    });
                }

                // 2. update tags
                postModel.PostTag.Clear();
                if (request.Tags.Any())
                {
                    foreach (var t in request.Tags)
                    {
                        var tag = _tagRepository.Get(_ => _.DisplayName == t);
                        if (tag != null) postModel.PostTag.Add(new PostTag
                        {
                            PostId = postModel.Id,
                            TagId = tag.Id
                        });
                    }
                }

                // 3. update categories
                postModel.PostCategory.Clear();
                if (null != request.CategoryIds && request.CategoryIds.Count > 0)
                {
                    foreach (var cid in request.CategoryIds)
                    {
                        if (_categoryRepository.Any(c => c.Id == cid))
                        {
                            postModel.PostCategory.Add(new PostCategory
                            {
                                PostId = postModel.Id,
                                CategoryId = cid
                            });
                        }
                    }
                }

                _postRepository.Update(postModel);
                return new SuccessResponse<Post>(postModel);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(EditPost)}, PostId: {request.PostId}");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public Response RestoreFromRecycle(Guid postId)
        {
            try
            {
                var pp = _postPublishRepository.Get(postId);
                if (null == pp) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                pp.IsDeleted = false;
                var rows = _postPublishRepository.Update(pp);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(RestoreFromRecycle)}(postId: {postId})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response Delete(Guid postId, bool isRecycle = false)
        {
            try
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
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Delete)}(postId: {postId}, isRecycle: {isRecycle})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }

        public async Task<Response> DeleteRecycledPostsAsync()
        {
            try
            {
                var spec = new GetPostSpec(true);
                var posts = await _postRepository.GetAsync(spec);
                await _postRepository.DeleteAsync(posts);

                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(DeleteRecycledPostsAsync)}()");
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message);
            }
        }
    }
}