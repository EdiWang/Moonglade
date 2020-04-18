using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Spec;
using Moonglade.HtmlEncoding;
using Moonglade.Model;
using Moonglade.Pingback;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    public partial class PostController
    {
        #region Management

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var list = await _postService.GetMetaListAsync(PostPublishStatus.Published);
            return View(list);
        }

        [Authorize]
        [Route("manage/draft")]
        public async Task<IActionResult> Draft()
        {
            var list = await _postService.GetMetaListAsync(PostPublishStatus.Draft);
            return View(list);
        }

        [Authorize]
        [Route("manage/recycle-bin")]
        public async Task<IActionResult> RecycleBin()
        {
            var list = await _postService.GetMetaListAsync(PostPublishStatus.Deleted);
            return View(list);
        }

        [Authorize]
        [Route("manage/create")]
        public async Task<IActionResult> Create()
        {
            var view = await GetCreatePostModelAsync();
            return View("CreateOrEdit", view);
        }

        [Authorize]
        [Route("manage/edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id, [FromServices] IHtmlCodec htmlCodec)
        {
            var postResponse = await _postService.GetPostAsync(id);
            if (!postResponse.IsSuccess)
            {
                return ServerError();
            }

            var post = postResponse.Item;
            if (null != post)
            {
                var editViewModel = new PostEditViewModel
                {
                    PostId = post.Id,
                    IsPublished = post.IsPublished,
                    EditorContent = AppSettings.Editor == Model.Settings.EditorChoice.Markdown ?
                                                        post.RawPostContent :
                                                        htmlCodec.HtmlDecode(post.RawPostContent),
                    Slug = post.Slug,
                    Title = post.Title,
                    EnableComment = post.CommentEnabled,
                    ExposedToSiteMap = post.ExposedToSiteMap,
                    FeedIncluded = post.FeedIncluded,
                    ContentLanguageCode = post.ContentLanguageCode
                };

                if (post.PubDateUtc != null)
                {
                    editViewModel.PublishDate = _dateTimeResolver.GetDateTimeWithUserTZone(post.PubDateUtc.GetValueOrDefault());
                }

                var tagStr = post.Tags
                                 .Select(p => p.TagName)
                                 .Aggregate(string.Empty, (current, item) => current + item + ",");

                tagStr = tagStr.TrimEnd(',');
                editViewModel.Tags = tagStr;

                var catResponse = await _categoryService.GetAllCategoriesAsync();
                if (!catResponse.IsSuccess)
                {
                    return ServerError("Unsuccessful response from _categoryService.GetAllCategoriesAsync().");
                }

                var catList = catResponse.Item;
                if (null != catList && catList.Count > 0)
                {
                    var cbCatList = catList.Select(p =>
                        new CheckBoxViewModel(
                            p.DisplayName,
                            p.Id.ToString(),
                            post.Categories.Any(q => q.Id == p.Id))).ToList();
                    editViewModel.CategoryList = cbCatList;
                }

                return View("CreateOrEdit", editViewModel);
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost("manage/createoredit")]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteMemoryCache), Arguments = new object[] { StaticCacheKeys.PostCount })]
        public async Task<IActionResult> CreateOrEdit(PostEditViewModel model,
            [FromServices] LinkGenerator linkGenerator,
            [FromServices] IPingbackSender pingbackSender)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var tagList = string.IsNullOrWhiteSpace(model.Tags)
                                             ? new string[] { }
                                             : model.Tags.Split(',').ToArray();

                    var request = new EditPostRequest(model.PostId)
                    {
                        Title = model.Title.Trim(),
                        Slug = model.Slug.Trim(),
                        EditorContent = model.EditorContent,
                        EnableComment = model.EnableComment,
                        ExposedToSiteMap = model.ExposedToSiteMap,
                        IsFeedIncluded = model.FeedIncluded,
                        ContentLanguageCode = model.ContentLanguageCode,
                        IsPublished = model.IsPublished,
                        Tags = tagList,
                        CategoryIds = model.SelectedCategoryIds,
                        RequestIp = HttpContext.Connection.RemoteIpAddress.ToString()
                    };

                    var tzDate = _dateTimeResolver.GetNowWithUserTZone();
                    if (model.ChangePublishDate &&
                        model.PublishDate.HasValue &&
                        model.PublishDate <= tzDate &&
                        model.PublishDate.GetValueOrDefault().Year >= 1975)
                    {
                        request.PublishDate = model.PublishDate;
                    }

                    var response = model.PostId == Guid.Empty ?
                        await _postService.CreateNewPost(request) :
                        await _postService.EditPost(request);

                    if (response.IsSuccess)
                    {
                        if (model.IsPublished)
                        {
                            Logger.LogInformation($"Trying to Ping URL for post: {response.Item.Id}");

                            var pubDate = response.Item.PostPublish.PubDateUtc.GetValueOrDefault();
                            var link = GetPostUrl(linkGenerator, pubDate, response.Item.Slug);

                            if (_blogConfig.AdvancedSettings.EnablePingBackSend)
                            {
                                _ = Task.Run(async () => { await pingbackSender.TrySendPingAsync(link, response.Item.PostContent); });
                            }
                        }

                        return Json(new { PostId = response.Item.Id });
                    }

                    Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return Json(new FailedResponse(response.Message));
                }

                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return Json(new FailedResponse("Invalid ModelState"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error Creating New Post.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new FailedResponse(ex.Message));
            }
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteMemoryCache), Arguments = new object[] { StaticCacheKeys.PostCount })]
        [HttpPost("manage/restore")]
        public async Task<IActionResult> Restore(Guid postId)
        {
            var response = await _postService.RestoreDeletedPostAsync(postId);
            return response.IsSuccess ? Json(postId) : ServerError();
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteMemoryCache), Arguments = new object[] { StaticCacheKeys.PostCount })]
        [HttpPost("manage/delete")]
        public async Task<IActionResult> Delete(Guid postId)
        {
            var response = await _postService.DeleteAsync(postId, true);

            return response.IsSuccess ? Json(postId) : ServerError();
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [HttpPost("manage/delete-from-recycle")]
        public async Task<IActionResult> DeleteFromRecycleBin(Guid postId)
        {
            var response = await _postService.DeleteAsync(postId);
            return response.IsSuccess ? Json(postId) : ServerError();
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [HttpGet("manage/empty-recycle-bin")]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            await _postService.DeleteRecycledPostsAsync();
            return RedirectToAction("RecycleBin");
        }

        [Authorize]
        [HttpGet("manage/insights")]
        public async Task<IActionResult> Insights()
        {
            var topReadList = await _postService.GetInsightsAsync(PostInsightsType.TopRead);
            var topCommentedList = await _postService.GetInsightsAsync(PostInsightsType.TopCommented);

            var vm = new PostInsightsViewModel
            {
                TopReadPosts = topReadList,
                TopCommentedPosts = topCommentedList
            };

            return View(vm);
        }

        #endregion

        #region Helper Methods

        private async Task<PostEditViewModel> GetCreatePostModelAsync()
        {
            var view = new PostEditViewModel
            {
                PostId = Guid.Empty,
                IsPublished = false,
                EnableComment = true,
                ExposedToSiteMap = true,
                FeedIncluded = true
            };

            var catList = await _categoryService.GetAllCategoriesAsync();
            if (null != catList.Item && catList.Item.Any())
            {
                var cbCatList = catList.Item.Select(p =>
                    new CheckBoxViewModel(p.DisplayName, p.Id.ToString(), false)).ToList();
                view.CategoryList = cbCatList;
            }

            return view;
        }

        #endregion
    }
}