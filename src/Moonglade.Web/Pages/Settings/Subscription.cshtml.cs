using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class SubscriptionModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;

        public SubscriptionSettingsViewModel ViewModel { get; set; }

        public SubscriptionModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.FeedSettings;
            ViewModel = new()
            {
                AuthorName = settings.AuthorName,
                RssCopyright = settings.RssCopyright,
                RssItemCount = settings.RssItemCount,
                RssTitle = settings.RssTitle,
                UseFullContent = settings.UseFullContent
            };
        }
    }
}
