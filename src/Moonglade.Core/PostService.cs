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
using Moonglade.Data;
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

        public enum StatisticType
        {
            Hits,
            Likes
        }

        public PostService(MoongladeDbContext context,
            ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<Post> postRepository,
            IRepository<PostExtension> postExtensionRepository,
            IRepository<Tag> tagRepository,
            IRepository<PostPublish> postPublishRepository,
            IRepository<Category> categoryRepository) : base(context, logger, settings)
        {
            _postRepository = postRepository;
            _postExtensionRepository = postExtensionRepository;
            _tagRepository = tagRepository;
            _postPublishRepository = postPublishRepository;
            _categoryRepository = categoryRepository;
        }

        public int CountForPublic => _postRepository.Count(p => p.PostPublish.IsPublished &&
                                                          !p.PostPublish.IsDeleted);

        public Response UpdatePostStatistic(Guid postId, StatisticType statisticType)
        {
            try
            {
                var pp = _postExtensionRepository.Get(postId);
                if (pp == null) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                if (statisticType == StatisticType.Hits)
                {
                    pp.Hits += 1;
                }
                if (statisticType == StatisticType.Likes)
                {
                    pp.Likes += 1;
                }

                int rows = _postExtensionRepository.Update(pp);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(UpdatePostStatistic)}(postId: {postId}, statisticType: {statisticType})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<Post> GetPost(Guid id)
        {
            try
            {
                var post = Context.Post.Include(p => p.PostPublish)
                                       .Include(p => p.PostTag)
                                       .ThenInclude(pt => pt.Tag)
                                       .Include(p => p.PostCategory)
                                       .ThenInclude(pc => pc.Category)
                                       .FirstOrDefault(p => p.Id == id);

                return new SuccessResponse<Post>(post);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetPost)}(id: {id})");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<(Guid Id, string Title)> GetPostIdTitle(string url)
        {
            try
            {
                // https://domain/post/yyyy/MM/dd/slug
                var blogSlugRegex = new Regex(@"^https?:\/\/.*\/post\/(?<yyyy>\d{4})\/(?<MM>\d{1,12})\/(?<dd>\d{1,31})\/(?<slug>.*)$");
                var match = blogSlugRegex.Match(url);
                if (!match.Success)
                {
                    return null;
                }

                var year = int.Parse(match.Groups["yyyy"].Value);
                var month = int.Parse(match.Groups["MM"].Value);
                var day = int.Parse(match.Groups["dd"].Value);
                var slug = match.Groups["slug"].Value;

                var date = new DateTime(year, month, day);

                var post = _postRepository.Get(p => p.Slug == slug &&
                                               p.PostPublish.PubDateUtc.GetValueOrDefault().Date == date.Date &&
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

        public Response<Post> GetPost(int year, int month, int day, string slug)
        {
            try
            {
                
                var date = new DateTime(year, month, day);
                var spec = new GetPostSpec(date, slug);
                var post = _postRepository.GetFirstOrDefault(spec);

                return new SuccessResponse<Post>(post);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error {nameof(GetPost)}(year: {year}, month: {month}, day: {day}, slug: {slug})");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, ex.Message, ex);
            }
        }

        public IReadOnlyList<PostMetaData> GetPostMetaList(bool isDeleted = false, bool? isPublished = true)
        {
            var spec = null != isPublished ? new GetPostMetaListSpec(isDeleted, isPublished.Value) : new GetPostMetaListSpec();
            var posts = _postRepository.Select(spec, p => new PostMetaData
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
            return posts;
        }

        public IQueryable<Post> GetPagedPosts(int pageSize, int pageIndex, Guid? categoryId = null)
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

            var startRow = (pageIndex - 1) * pageSize;
            var query = Context.Post.Where(p => !p.PostPublish.IsDeleted &&
                                           p.PostPublish.IsPublished &&
                                           (categoryId == null || p.PostCategory.Select(c => c.CategoryId).Contains(categoryId.Value)))
                                    .Include(p => p.PostPublish)
                                    .Include(p => p.PostExtension)
                                    .Include(p => p.PostTag)
                                       .ThenInclude(pt => pt.Tag)
                                    .OrderByDescending(p => p.PostPublish.PubDateUtc)
                                    .Skip(startRow)
                                    .Take(pageSize).AsNoTracking();

            return query;
        }

        public async Task<IReadOnlyList<PostArchiveItemModel>> GetArchivedPosts(int year, int month = 0)
        {
            var spec = new ArchivedPostSpec(year, month);
            var list = await _postRepository.SelectAsync(spec, p => new PostArchiveItemModel
            {
                PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                Slug = p.Slug,
                Title = p.Title
            });
            return list;
        }

        public Response<IEnumerable<Post>> GetPostsByTag(string normalizedName)
        {
            try
            {
                var posts = Context.PostTag
                                   .Where(pt => pt.Tag.NormalizedName == normalizedName)
                                   .Select(pt => pt.Post)
                                   .Include(p => p.PostPublish).AsNoTracking();

                return new SuccessResponse<IEnumerable<Post>>(posts);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetPostsByTag)}(normalizedName: {normalizedName})");
                return new FailedResponse<IEnumerable<Post>>((int)ResponseFailureCode.GeneralException);
            }
        }

        #region Search

        public Response<IEnumerable<SearchResult>> SearchPost(string keyword)
        {
            var postList = SearchPostByKeyword(keyword);

            var resultList = postList.Select(p => p.PostPublish.PubDateUtc != null ? new SearchResult
            {
                Slug = p.Slug,
                PubDateUtc = p.PostPublish.PubDateUtc.GetValueOrDefault(),
                Summary = p.ContentAbstract,
                Title = p.Title
            } : null);

            return new SuccessResponse<IEnumerable<SearchResult>>(resultList);
        }

        private IEnumerable<Post> SearchPostByKeyword(string keyword)
        {
            var query = Context.Post.Include(p => p.PostPublish)
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
            return _postRepository.SelectFirstOrDefault(new PostSpec(postId), p => p.Title);
        }

        public Response<Post> CreateNewPost(CreateEditPostRequest request)
        {
            try
            {
                var postModel = new Post
                {
                    CommentEnabled = request.EnableComment,
                    Id = request.PostId,
                    PostContent = HttpUtility.HtmlEncode(request.HtmlContent),
                    ContentAbstract = Utils.GetPostAbstract(request.HtmlContent, AppSettings.PostSummaryWords),
                    CreateOnUtc = DateTime.UtcNow,
                    Slug = request.Slug.Trim(),
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
                    postModel.Slug += $"-{uid.ToString().Substring(0, 8)}";
                    Logger.LogInformation($"Found conflict for post slug, generated new slug: {postModel.Slug}");
                }

                // add categories
                if (null != request.CategoryIds && request.CategoryIds.Count > 0)
                {
                    request.CategoryIds.ForEach(cid =>
                    {
                        if (_categoryRepository.Any(c => c.Id == cid))
                        {
                            postModel.PostCategory.Add(new PostCategory
                            {
                                CategoryId = cid,
                                PostId = postModel.Id
                            });
                        }
                    });
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
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error in {nameof(CreateNewPost)}");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException);
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
                foreach (var item in request.Tags.Where(item => !Context.Tag.Any(p => p.DisplayName == item)))
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
                    request.Tags.ForEach(t =>
                    {
                        var tag = _tagRepository.Get(_ => _.DisplayName == t);
                        if (tag != null) postModel.PostTag.Add(new PostTag
                        {
                            PostId = postModel.Id,
                            TagId = tag.Id
                        });
                    });
                }

                // 3. update categories
                postModel.PostCategory.Clear();
                if (null != request.CategoryIds && request.CategoryIds.Count > 0)
                {
                    request.CategoryIds.ForEach(cid =>
                    {
                        if (_categoryRepository.Any(c => c.Id == cid))
                        {
                            postModel.PostCategory.Add(new PostCategory
                            {
                                PostId = postModel.Id,
                                CategoryId = cid
                            });
                        }
                    });
                }

                _postRepository.Update(postModel);
                return new SuccessResponse<Post>(postModel);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error Editing Post, PostId: {request.PostId}");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException);
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
                Logger.LogError(e, $"Error {nameof(RestoreFromRecycle)}");
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
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        private static void ApplyDefaultValuesOnPost(Post postModel)
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
    }
}