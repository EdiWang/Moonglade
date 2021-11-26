using Microsoft.AspNetCore.Mvc.RazorPages;
using X.PagedList;

namespace Moonglade.Web.Pages.Admin;

public class CommentsModel : PageModel
{
    private readonly IMediator _mediator;

    public StaticPagedList<CommentDetailedItem> CommentDetailedItems { get; set; }

    public CommentsModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGet(int pageIndex = 1)
    {
        const int pageSize = 10;
        var comments = await _mediator.Send(new GetCommentsQuery(pageSize, pageIndex));
        var count = await _mediator.Send(new CountCommentsQuery());
        CommentDetailedItems = new(comments, pageIndex, pageSize, count);
    }
}