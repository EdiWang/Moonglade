using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Core.PageFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PageController(ICacheAside cache, IQueryMediator queryMediator, ICommandMediator commandMediator) : Controller
{
    [HttpPost]
    [ReadonlyMode]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(EditPageRequest model)
    {
        var uid = await commandMediator.SendAsync(new CreatePageCommand(model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpPut("{id:guid}")]
    [ReadonlyMode]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Edit([NotEmpty] Guid id, EditPageRequest model)
    {
        var uid = await commandMediator.SendAsync(new UpdatePageCommand(id, model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpDelete("{id:guid}")]
    [ReadonlyMode]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        await commandMediator.SendAsync(new DeletePageCommand(id));

        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug);
        return NoContent();
    }
}