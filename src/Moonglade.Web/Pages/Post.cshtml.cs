using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Pingback;

namespace Moonglade.Web.Pages;

[AddPingbackHeader("pingback")]
public class PostModel(IConfiguration configuration, IMediator mediator) : PageModel
{
    public PostEntity Post { get; set; }

    public PostViewEntity PostView { get; set; }

    public bool IsViewCountEnabled { get; } = configuration.GetValue<bool>("Post:EnableViewCount");

    public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
    {
        if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var post = await mediator.Send(new GetPostBySlugQuery(year, month, day, slug));

        if (post is null) return NotFound();

        Post = post;
        ViewData["TitlePrefix"] = $"{Post.Title}";

        if (IsViewCountEnabled)
        {
            await mediator.Send(new AddRequestCountCommand(post.Id));
            PostView = await mediator.Send(new GetPostViewQuery(post.Id));
        }

        return Page();
    }
}