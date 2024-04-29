using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Pages;

[Authorize]
public class PostPreviewModel(IMediator mediator) : PageModel
{
    public PostEntity Post { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid postId)
    {
        var post = await mediator.Send(new GetDraftQuery(postId));
        if (post is null) return NotFound();

        ViewData["TitlePrefix"] = $"{post.Title}";

        Post = post;
        return Page();
    }
}