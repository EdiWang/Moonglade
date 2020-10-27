using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class SecuritySettingsViewModel
    {
        [Display(Name = "Show warning when clicking external links")]
        public bool WarnExternalLink { get; set; }

        [Display(Name = "Allow javascript in pages")]
        public bool AllowScriptsInPage { get; set; }

        [Display(Name = "Show Admin login button under sidebar")]
        public bool ShowAdminLoginButton { get; set; }

        [Display(Name = "Enable raw endpoint for posts")]
        public bool EnablePostRawEndpoint { get; set; }
    }
}
