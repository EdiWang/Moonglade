using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages.Admin;

public class CommentsModel(IMediator mediator) : PageModel
{
    public BasePagedList<CommentDetailedItem> CommentDetailedItems { get; set; }

    public async Task OnGet(int pageIndex = 1)
    {
        const int pageSize = 5;
        var comments = await mediator.Send(new GetCommentsQuery(pageSize, pageIndex));
        var count = await mediator.Send(new CountCommentsQuery());
        CommentDetailedItems = new(comments, pageIndex, pageSize, count);
    }
}