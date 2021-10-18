using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Auth;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Admin;

public class LocalAccountModel : PageModel
{
    private readonly IMediator _mediator;

    public EditAccountRequest EditAccountRequest { get; set; }

    public IReadOnlyList<Account> Accounts { get; set; }

    public LocalAccountModel(IMediator mediator)
    {
        _mediator = mediator;
        EditAccountRequest = new();
    }

    public async Task OnGet()
    {
        Accounts = await _mediator.Send(new GetAccountsQuery());
    }
}