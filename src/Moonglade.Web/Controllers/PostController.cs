using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Caching.Filters;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Pingback;
using Moonglade.Utils;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IPostQueryService _postQueryService;
        private readonly IPostManageService _postManageService;

        private readonly IBlogConfig _blogConfig;
        private readonly ITimeZoneResolver _timeZoneResolver;
        private readonly IPingbackSender _pingbackSender;
        private readonly ILogger<PostController> _logger;

        public PostController(
            IPostQueryService postQueryService,
            IPostManageService postManageService,
            IBlogConfig blogConfig,
            ITimeZoneResolver timeZoneResolver,
            IPingbackSender pingbackSender,
            ILogger<PostController> logger)
        {
            _postQueryService = postQueryService;
            _postManageService = postManageService;
            _blogConfig = blogConfig;
            _timeZoneResolver = timeZoneResolver;
            _pingbackSender = pingbackSender;
            _logger = logger;
        }

        [HttpGet("segment/published")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
        [ProducesResponseType(typeof(IReadOnlyList<PostSegment>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Segment()
        {
            // for security, only allow published posts to be listed to third party API calls
            var list = await _postQueryService.ListSegmentAsync(PostStatus.Published);
            return Ok(list);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("list-published")]
        [ProducesResponseType(typeof(JqDataTable<PostSegment>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListPublished([FromForm] DataTableRequest model)
        {
            var searchBy = model.Search?.Value;
            var take = model.Length;
            var offset = model.Start;

            var (posts, totalRows) = await _postQueryService.ListSegmentAsync(PostStatus.Published, offset, take, searchBy);
            var response = new JqDataTable<PostSegment>
            {
                Draw = model.Draw,
                RecordsFiltered = totalRows,
                RecordsTotal = totalRows,
                Data = posts
            };
            return Ok(response);
        }

        [HttpPost("createoredit")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateOrEdit(
            [FromForm] MagicWrapper<PostEditModel> temp, [FromServices] LinkGenerator linkGenerator)
        {
            try
            {
                if (temp.ViewModel.CategoryList.All(p => !p.IsChecked))
                {
                    ModelState.AddModelError(nameof(temp.ViewModel.CategoryList), "Please select at least one category.");
                }

                if (!temp.ViewModel.IsOriginal && string.IsNullOrWhiteSpace(temp.ViewModel.OriginLink))
                {
                    ModelState.AddModelError(nameof(temp.ViewModel.OriginLink), "Please enter the origin link.");
                }

                if (!ModelState.IsValid) return Conflict(ModelState.CombineErrorMessages());

                // temp solution
                var model = temp.ViewModel;

                var tags = string.IsNullOrWhiteSpace(model.Tags)
                    ? Array.Empty<string>()
                    : model.Tags.Split(',').ToArray();

                var catIds = model.CategoryList.Where(p => p.IsChecked).Select(p => p.Id).ToArray();

                var request = new UpdatePostRequest
                {
                    Title = model.Title.Trim(),
                    Slug = model.Slug.Trim(),
                    Author = model.Author?.Trim(),
                    EditorContent = model.EditorContent,
                    EnableComment = model.EnableComment,
                    ExposedToSiteMap = model.ExposedToSiteMap,
                    IsFeedIncluded = model.FeedIncluded,
                    ContentLanguageCode = model.LanguageCode,
                    Abstract = model.Abstract,
                    IsPublished = model.IsPublished,
                    IsFeatured = model.Featured,
                    IsOriginal = model.IsOriginal,
                    OriginLink = string.IsNullOrWhiteSpace(model.OriginLink) ? null : model.OriginLink,
                    HeroImageUrl = string.IsNullOrWhiteSpace(model.HeroImageUrl) ? null : model.HeroImageUrl,
                    Tags = tags,
                    CategoryIds = catIds
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

                    var link = linkGenerator.GetUriByPage(HttpContext, "/Post", null,
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
                return Conflict(ex.Message);
            }
        }

        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
        [HttpPost("{postId:guid}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Restore([NotEmpty] Guid postId)
        {
            await _postManageService.RestoreAsync(postId);
            return NoContent();
        }

        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
        [HttpDelete("{postId:guid}/recycle")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([NotEmpty] Guid postId)
        {
            await _postManageService.DeleteAsync(postId, true);
            return NoContent();
        }

        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [HttpDelete("{postId:guid}/destroy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteFromRecycleBin([NotEmpty] Guid postId)
        {
            await _postManageService.DeleteAsync(postId);
            return NoContent();
        }

        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [HttpDelete("empty-recycle-bin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> EmptyRecycleBin()
        {
            await _postManageService.PurgeRecycledAsync();
            return NoContent();
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("keep-alive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult KeepAlive([MaxLength(16)] string nonce)
        {
            return Ok(new
            {
                ServerTime = DateTime.UtcNow,
                Nonce = nonce
            });
        }
    }
}
