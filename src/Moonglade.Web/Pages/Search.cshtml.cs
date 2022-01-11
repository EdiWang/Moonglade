using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Pages;

[AddXRobotsTag("noindex, nofollow")]
public class SearchModel : PageModel
{
    private readonly IMediator _mediator;

    public SearchModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public IReadOnlyList<PostDigest> Posts { get; set; }

    public async Task<IActionResult> OnGetAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return RedirectToPage("Index");
        }

        ViewData["TitlePrefix"] = term;

        var posts = await _mediator.Send(new SearchPostQuery(term));
        Posts = posts;

        return Page();
    }
}