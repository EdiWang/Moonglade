using Moonglade.Core.PageFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PageController(ICacheAside cache, IMediator mediator) : Controller
{
    [HttpPost]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(EditPageRequest model)
    {
        var uid = await mediator.Send(new CreatePageCommand(model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpPut("{id:guid}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Edit([NotEmpty] Guid id, EditPageRequest model)
    {
        var uid = await mediator.Send(new UpdatePageCommand(id, model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var page = await mediator.Send(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        await mediator.Send(new DeletePageCommand(id));

        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug);
        return NoContent();
    }
}