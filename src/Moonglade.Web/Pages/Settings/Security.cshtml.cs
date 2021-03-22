using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class SecurityModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public SecuritySettingsViewModel ViewModel { get; set; }

        public SecurityModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.SecuritySettings;
            ViewModel = new()
            {
                WarnExternalLink = settings.WarnExternalLink,
                AllowScriptsInPage = settings.AllowScriptsInPage,
                ShowAdminLoginButton = settings.ShowAdminLoginButton
            };
        }
    }
}
