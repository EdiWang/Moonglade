using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Auth;
using Moonglade.Web.Models.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class LocalAccountModel : PageModel
    {
        private readonly IMediator _mediator;

        public AccountEditModel AccountEditModel { get; set; }

        public IReadOnlyList<Account> Accounts { get; set; }

        public LocalAccountModel(IMediator mediator)
        {
            _mediator = mediator;
            AccountEditModel = new();
        }

        public async Task OnGet()
        {
            Accounts = await _mediator.Send(new GetAccountsQuery());
        }
    }
}
