using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.DateTimeOps;
using Moonglade.Model;
using Moonglade.Pingback;
using Moonglade.Pingback.Mvc;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Route("post")]
    public class PostController : BlogController
    {
        private readonly PostService _postService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IDateTimeResolver _dateTimeResolver;

        public PostController(
            ILogger<PostController> logger,
            PostService postService,
            CategoryService categoryService,
            IBlogConfig blogConfig,
            IDateTimeResolver dateTimeResolver)
            : base(logger)
        {
            _postService = postService;
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _dateTimeResolver = dateTimeResolver;
        }

        [Route("{year:int:min(1975):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        [AddPingbackHeader("pingback")]
        public async Task<IActionResult> Slug(int year, int month, int day, string slug)
        {
            ViewBag.ErrorMessage = string.Empty;

            if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug))
            {
                Logger.LogWarning($"Invalid parameter year: {year}, slug: {slug}");
                return NotFound();
            }

            var slugInfo = new PostSlugInfo(year, month, day, slug);
            var post = await _postService.GetAsync(slugInfo);

            if (post is null)
            {
                Logger.LogWarning($"Post not found, parameter '{year}/{month}/{day}/{slug}'.");
                return NotFound();
            }

            ViewBag.TitlePrefix = $"{post.Title}";
            return View(post);
        }

        [Route("{year:int:min(1975):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}/{raw:regex(^(meta|content)$)}")]
        public async Task<IActionResult> Raw(int year, int month, int day, string slug, string raw)
        {
            var slugInfo = new PostSlugInfo(year, month, day, slug);

            if (!_blogConfig.SecuritySettings.EnablePostRawEndpoint) return NotFound();

            if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug))
            {
                Logger.LogWarning($"Invalid parameter year: {year}, slug: {slug}");
                return NotFound();
            }

            switch (raw.ToLower())
            {
                case "meta":
                    var meta = await _postService.GetSegmentAsync(slugInfo);
                    return Json(meta);

                case "content":
                    var content = await _postService.GetRawContentAsync(slugInfo);
                    return Content(content, "text/plain");
            }

            return BadRequest();
        }

        [Authorize]
        [Route("preview/{postId}")]
        public async Task<IActionResult> Preview(Guid postId)
        {
            var post = await _postService.GetDraftPreviewAsync(postId);
            if (post is null)
            {
                Logger.LogWarning($"Post not found, parameter '{postId}'.");
                return NotFound();
            }

            ViewBag.TitlePrefix = $"{post.Title}";
            ViewBag.IsDraftPreview = true;
            return View("Slug", post);
        }

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
            var view = new PostEditViewModel
            {
                IsPublished = false,
                EnableComment = true,
                ExposedToSiteMap = true,
                FeedIncluded = true,
                ContentLanguageCode = _blogConfig.ContentSettings.DefaultLangCode
            };

            var cats = await _categoryService.GetAllAsync();
            if (cats.Count > 0)
            {
                var cbCatList = cats.Select(p =>
                    new CheckBoxViewModel(p.DisplayName, p.Id.ToString(), false));
                view.CategoryList = cbCatList;
            }

            return View("CreateOrEdit", view);
        }

        [Authorize]
        [Route("manage/edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await _postService.GetAsync(id);
            if (null == post) return NotFound();

            var viewModel = new PostEditViewModel
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

            if (post.PubDateUtc is not null)
            {
                viewModel.PublishDate = _dateTimeResolver.ToTimeZone(post.PubDateUtc.GetValueOrDefault());
            }

            var tagStr = post.Tags
                .Select(p => p.DisplayName)
                .Aggregate(string.Empty, (current, item) => current + item + ",");

            tagStr = tagStr.TrimEnd(',');
            viewModel.Tags = tagStr;

            var cats = await _categoryService.GetAllAsync();
            if (cats.Count > 0)
            {
                var cbCatList = cats.Select(p =>
                    new CheckBoxViewModel(
                        p.DisplayName,
                        p.Id.ToString(),
                        post.Categories.Any(q => q.Id == p.Id)));
                viewModel.CategoryList = cbCatList;
            }

            return View("CreateOrEdit", viewModel);
        }

        [Authorize]
        [HttpPost("manage/createoredit")]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "postcount" })]
        [TypeFilter(typeof(DeleteBlogCacheDivision), Arguments = new object[] { CacheDivision.PostCountCategory })]
        public async Task<IActionResult> CreateOrEdit(PostEditViewModel model,
            [FromServices] LinkGenerator linkGenerator,
            [FromServices] IPingbackSender pingbackSender)
        {
            try
            {
                if (!ModelState.IsValid) return Conflict(ModelState);

                var tags = string.IsNullOrWhiteSpace(model.Tags)
                    ? Array.Empty<string>()
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
                    Tags = tags,
                    CategoryIds = model.SelectedCategoryIds
                };

                var tzDate = _dateTimeResolver.NowOfTimeZone;
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
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "postcount" })]
        [TypeFilter(typeof(DeleteBlogCacheDivision), Arguments = new object[] { CacheDivision.PostCountCategory })]
        [HttpPost("{postId:guid}/restore")]
        public async Task<IActionResult> Restore(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState);
            }

            await _postService.RestoreDeletedAsync(postId);
            return Ok();
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "postcount" })]
        [TypeFilter(typeof(DeleteBlogCacheDivision), Arguments = new object[] { CacheDivision.PostCountCategory })]
        [HttpDelete("{postId:guid}/recycle")]
        public async Task<IActionResult> Delete(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState);
            }

            await _postService.DeleteAsync(postId, true);
            return Ok();
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [HttpDelete("{postId:guid}/destroy")]
        public async Task<IActionResult> DeleteFromRecycleBin(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState);
            }

            await _postService.DeleteAsync(postId);
            return Ok();
        }

        [Authorize]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
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
    }
}