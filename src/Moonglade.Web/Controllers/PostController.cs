using Moonglade.Caching.Filters;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using Moonglade.Pingback;
using Moonglade.Web.Attributes;
using NUglify;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PostController : ControllerBase
{
    private readonly IMediator _mediator;

    private readonly IBlogConfig _blogConfig;
    private readonly ITimeZoneResolver _timeZoneResolver;
    private readonly IPingbackSender _pingbackSender;
    private readonly ILogger<PostController> _logger;

    public PostController(
        IMediator mediator,
        IBlogConfig blogConfig,
        ITimeZoneResolver timeZoneResolver,
        IPingbackSender pingbackSender,
        ILogger<PostController> logger)
    {
        _mediator = mediator;
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
        var list = await _mediator.Send(new ListPostSegmentByStatusQuery(PostStatus.Published));
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

        var (posts, totalRows) = await _mediator.Send(new ListPostSegmentQuery(PostStatus.Published, offset, take, searchBy));
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

            if (!string.IsNullOrWhiteSpace(model.InlineCss))
            {
                var uglifyTest = Uglify.Css(model.InlineCss);
                if (uglifyTest.HasErrors)
                {
                    foreach (var err in uglifyTest.Errors)
                    {
                        ModelState.AddModelError(model.InlineCss, err.ToString());
                    }
                    return BadRequest(ModelState.CombineErrorMessages());
                }

                model.InlineCss = uglifyTest.Code;
            }

            var tzDate = _timeZoneResolver.NowOfTimeZone;
            if (model.ChangePublishDate &&
                model.PublishDate.HasValue &&
                model.PublishDate <= tzDate &&
                model.PublishDate.GetValueOrDefault().Year >= 1975)
            {
                model.PublishDate = _timeZoneResolver.ToUtc(model.PublishDate.Value);
            }

            var postEntity = model.PostId == Guid.Empty ?
                await _mediator.Send(new CreatePostCommand(model)) :
                await _mediator.Send(new UpdatePostCommand(model.PostId, model));

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

                if (_blogConfig.AdvancedSettings.EnablePingbackSend)
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
        await _mediator.Send(new RestorePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
    [HttpDelete("{postId:guid}/recycle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid postId)
    {
        await _mediator.Send(new DeletePostCommand(postId, true));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [HttpDelete("{postId:guid}/destroy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteFromRecycleBin([NotEmpty] Guid postId)
    {
        await _mediator.Send(new DeletePostCommand(postId));
        return NoContent();
    }

    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.Subscription })]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [HttpDelete("empty-recycle-bin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EmptyRecycleBin()
    {
        await _mediator.Send(new PurgeRecycledCommand());
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