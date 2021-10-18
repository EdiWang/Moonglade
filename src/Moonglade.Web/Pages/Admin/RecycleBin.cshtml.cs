using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin;

public class RecycleBinModel : PageModel
{
    private readonly IMediator _mediator;

    public IReadOnlyList<PostSegment> Posts { get; set; }

    public RecycleBinModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGet()
    {
        Posts = await _mediator.Send(new ListPostSegmentByStatusQuery(PostStatus.Deleted));
    }
}