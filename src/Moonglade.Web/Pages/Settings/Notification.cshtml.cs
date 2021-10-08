using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;

namespace Moonglade.Web.Pages.Settings
{
    public class NotificationModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public NotificationSettings ViewModel { get; set; }

        public NotificationModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            ViewModel = _blogConfig.NotificationSettings;
        }
    }
}
