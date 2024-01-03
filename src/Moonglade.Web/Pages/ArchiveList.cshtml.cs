using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;

namespace Moonglade.Web.Pages;

public class ArchiveListModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<PostDigest> Posts { get; set; }

    public string ArchiveInfo { get; set; }

    public async Task<IActionResult> OnGetAsync(int year, int? month)
    {
        if (year < DateTime.MinValue.Year || year > DateTime.UtcNow.Year) return BadRequest();
        if (month is > 12 or < 0) return BadRequest();

        IReadOnlyList<PostDigest> model;

        if (month is not null)
        {
            // {year}/{month}
            ArchiveInfo = new DateTime(year, month.Value, 1).ToString("MMMM yyyy");
            model = await mediator.Send(new ListArchiveQuery(year, month));
        }
        else
        {
            // {year}
            ArchiveInfo = $"{year}";
            model = await mediator.Send(new ListArchiveQuery(year));
        }

        Posts = model;
        return Page();
    }
}