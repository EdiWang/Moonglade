using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Pages.Admin;

public class LocalAccountModel(IMediator mediator) : PageModel
{
    public IReadOnlyList<LoginHistoryEntity> LoginHistoryList { get; set; }

    public async Task OnGet() => LoginHistoryList = await mediator.Send(new GetLoginHistoryQuery());
}