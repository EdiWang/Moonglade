using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Pages.Admin;

public class EditPageModel(IMediator mediator, IQueryMediator queryMediator) : PageModel
{
    public Guid PageId { get; set; }

    public EditPageRequest EditPageRequest { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id is null) return Page();

        var page = await queryMediator.QueryAsync(new GetPageByIdQuery(id.Value));
        if (page is null) return NotFound();

        StyleSheetEntity css = null;
        if (!string.IsNullOrWhiteSpace(page.CssId))
        {
            css = await mediator.Send(new GetStyleSheetQuery(Guid.Parse(page.CssId)));
        }

        PageId = page.Id;

        EditPageRequest = new()
        {
            Title = page.Title,
            Slug = page.Slug,
            MetaDescription = page.MetaDescription,
            CssContent = css?.CssContent,
            RawHtmlContent = page.HtmlContent,
            HideSidebar = page.HideSidebar,
            IsPublished = page.IsPublished
        };

        return Page();
    }
}