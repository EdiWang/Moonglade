using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;

namespace Moonglade.Web.Pages;

[Authorize]
public class PostPreviewModel(IMediator mediator) : PageModel
{
    public Post Post { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid postId)
    {
        var post = await mediator.Send(new GetDraftQuery(postId));
        if (post is null) return NotFound();

        ViewData["TitlePrefix"] = $"{post.Title}";

        Post = post;
        return Page();
    }
}