using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Auth;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Admin
{
    public class LocalAccountModel : PageModel
    {
        private readonly ILocalAccountService _accountService;

        public AccountEditViewModel AccountEditViewModel { get; set; }

        public IReadOnlyList<Account> Accounts { get; set; }

        public LocalAccountModel(ILocalAccountService accountService)
        {
            _accountService = accountService;
            AccountEditViewModel = new();
        }

        public async Task OnGet()
        {
            Accounts = await _accountService.GetAllAsync();
        }
    }
}
