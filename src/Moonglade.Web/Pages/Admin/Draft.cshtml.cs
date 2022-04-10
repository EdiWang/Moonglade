using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin;

public class DraftModel : PageModel
{
    private readonly IMediator _mediator;

    public IReadOnlyList<PostSegment> PostSegments { get; set; }

    public DraftModel(IMediator mediator) => _mediator = mediator;

    public async Task OnGet() => PostSegments = await _mediator.Send(new ListPostSegmentByStatusQuery(PostStatus.Draft));
}