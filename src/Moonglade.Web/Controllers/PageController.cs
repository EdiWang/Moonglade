using Moonglade.Caching.Filters;
using Moonglade.Core.PageFeature;
using Moonglade.Web.Attributes;
using NUglify;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PageController : Controller
{
    private readonly IBlogCache _cache;
    private readonly IMediator _mediator;

    public PageController(
        IBlogCache cache,
        IMediator mediator)
    {
        _cache = cache;
        _mediator = mediator;
    }

    [HttpGet("segment/published")]
    [FeatureGate(FeatureFlags.EnableWebApi)]
    [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
    [ProducesResponseType(typeof(IEnumerable<PageSegment>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Segment()
    {
        var pageSegments = await _mediator.Send(new ListPageSegmentQuery());
        if (pageSegments is null) return Ok(Array.Empty<PageSegment>());

        // for security, only allow published pages to be listed to third party API calls
        var published = pageSegments.Where(p => p.IsPublished);
        return Ok(published);
    }

    [HttpPost]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Create(EditPageRequest model)
    {
        return CreateOrEdit(model, async request => await _mediator.Send(new CreatePageCommand(request)));
    }

    [HttpPut("{id:guid}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Edit([NotEmpty] Guid id, EditPageRequest model)
    {
        return CreateOrEdit(model, async request => await _mediator.Send(new UpdatePageCommand(id, request)));
    }

    private async Task<IActionResult> CreateOrEdit(EditPageRequest model, Func<EditPageRequest, Task<Guid>> pageServiceAction)
    {
        if (!string.IsNullOrWhiteSpace(model.CssContent))
        {
            var uglifyTest = Uglify.Css(model.CssContent);
            if (uglifyTest.HasErrors)
            {
                foreach (var err in uglifyTest.Errors)
                {
                    ModelState.AddModelError(model.CssContent, err.ToString());
                }
                return BadRequest(ModelState.CombineErrorMessages());
            }
        }

        var uid = await pageServiceAction(model);

        _cache.Remove(CacheDivision.Page, model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var page = await _mediator.Send(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        await _mediator.Send(new DeletePageCommand(id));

        _cache.Remove(CacheDivision.Page, page.Slug);
        return NoContent();
    }
}