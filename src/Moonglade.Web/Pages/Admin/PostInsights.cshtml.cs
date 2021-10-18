using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin;

public class PostInsightsModel : PageModel
{
    private readonly IMediator _mediator;

    public IReadOnlyList<PostSegment> TopReadList { get; set; }

    public IReadOnlyList<PostSegment> TopCommentedList { get; set; }

    public PostInsightsModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGet()
    {
        TopReadList = await _mediator.Send(new ListInsightsQuery(PostInsightsType.TopRead));
        TopCommentedList = await _mediator.Send(new ListInsightsQuery(PostInsightsType.TopCommented));
    }
}