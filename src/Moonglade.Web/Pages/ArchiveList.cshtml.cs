using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;

namespace Moonglade.Web.Pages;

public class ArchiveListModel : PageModel
{
    private readonly IMediator _mediator;

    public IReadOnlyList<PostDigest> Posts { get; set; }

    public ArchiveListModel(IMediator mediator) => _mediator = mediator;

    public async Task<IActionResult> OnGetAsync(int year, int? month)
    {
        if (year < DateTime.MinValue.Year || year > DateTime.UtcNow.Year) return BadRequest();
        if (month is > 12 or < 0) return BadRequest();

        IReadOnlyList<PostDigest> model;

        if (month is not null)
        {
            // {year}/{month}
            ViewData["ArchiveInfo"] = $"{year}.{month}";
            model = await _mediator.Send(new ListArchiveQuery(year, month));
        }
        else
        {
            // {year}
            ViewData["ArchiveInfo"] = $"{year}";
            model = await _mediator.Send(new ListArchiveQuery(year));
        }

        Posts = model;
        return Page();
    }
}