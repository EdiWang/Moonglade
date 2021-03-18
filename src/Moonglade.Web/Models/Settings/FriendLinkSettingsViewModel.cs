using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class FriendLinkSettingsViewModel
    {
        [Display(Name = "Show Friend Links Section")]
        public bool ShowFriendLinksSection { get; set; }
    }
}
