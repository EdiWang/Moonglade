using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;

namespace Moonglade.Web.Pages.Settings
{
    public class AdvancedModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public AdvancedSettings ViewModel { get; set; }

        public AdvancedModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            ViewModel = _blogConfig.AdvancedSettings;
        }
    }
}
