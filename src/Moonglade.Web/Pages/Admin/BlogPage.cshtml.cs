using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;

namespace Moonglade.Web.Pages.Admin;

public class BlogPageModel : PageModel
{
    private readonly IMediator _mediator;

    public IReadOnlyList<PageSegment> PageSegments { get; set; }

    public BlogPageModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGet()
    {
        PageSegments = await _mediator.Send(new ListPageSegmentQuery());
    }
}