using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Moonglade.Web.Pages.Admin;

public class LocalAccountModel : PageModel
{
    private readonly IMediator _mediator;

    public EditAccountRequest ViewModel { get; set; }

    public IReadOnlyList<Account> Accounts { get; set; }

    public LocalAccountModel(IMediator mediator)
    {
        _mediator = mediator;
        ViewModel = new();
    }

    public async Task OnGet()
    {
        Accounts = await _mediator.Send(new GetAccountsQuery());
    }
}