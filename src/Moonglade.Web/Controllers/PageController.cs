using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PageController(ICacheAside cache, IQueryMediator queryMediator, ICommandMediator commandMediator) : Controller
{
    [HttpPost]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(EditPageRequest model)
    {
        var uid = await commandMediator.SendAsync(new CreatePageCommand(model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpPut("{id:guid}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Edit([NotEmpty] Guid id, EditPageRequest model)
    {
        var uid = await commandMediator.SendAsync(new UpdatePageCommand(id, model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());
        return Ok(new { PageId = uid });
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        await commandMediator.SendAsync(new DeletePageCommand(id));

        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug);
        return NoContent();
    }

    [HttpGet("segment/list")]
    [ProducesResponseType<List<PageSegment>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPageSegmentList()
    {
        var segments = await queryMediator.QueryAsync(new ListPageSegmentsQuery());
        return Ok(segments);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PageDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        StyleSheetEntity css = null;
        if (!string.IsNullOrWhiteSpace(page.CssId))
        {
            css = await queryMediator.QueryAsync(new GetStyleSheetQuery(Guid.Parse(page.CssId)));
        }

        var response = new PageDetail
        {
            Id = page.Id,
            Title = page.Title,
            Slug = page.Slug,
            MetaDescription = page.MetaDescription,
            CssContent = css?.CssContent,
            HtmlContent = page.HtmlContent,
            HideSidebar = page.HideSidebar,
            IsPublished = page.IsPublished
        };

        return Ok(response);
    }
}