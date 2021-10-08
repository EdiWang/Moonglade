using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class SubscriptionModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;

        public FeedSettings ViewModel { get; set; }

        public SubscriptionModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            ViewModel = _blogConfig.FeedSettings;
        }
    }
}
