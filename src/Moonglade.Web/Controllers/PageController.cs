using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class PageController(ICacheAside cache, IQueryMediator queryMediator, ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpPost]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    public async Task<IActionResult> Create(EditPageRequest model)
    {
        var uid = await CommandMediator.SendAsync(new CreatePageCommand(model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());

        // Log activity
        await LogActivityAsync(
            EventType.PageCreated,
            "Create Page",
            model.Title,
            new { PageId = uid, model.Slug, model.IsPublished });

        return Ok(new { PageId = uid });
    }

    [HttpPut("{id:guid}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCacheType.SiteMap])]
    public async Task<IActionResult> Edit([NotEmpty] Guid id, EditPageRequest model)
    {
        var uid = await CommandMediator.SendAsync(new UpdatePageCommand(id, model));

        cache.Remove(BlogCachePartition.Page.ToString(), model.Slug.ToLower());

        // Log activity
        await LogActivityAsync(
            EventType.PageUpdated,
            "Update Page",
            model.Title,
            new { PageId = uid, model.Slug, model.IsPublished });

        return Ok(new { PageId = uid });
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(id));
        if (page == null) return NotFound();

        await CommandMediator.SendAsync(new DeletePageCommand(id));

        cache.Remove(BlogCachePartition.Page.ToString(), page.Slug);

        // Log activity
        await LogActivityAsync(
            EventType.PageDeleted,
            "Delete Page",
            page.Title,
            new { PageId = id, page.Slug });

        return NoContent();
    }

    [HttpGet("segment/list")]
    public async Task<IActionResult> GetPageSegmentList()
    {
        var segments = await queryMediator.QueryAsync(new ListPageSegmentsQuery());
        return Ok(segments);
    }

    [HttpGet("{id:guid}")]
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