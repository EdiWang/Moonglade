using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Pingback;

namespace Moonglade.Web.Pages;

[AddPingbackHeader("pingback")]
public class PostModel(IMediator mediator) : PageModel
{
    public Post Post { get; set; }

    public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
    {
        if (year > DateTime.UtcNow.Year || month is < 1 or > 12 || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var slugInfo = new PostSlug(year, month, day, slug);
        var post = await mediator.Send(new GetPostBySlugQuery(slugInfo));

        if (post is null) return NotFound();

        ViewData["TitlePrefix"] = $"{post.Title}";

        Post = post;
        return Page();
    }
}