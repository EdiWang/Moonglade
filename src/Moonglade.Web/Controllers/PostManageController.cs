using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Pingback;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PostManageController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly IPostManageService _postManageService;

        private readonly IBlogConfig _blogConfig;
        private readonly ITimeZoneResolver _timeZoneResolver;
        private readonly IPingbackSender _pingbackSender;
        private readonly ILogger<PostManageController> _logger;

        public PostManageController(
            IPostService postService,
            IPostManageService postManageService,
            IBlogConfig blogConfig,
            ITimeZoneResolver timeZoneResolver,
            IPingbackSender pingbackSender,
            ILogger<PostManageController> logger)
        {
            _postService = postService;
            _postManageService = postManageService;
            _blogConfig = blogConfig;
            _timeZoneResolver = timeZoneResolver;
            _pingbackSender = pingbackSender;
            _logger = logger;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("list-published")]
        [ProducesResponseType(typeof(JqDataTableResponse<PostSegment>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListPublished([FromForm] DataTableRequest model)
        {
            var jqdtResponse = await GetJqDataTableResponse(PostStatus.Published, model);
            return Ok(jqdtResponse);
        }

        private async Task<JqDataTableResponse<PostSegment>> GetJqDataTableResponse(PostStatus status, DataTableRequest model)
        {
            var searchBy = model.Search?.Value;
            var take = model.Length;
            var offset = model.Start;

            var (posts, totalRows) = await _postService.ListSegment(status, offset, take, searchBy);
            var jqdtResponse = new JqDataTableResponse<PostSegment>
            {
                Draw = model.Draw,
                RecordsFiltered = totalRows,
                RecordsTotal = totalRows,
                Data = posts
            };

            return jqdtResponse;
        }

        [HttpPost("createoredit")]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [ServiceFilter(typeof(ClearSubscriptionCache))]
        [TypeFilter(typeof(ClearPagingCountCache))]
        public async Task<IActionResult> CreateOrEdit(
            [FromForm] MagicWrapper<PostEditModel> temp, [FromServices] LinkGenerator linkGenerator)
        {
            try
            {
                if (!ModelState.IsValid) return Conflict(ModelState.CombineErrorMessages());

                // temp solution
                var model = temp.ViewModel;

                var tags = string.IsNullOrWhiteSpace(model.Tags)
                    ? Array.Empty<string>()
                    : model.Tags.Split(',').ToArray();

                var request = new UpdatePostRequest
                {
                    Title = model.Title.Trim(),
                    Slug = model.Slug.Trim(),
                    EditorContent = model.EditorContent,
                    EnableComment = model.EnableComment,
                    ExposedToSiteMap = model.ExposedToSiteMap,
                    IsFeedIncluded = model.FeedIncluded,
                    ContentLanguageCode = model.LanguageCode,
                    IsPublished = model.IsPublished,
                    IsFeatured = model.Featured,
                    Tags = tags,
                    CategoryIds = model.SelectedCategoryIds
                };

                var tzDate = _timeZoneResolver.NowOfTimeZone;
                if (model.ChangePublishDate &&
                    model.PublishDate.HasValue &&
                    model.PublishDate <= tzDate &&
                    model.PublishDate.GetValueOrDefault().Year >= 1975)
                {
                    request.PublishDate = _timeZoneResolver.ToUtc(model.PublishDate.Value);
                }

                var postEntity = model.PostId == Guid.Empty ?
                    await _postManageService.CreateAsync(request) :
                    await _postManageService.UpdateAsync(model.PostId, request);

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
                        _ = Task.Run(async () => { await _pingbackSender.TrySendPingAsync(link, postEntity.PostContent); });
                    }
                }

                return Ok(new { PostId = postEntity.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Creating New Post.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Conflict(ex.Message);
            }
        }

        [ServiceFilter(typeof(ClearSubscriptionCache))]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [TypeFilter(typeof(ClearPagingCountCache))]
        [HttpPost("{postId:guid}/restore")]
        public async Task<IActionResult> Restore(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _postManageService.RestoreAsync(postId);
            return Ok();
        }

        [ServiceFilter(typeof(ClearSubscriptionCache))]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [TypeFilter(typeof(ClearPagingCountCache))]
        [HttpDelete("{postId:guid}/recycle")]
        public async Task<IActionResult> Delete(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _postManageService.DeleteAsync(postId, true);
            return Ok();
        }

        [ServiceFilter(typeof(ClearSubscriptionCache))]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [HttpDelete("{postId:guid}/destroy")]
        public async Task<IActionResult> DeleteFromRecycleBin(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(postId), "value is empty");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _postManageService.DeleteAsync(postId);
            return Ok();
        }

        [ServiceFilter(typeof(ClearSubscriptionCache))]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [HttpGet("empty-recycle-bin")]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            await _postManageService.PurgeRecycledAsync();
            return Redirect("/admin/post/recycle-bin");
        }
    }
}
