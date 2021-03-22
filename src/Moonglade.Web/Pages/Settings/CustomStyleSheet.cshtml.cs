using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class CustomStyleSheetModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public CustomStyleSheetSettingsViewModel ViewModel { get; set; }

        public CustomStyleSheetModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.CustomStyleSheetSettings;
            ViewModel = new()
            {
                EnableCustomCss = settings.EnableCustomCss,
                CssCode = settings.CssCode
            };
        }
    }
}
