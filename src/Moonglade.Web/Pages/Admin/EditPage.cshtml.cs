using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;

namespace Moonglade.Web.Pages.Admin;

public class EditPageModel : PageModel
{
    public IActionResult OnGet(Guid? id)
    {
        return Page();
    }
}