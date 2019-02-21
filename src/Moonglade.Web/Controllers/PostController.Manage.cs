using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    public partial class PostController
    {
        #region Management

        [Authorize]
        [Route("manage")]
        public IActionResult Manage()
        {
            var query = _postService.GetPostsAsQueryable()
                .Where(p => !p.PostPublish.IsDeleted && p.PostPublish.IsPublished);
            var grid = QueryToPostGridModel(query);
            return View(grid);
        }

        [Authorize]
        [Route("manage/draft")]
        public IActionResult Draft()
        {
            var query = _postService.GetPostsAsQueryable()
                .Where(p => !p.PostPublish.IsDeleted && !p.PostPublish.IsPublished);
            var grid = QueryToPostGridModel(query);
            return View(grid);
        }

        [Authorize]
        [Route("manage/recycle-bin")]
        public IActionResult RecycleBin()
        {
            var query = _postService.GetPostsAsQueryable().Where(p => p.PostPublish.IsDeleted);
            var grid = QueryToPostGridModel(query);
            return View(grid);
        }

        [Authorize]
        [Route("manage/create")]
        public IActionResult Create()
        {
            var view = GetCreatePostModel();
            return View("CreateOrEdit", view);
        }

        [Authorize]
        [Route("manage/create")]
        [HttpPost, ValidateAntiForgeryToken]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        public IActionResult Create(PostEditModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var id = model.PostId == Guid.Empty ? Guid.NewGuid() : model.PostId;

                    var post = new Post
                    {
                        CommentEnabled = model.EnableComment,
                        Id = id,
                        PostContent = HttpUtility.HtmlEncode(model.HtmlContent),
                        ContentAbstract = Utils.GetPostAbstract(model.HtmlContent, AppSettings.PostSummaryWords),
                        CreateOnUtc = DateTime.UtcNow,
                        Slug = model.Slug.Trim(),
                        Title = model.Title.Trim(),
                        PostPublish = new PostPublish
                        {
                            IsPublished = model.IsPublished,
                            PubDateUtc = model.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                            ExposedToSiteMap = model.ExposedToSiteMap,
                            IsFeedIncluded = model.FeedIncluded,
                            Revision = 0,
                            PublisherIp = HttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString(),
                            ContentLanguageCode = model.ContentLanguageCode
                        }
                    };

                    // get tags
                    List<string> tagList = string.IsNullOrWhiteSpace(model.Tags)
                        ? new List<string>()
                        : model.Tags.Split(',').ToList();

                    // get category Ids
                    List<Guid> catIds = model.SelectedCategoryIds.ToList();

                    var response = _postService.CreateNewPost(post, tagList, catIds);
                    if (response.IsSuccess)
                    {
                        if (model.IsPublished)
                        {
                            Logger.LogInformation($"Trying to Ping URL for post: {post.Id}");

                            var pubDate = post.PostPublish.PubDateUtc.GetValueOrDefault();
                            var link = GetPostUrl(_linkGenerator, pubDate, post.Slug);

                            if (AppSettings.EnablePingBackSend)
                            {
                                Task.Run(async () => { await _pingbackSender.TrySendPingAsync(link, post.PostContent); });
                            }
                        }

                        return RedirectToAction(nameof(Manage));
                    }

                    ModelState.AddModelError("", response.Message);
                    return View("CreateOrEdit", model);
                }

                return View("CreateOrEdit", model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error Creating New Post.");
                ModelState.AddModelError("", ex.Message);
                return View("CreateOrEdit", model);
            }
        }


        [Authorize]
        [Route("manage/edit")]
        public IActionResult Edit(Guid id)
        {
            var postResponse = _postService.GetPost(id);
            if (!postResponse.IsSuccess)
            {
                return ServerError();
            }

            var post = postResponse.Item;
            if (null != post)
            {
                var editViewModel = new PostEditModel
                {
                    PostId = post.Id,
                    IsPublished = post.PostPublish.IsPublished,
                    HtmlContent = HttpUtility.HtmlDecode(post.PostContent),
                    Slug = post.Slug,
                    Title = post.Title,
                    EnableComment = post.CommentEnabled.GetValueOrDefault(),
                    ExposedToSiteMap = post.PostPublish.ExposedToSiteMap,
                    FeedIncluded = post.PostPublish.IsFeedIncluded,
                    ContentLanguageCode = post.PostPublish.ContentLanguageCode
                };

                ViewBag.PubDateStr = $"{post.PostPublish.PubDateUtc.GetValueOrDefault():yyyy/M/d}";

                var tagStr = post.PostTag
                    .Select(pt => pt.Tag)
                    .Select(p => p.DisplayName)
                    .Aggregate(string.Empty, (current, item) => current + (item + ","));

                tagStr = tagStr.TrimEnd(',');
                editViewModel.Tags = tagStr;

                var catResponse = _categoryService.GetAllCategories();
                if (!catResponse.IsSuccess)
                {
                    return ServerError("Unsuccessful response from _categoryService.GetAllCategories().");
                }

                var catList = catResponse.Item;
                if (null != catList && catList.Count > 0)
                {
                    var cbCatList = catList.Select(p =>
                        new CheckBoxListInfo(
                            p.DisplayName,
                            p.Id.ToString(),
                            post.PostCategory.Any(q => q.CategoryId == p.Id))).ToList();
                    editViewModel.CategoryList = cbCatList;
                }

                return View("CreateOrEdit", editViewModel);
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [Route("manage/edit")]
        public IActionResult Edit(PostEditModel model)
        {
            if (ModelState.IsValid)
            {
                var postResponse = _postService.GetPost(model.PostId);
                if (!postResponse.IsSuccess)
                {
                    return ServerError();
                }

                var post = postResponse.Item;
                if (null == post) return NotFound();

                post.CommentEnabled = model.EnableComment;
                post.PostContent = HttpUtility.HtmlEncode(model.HtmlContent);
                post.ContentAbstract = Utils.GetPostAbstract(model.HtmlContent, AppSettings.PostSummaryWords);
                post.PostPublish.IsPublished = model.IsPublished;
                post.Slug = model.Slug;
                post.Title = model.Title;
                post.PostPublish.ExposedToSiteMap = model.ExposedToSiteMap;
                post.PostPublish.LastModifiedUtc = DateTime.UtcNow;
                post.PostPublish.IsFeedIncluded = model.FeedIncluded;
                post.PostPublish.ContentLanguageCode = model.ContentLanguageCode;

                var tagList = string.IsNullOrWhiteSpace(model.Tags)
                    ? new List<string>()
                    : model.Tags.Split(',').ToList();

                var catIds = model.SelectedCategoryIds.ToList();

                var response = _postService.EditPost(post, tagList, catIds);
                if (response.IsSuccess)
                {
                    if (model.IsPublished)
                    {
                        Logger.LogInformation($"Trying to Ping URL for post: {post.Id}");

                        var pubDate = post.PostPublish.PubDateUtc.GetValueOrDefault();
                        var link = GetPostUrl(_linkGenerator, pubDate, post.Slug);

                        if (AppSettings.EnablePingBackSend)
                        {
                            Task.Run(async () => { await _pingbackSender.TrySendPingAsync(link, post.PostContent); });
                        }
                    }

                    return RedirectToAction(nameof(Manage));
                }

                ModelState.AddModelError("", response.Message);
                return View("CreateOrEdit", model);

            }

            return View("CreateOrEdit", model);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [Route("manage/restore")]
        public IActionResult Restore(Guid postId)
        {
            var response = _postService.RestoreFromRecycle(postId);
            if (response.IsSuccess)
            {
                return Json(postId);
            }

            return ServerError();
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [Route("manage/delete")]
        public IActionResult Delete(Guid postId)
        {
            var response = _postService.Delete(postId, true);
            if (response.IsSuccess)
            {
                return Json(postId);
            }

            return ServerError();
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [Route("manage/delete-from-recycle")]
        public IActionResult DeleteFromRecycleBin(Guid postId)
        {
            var response = _postService.Delete(postId, false);
            if (response.IsSuccess)
            {
                return Json(postId);
            }

            return ServerError();
        }

        #endregion

        #region Helper Methods

        private PostEditModel GetCreatePostModel()
        {
            var view = new PostEditModel
            {
                PostId = Guid.NewGuid(),
                IsPublished = true,
                EnableComment = true,
                ExposedToSiteMap = true,
                FeedIncluded = true
            };

            var catList = _categoryService.GetCategoriesAsQueryable();
            if (null != catList && catList.Any())
            {
                var cbCatList = catList.Select(p => new CheckBoxListInfo(p.DisplayName, p.Id.ToString(), false)).ToList();
                view.CategoryList = cbCatList;
            }

            return view;
        }

        private List<PostGridModel> QueryToPostGridModel(IQueryable<Post> query)
        {
            var result = query.Select(p => new PostGridModel
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

            return result.ToList();
        }

        #endregion
    }
}