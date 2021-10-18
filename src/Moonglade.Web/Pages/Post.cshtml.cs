using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Pingback;

namespace Moonglade.Web.Pages;

[AddPingbackHeader("pingback")]
public class PostModel : PageModel
{
    private readonly IMediator _mediator;

    public Post Post { get; set; }

    public PostModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
    {
        if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var slugInfo = new PostSlug(year, month, day, slug);
        var post = await _mediator.Send(new GetPostBySlugQuery(slugInfo));

        if (post is null) return NotFound();

        ViewData["TitlePrefix"] = $"{post.Title}";

        Post = post;
        return Page();
    }
}