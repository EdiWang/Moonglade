using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class ImageModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public ImageSettingsViewModel ViewModel { get; set; }

        public ImageModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.WatermarkSettings;
            ViewModel = new()
            {
                IsEnabled = settings.IsEnabled,
                KeepOriginImage = settings.KeepOriginImage,
                FontSize = settings.FontSize,
                WatermarkText = settings.WatermarkText
            };
        }
    }
}
