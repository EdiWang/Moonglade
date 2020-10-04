using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Core.Caching;
using Moonglade.Data.Spec;
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
            var list = await _postService.ListSegmentAsync(PostPublishStatus.Published);
            return View(list);
        }

        [Authorize]
        [Route("manage/draft")]
        public async Task<IActionResult> Draft()
        {
            var list = await _postService.ListSegmentAsync(PostPublishStatus.Draft);
            return View(list);
        }

        [Authorize]
        [Route("manage/recycle-bin")]
        public async Task<IActionResult> RecycleBin()
        {
            var list = await _postService.ListSegmentAsync(PostPublishStatus.Deleted);
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
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await _postService.GetAsync(id);
            if (null == post) return NotFound();

            var editViewModel = new PostEditViewModel
            {
                PostId = post.Id,
                IsPublished = post.IsPublished,
                EditorContent = post.RawPostContent,
                Slug = post.Slug,
                Title = post.Title,
                EnableComment = post.CommentEnabled,
                ExposedToSiteMap = post.ExposedToSiteMap,
                FeedIncluded = post.IsFeedIncluded,
                ContentLanguageCode = post.ContentLanguageCode
            };

            if (post.PubDateUtc != null)
            {
                editViewModel.PublishDate = _dateTimeResolver.ToTimeZone(post.PubDateUtc.GetValueOrDefault());
            }

            var tagStr = post.Tags
                .Select(p => p.DisplayName)
                .Aggregate(string.Empty, (current, item) => current + item + ",");

            tagStr = tagStr.TrimEnd(',');
            editViewModel.Tags = tagStr;

            var cats = await _categoryService.GetAllAsync();
            if (cats.Count > 0)
            {
                var cbCatList = cats.Select(p =>
                    new CheckBoxViewModel(
                        p.DisplayName,
                        p.Id.ToString(),
                        post.Categories.Any(q => q.Id == p.Id)));
                editViewModel.CategoryList = cbCatList;
            }

            return View("CreateOrEdit", editViewModel);
        }

        [Authorize]
        [HttpPost("manage/createoredit")]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "postcount" })]
        [TypeFilter(typeof(DeleteBlogCacheDivision), Arguments = new object[] { CacheDivision.PostCountCategory })]
        public async Task<IActionResult> CreateOrEdit(PostEditViewModel model,
            [FromServices] LinkGenerator linkGenerator,
            [FromServices] IPingbackSender pingbackSender)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json("Invalid ModelState");
                }

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
                    CategoryIds = model.SelectedCategoryIds
                };

                var tzDate = _dateTimeResolver.GetNowOfTimeZone();
                if (model.ChangePublishDate &&
                    model.PublishDate.HasValue &&
                    model.PublishDate <= tzDate &&
                    model.PublishDate.GetValueOrDefault().Year >= 1975)
                {
                    request.PublishDate = model.PublishDate;
                }

                var postEntity = model.PostId == Guid.Empty ?
                    await _postService.CreateAsync(request) :
                    await _postService.UpdateAsync(request);

                if (model.IsPublished)
                {
                    Logger.LogInformation($"Trying to Ping URL for post: {postEntity.Id}");

                    var pubDate = postEntity.PubDateUtc.GetValueOrDefault();
                    var link = GetPostUrl(linkGenerator, pubDate, postEntity.Slug);

                    if (_blogConfig.AdvancedSettings.EnablePingBackSend)
                    {
                        _ = Task.Run(async () => { await pingbackSender.TrySendPingAsync(link, postEntity.PostContent); });
                    }
                }

                return Json(new { PostId = postEntity.Id });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error Creating New Post.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(ex.Message);
            }
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "postcount" })]
        [TypeFilter(typeof(DeleteBlogCacheDivision), Arguments = new object[] { CacheDivision.PostCountCategory })]
        [HttpPost("manage/restore")]
        public async Task<IActionResult> Restore(Guid postId)
        {
            await _postService.RestoreDeletedAsync(postId);
            return Json(postId);
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "postcount" })]
        [TypeFilter(typeof(DeleteBlogCacheDivision), Arguments = new object[] { CacheDivision.PostCountCategory })]
        [HttpPost("manage/delete")]
        public async Task<IActionResult> Delete(Guid postId)
        {
            await _postService.DeleteAsync(postId, true);
            return Json(postId);
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [HttpPost("manage/delete-from-recycle")]
        public async Task<IActionResult> DeleteFromRecycleBin(Guid postId)
        {
            await _postService.DeleteAsync(postId);
            return Json(postId);
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [HttpGet("manage/empty-recycle-bin")]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            await _postService.DeleteRecycledAsync();
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
                FeedIncluded = true,
                ContentLanguageCode = _blogConfig.ContentSettings.DefaultLangCode
            };

            var catList = await _categoryService.GetAllAsync();
            if (catList.Count > 0)
            {
                var cbCatList = catList.Select(p =>
                    new CheckBoxViewModel(p.DisplayName, p.Id.ToString(), false));
                view.CategoryList = cbCatList;
            }

            return view;
        }

        #endregion
    }
}