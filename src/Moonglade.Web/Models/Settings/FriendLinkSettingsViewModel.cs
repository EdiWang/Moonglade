using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models.Settings
{
    public class FriendLinkSettingsViewModelWrap
    {
        public FriendLinkSettingsViewModel FriendLinkSettingsViewModel { get; set; }

        public IReadOnlyList<Model.FriendLink> FriendLinks { get; set; }
    }

    public class FriendLinkSettingsViewModel
    {
        [Display(Name = "Show Friend Links Section")]
        public bool ShowFriendLinksSection { get; set; }
    }
}
