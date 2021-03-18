using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Comments;
using X.PagedList;

namespace Moonglade.Web.Pages.Admin
{
    public class CommentsModel : PageModel
    {
        private readonly ICommentService _commentService;

        public StaticPagedList<CommentDetailedItem> CommentDetailedItems { get; set; }

        public CommentsModel(ICommentService commentService)
        {
            _commentService = commentService;
        }

        public async Task OnGet(int pageIndex = 1)
        {
            const int pageSize = 10;
            var comments = await _commentService.GetCommentsAsync(pageSize, pageIndex);
            CommentDetailedItems = new(comments, pageIndex, pageSize, _commentService.Count());
        }
    }
}
