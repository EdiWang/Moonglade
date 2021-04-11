using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.FriendLink;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Admin
{
    public class FriendLinkModel : PageModel
    {
        private readonly IFriendLinkService _friendLinkService;
        private readonly IBlogConfig _blogConfig;

        public FriendLinkSettingsViewModel FriendLinkSettingsViewModel { get; set; }

        public FriendLinkEditModel FriendLinkEditViewModel { get; set; }

        public IReadOnlyList<Link> FriendLinks { get; set; }

        public FriendLinkModel(
            IFriendLinkService friendLinkService, IBlogConfig blogConfig)
        {
            _friendLinkService = friendLinkService;
            _blogConfig = blogConfig;
            FriendLinkEditViewModel = new();
        }

        public async Task OnGet()
        {
            FriendLinks = await _friendLinkService.GetAllAsync();
            FriendLinkSettingsViewModel = new()
            {
                ShowFriendLinksSection = _blogConfig.FriendLinksSettings.ShowFriendLinksSection
            };
        }
    }
}
