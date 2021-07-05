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
            var settings = _blogConfig.ImageSettings;
            ViewModel = new()
            {
                IsWatermarkEnabled = settings.IsWatermarkEnabled,
                KeepOriginImage = settings.KeepOriginImage,
                WatermarkFontSize = settings.WatermarkFontSize,
                WatermarkText = settings.WatermarkText,
                UseFriendlyNotFoundImage = settings.UseFriendlyNotFoundImage,
                FitImageToDevicePixelRatio = settings.FitImageToDevicePixelRatio
            };
        }
    }
}
