using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Comments;
using System.Threading.Tasks;
using X.PagedList;

namespace Moonglade.Web.Pages.Admin
{
    public class CommentsModel : PageModel
    {
        private readonly ICommentService _commentService;
        private readonly IMediator _mediator;

        public StaticPagedList<CommentDetailedItem> CommentDetailedItems { get; set; }

        public CommentsModel(ICommentService commentService, IMediator mediator)
        {
            _commentService = commentService;
            _mediator = mediator;
        }

        public async Task OnGet(int pageIndex = 1)
        {
            const int pageSize = 10;
            var comments = await _mediator.Send(new GetCommentsQuery(pageSize, pageIndex));
            CommentDetailedItems = new(comments, pageIndex, pageSize, _commentService.Count());
        }
    }
}
