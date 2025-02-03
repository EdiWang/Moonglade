using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Pingback;

namespace Moonglade.Web.Pages;

[AddPingbackHeader("pingback")]
public class PostModel(IMediator mediator) : PageModel
{
    public PostEntity Post { get; set; }

    public PostViewEntity PostView { get; set; }

    public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
    {
        if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var post = await mediator.Send(new GetPostBySlugQuery(year, month, day, slug));

        if (post is null) return NotFound();

        await mediator.Send(new AddRequestCountCommand(post.Id));

        Post = post;
        ViewData["TitlePrefix"] = $"{Post.Title}";

        PostView = await mediator.Send(new GetPostViewQuery(post.Id));

        return Page();
    }
}