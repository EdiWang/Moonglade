using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Edi.Practice.RequestResponseModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class PostService : MoongladeService
    {
        public enum StatisticType
        {
            Hits,
            Likes
        }

        public PostService(MoongladeDbContext context, ILogger<PostService> logger) : base(context, logger)
        {
        }

        public int CountForPublic => Context.Post.Count(p => p.PostPublish.IsPublished &&
                                                               !p.PostPublish.IsDeleted);

        public Response UpdatePostStatistic(Guid postId, StatisticType statisticType)
        {
            try
            {
                var pp = Context.PostExtension.FirstOrDefault(pe => pe.PostId == postId);
                if (pp == null) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                if (statisticType == StatisticType.Hits)
                {
                    pp.Hits += 1;
                }
                if (statisticType == StatisticType.Likes)
                {
                    pp.Likes += 1;
                }

                var rows = Context.SaveChanges();
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error UpdatePostHit({postId})");
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

        public Response<Post> GetPost(string url)
        {
            try
            {
                // https://domain/post/yyyy/MM/dd/slug

                var uri = new Uri(url);
                if (uri.Segments.Length < 5)
                {
                    return null;
                }

                var yyyy = Convert.ToInt32(uri.Segments[2].Replace("/", string.Empty));
                var mm = Convert.ToInt32(uri.Segments[3].Replace("/", string.Empty));
                var dd = Convert.ToInt32(uri.Segments[4].Replace("/", string.Empty));
                var slug = uri.Segments[5];

                var post = GetPost(yyyy, mm, dd, slug.Trim());
                return post;
            }
            catch (Exception ex)
            {
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, ex.Message, ex);
            }
        }

        public Response<Post> GetPost(int year, int month, int day, string slug)
        {
            try
            {
                var post = Context.Post.Include(p => p.PostPublish)
                                   .Include(p => p.PostExtension)
                                   .Include(p => p.Comment)
                                   .Include(p => p.PostTag).ThenInclude(pt => pt.Tag)
                                   .Include(p => p.PostCategory).ThenInclude(pc => pc.Category)
                                   .FirstOrDefault(p => p.Slug == slug &&
                                                   p.PostPublish.PubDateUtc.Value.Year == year &&
                                                   p.PostPublish.PubDateUtc.Value.Month == month &&
                                                   p.PostPublish.PubDateUtc.Value.Day == day &&
                                                   p.PostPublish.IsPublished &&
                                                   !p.PostPublish.IsDeleted);

                return new SuccessResponse<Post>(post);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error {nameof(GetPost)}(year: {year}, month: {month}, day: {day}, slug: {slug})");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException, ex.Message, ex);
            }
        }

        public IQueryable<Post> GetPosts()
        {
            return Context.Post.Include(p => p.PostPublish).Include(p => p.PostExtension);
        }

        public IQueryable<Post> GetPagedPosts(int pageSize, int pageIndex, Guid? categoryId = null)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize can not be less than 1.");
            }
            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "pageIndex can not be less than 1.");
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

            var list = query;
            return list;
        }

        public IQueryable<Post> GetArchivedPosts(int year, int month = 0)
        {
            Logger.LogInformation($"Querying archived posts for {year}/{month}");

            var query = Context.Post.Include(p => p.PostPublish)
                                    .Where(p => p.PostPublish.PubDateUtc.Value.Year == year &&
                                          (month == 0 || p.PostPublish.PubDateUtc.Value.Month == month)).AsNoTracking();

            return query;
        }

        public Response<IEnumerable<Post>> GetPostsByTag(string normalizedName)
        {
            try
            {
                Logger.LogInformation($"Querying tagged posts for {normalizedName}");

                var posts = Context.PostTag //.Include(pt => pt.Post)
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
            var query = Context.Post.Where(p => p.Id == postId).Select(p => p.Title).FirstOrDefault();
            return query;
        }

        public Response<Post> CreateNewPost(Post postModel, List<string> tags, List<Guid> categoryIds)
        {
            try
            {
                // check required fields
                if (string.IsNullOrWhiteSpace(postModel.Title))
                {
                    throw new ArgumentNullException(nameof(postModel.Title));
                }
                if (string.IsNullOrWhiteSpace(postModel.PostContent))
                {
                    throw new ArgumentNullException(nameof(postModel.PostContent));
                }

                // add default values if fields are not assigned
                ApplyDefaultValuesOnPost(postModel);

                // check if exist same slug under the same day
                // linq to sql fix:
                // cannot write "p.PubDateUTC.Date == DateTime.Now.Date"
                // it will blow up "The specified type member 'Date' is not supported in LINQ to Entities"
                var today = DateTime.UtcNow.Date;
                if (null != Context.Post.FirstOrDefault(p =>
                           p.Slug == postModel.Slug &&
                           p.PostPublish.PubDateUtc.Value.Year == today.Year &&
                           p.PostPublish.PubDateUtc.Value.Month == today.Month &&
                           p.PostPublish.PubDateUtc.Value.Day == today.Day))
                {
                    postModel.Slug += "-1";
                }

                // add categories
                if (null != categoryIds && categoryIds.Count > 0)
                {
                    categoryIds.ForEach(cid =>
                    {
                        var cat = Context.Category.Find(cid);
                        if (null != cat)
                        {
                            postModel.PostCategory.Add(new PostCategory
                            {
                                CategoryId = cat.Id,
                                PostId = postModel.Id
                            });
                        }
                    });
                }

                // add tags
                if (null != tags && tags.Count > 0)
                {
                    var tagsList = new List<Tag>();
                    foreach (var item in tags)
                    {
                        var tag = Context.Tag.FirstOrDefault(q => q.DisplayName == item);
                        if (null == tag)
                        {
                            // for new tags
                            var newTag = new Tag
                            {
                                DisplayName = item,
                                NormalizedName = Utils.NormalizeTagName(item)
                            };

                            tagsList.Add(newTag);
                            Context.Tag.Add(newTag);
                            Context.SaveChanges();
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

                Context.Post.Add(postModel);
                var rows = Context.SaveChanges();
                if (rows <= 0) return new FailedResponse<Post>((int)ResponseFailureCode.DataOperationFailed);

                Logger.LogInformation($"New Post Created Successfully. PostId: {postModel.Id}");
                return new SuccessResponse<Post>(postModel);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in CreateNewPost()");
                return new FailedResponse<Post>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response EditPost(Post postModel, List<string> tags, List<Guid> categoryIds)
        {
            try
            {
                if (!postModel.PostPublish.LastModifiedUtc.HasValue)
                {
                    postModel.PostPublish.LastModifiedUtc = DateTime.UtcNow;
                }

                ++postModel.PostPublish.Revision;

                // from draft
                if (!postModel.PostPublish.PubDateUtc.HasValue)
                {
                    postModel.PostPublish.PubDateUtc = DateTime.UtcNow;
                }

                // 1. Add new tags to tag lib
                foreach (var item in tags.Where(item => !Context.Tag.Any(p => p.DisplayName == item)))
                {
                    Context.Tag.Add(new Tag
                    {
                        DisplayName = item,
                        NormalizedName = Utils.NormalizeTagName(item)
                    });
                }
                Context.SaveChanges();

                // 2. update tags
                postModel.PostTag.Clear();
                if (tags.Any())
                {
                    tags.ForEach(t =>
                    {
                        var tag = Context.Tag.FirstOrDefault(_ => _.DisplayName == t);
                        if (tag != null) postModel.PostTag.Add(new PostTag
                        {
                            PostId = postModel.Id,
                            TagId = tag.Id
                        });
                    });
                }

                // 3. update categories
                postModel.PostCategory.Clear();
                if (null != categoryIds && categoryIds.Count > 0)
                {
                    categoryIds.ForEach(cid =>
                    {
                        var cat = Context.Category.Find(cid);
                        if (null != cat)
                        {
                            postModel.PostCategory.Add(new PostCategory
                            {
                                PostId = postModel.Id,
                                CategoryId = cat.Id
                            });
                        }
                    });
                }

                Context.SaveChanges();
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Eroor Editing Post Id: {postModel.Id}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response RestoreFromRecycle(Guid postId)
        {
            try
            {
                var post = Context.Post.Find(postId);
                if (null == post) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                post.PostPublish.IsDeleted = false;
                var rows = Context.SaveChanges();
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
                var post = Context.Post.Find(postId);
                if (null == post) return new FailedResponse((int)ResponseFailureCode.PostNotFound);

                int rows;

                if (isRecycle)
                {
                    post.PostPublish.IsDeleted = true;
                    rows = Context.SaveChanges();
                }
                else
                {
                    Context.Post.Remove(post);
                    rows = Context.SaveChanges();
                }

                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error Delete(postId: {postId}, isRecycle: {isRecycle})");
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