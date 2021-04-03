using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class AdvancedModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public AdvancedSettingsViewModel ViewModel { get; set; }

        public AdvancedModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.AdvancedSettings;
            ViewModel = new()
            {
                DNSPrefetchEndpoint = settings.DNSPrefetchEndpoint,
                RobotsTxtContent = settings.RobotsTxtContent,
                EnablePingbackSend = settings.EnablePingBackSend,
                EnablePingbackReceive = settings.EnablePingBackReceive,
                EnableOpenGraph = settings.EnableOpenGraph,
                EnableCDNRedirect = settings.EnableCDNRedirect,
                EnableOpenSearch = settings.EnableOpenSearch,
                EnableMetaWeblog = settings.EnableMetaWeblog,
                CDNEndpoint = settings.CDNEndpoint,
                FitImageToDevicePixelRatio = settings.FitImageToDevicePixelRatio
            };
        }
    }
}
