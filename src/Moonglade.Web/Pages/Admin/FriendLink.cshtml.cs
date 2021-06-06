using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.FriendLink;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Admin
{
    public class FriendLinkModel : PageModel
    {
        private readonly IFriendLinkService _friendLinkService;

        public FriendLinkEditModel FriendLinkEditViewModel { get; set; }

        public IReadOnlyList<Link> FriendLinks { get; set; }

        public FriendLinkModel(IFriendLinkService friendLinkService)
        {
            _friendLinkService = friendLinkService;
            FriendLinkEditViewModel = new();
        }

        public async Task OnGet()
        {
            FriendLinks = await _friendLinkService.GetAllAsync();
        }
    }
}
