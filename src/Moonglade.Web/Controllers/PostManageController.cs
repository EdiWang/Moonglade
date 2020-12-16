using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.DateTimeOps;
using Moonglade.Model;
using Moonglade.Pingback;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("post/manage")]
    public class PostManageController : BlogController
    {
        private readonly PostService _postService;
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IDateTimeResolver _dateTimeResolver;
        private readonly ILogger<PostManageController> _logger;

        public PostManageController(
            PostService postService,
            CategoryService categoryService,
            IBlogConfig blogConfig,
            IDateTimeResolver dateTimeResolver,
            ILogger<PostManageController> logger)
        {
            _postService = postService;
            _blogConfig = blogConfig;
            _categoryService = categoryService;
            _dateTimeResolver = dateTimeResolver;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _postService.ListSegmentAsync(PostStatus.Published);
            return View(list);
        }

        [Route("draft")]
        public async Task<IActionResult> Draft()
        {
            var list = await _postService.ListSegmentAsync(PostStatus.Draft);
            return View(list);
        }

        [Route("recycle-bin")]
        public async Task<IActionResult> RecycleBin()
        {
            var list = await _postService.ListSegmentAsync(PostStatus.Deleted);
            return View(list);
        }

        [Route("create")]
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

        [Route("edit/{id:guid}")]
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

        [HttpPost("createoredit")]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [TypeFilter(typeof(DeletePagingCountCache))]
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
                    _logger.LogInformation($"Trying to Ping URL for post: {postEntity.Id}");

                    var pubDate = postEntity.PubDateUtc.GetValueOrDefault();

                    var link = linkGenerator.GetUriByAction(HttpContext, "Slug", "Post",
                               new
                               {
                                   year = pubDate.Year,
                                   month = pubDate.Month,
                                   day = pubDate.Day,
                                   postEntity.Slug
                               });

                    if (_blogConfig.AdvancedSettings.EnablePingBackSend)
                    {
                        _ = Task.Run(async () => { await pingbackSender.TrySendPingAsync(link, postEntity.PostContent); });
                    }
                }

                return Json(new { PostId = postEntity.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Creating New Post.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(ex.Message);
            }
        }

        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [TypeFilter(typeof(DeletePagingCountCache))]
        [HttpPost("{postId:guid}/restore")]
        public async Task<IActionResult> Restore(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState);
            }

            await _postService.RestoreAsync(postId);
            return Ok();
        }

        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [TypeFilter(typeof(DeletePagingCountCache))]
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

        [ServiceFilter(typeof(DeleteSubscriptionCache))]
        [ServiceFilter(typeof(DeleteSiteMapCache))]
        [HttpGet("empty-recycle-bin")]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            await _postService.PurgeRecycledAsync();
            return RedirectToAction("RecycleBin");
        }

        [HttpGet("insights")]
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
    }
}
