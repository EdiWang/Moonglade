using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Pages.Admin;

public class PagePreviewModel(IMediator mediator) : PageModel
{
    public PageEntity BlogPage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid pageId)
    {
        var page = await mediator.Send(new GetPageByIdQuery(pageId));
        if (page is null) return NotFound();

        BlogPage = page;
        return Page();
    }
}