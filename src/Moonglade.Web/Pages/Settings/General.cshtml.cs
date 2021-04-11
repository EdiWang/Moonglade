using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class GeneralModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        private readonly ITimeZoneResolver _timeZoneResolver;
        public GeneralSettingsViewModel ViewModel { get; set; }

        public GeneralModel(IBlogConfig blogConfig, ITimeZoneResolver timeZoneResolver)
        {
            _blogConfig = blogConfig;
            _timeZoneResolver = timeZoneResolver;
        }

        public void OnGet()
        {
            ViewModel = new()
            {
                LogoText = _blogConfig.GeneralSettings.LogoText,
                MetaKeyword = _blogConfig.GeneralSettings.MetaKeyword,
                MetaDescription = _blogConfig.GeneralSettings.MetaDescription,
                CanonicalPrefix = _blogConfig.GeneralSettings.CanonicalPrefix,
                SiteTitle = _blogConfig.GeneralSettings.SiteTitle,
                Copyright = _blogConfig.GeneralSettings.Copyright,
                SideBarCustomizedHtmlPitch = _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch,
                SideBarOption = _blogConfig.GeneralSettings.SideBarOption.ToString(),
                FooterCustomizedHtmlPitch = _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch,
                OwnerName = _blogConfig.GeneralSettings.OwnerName,
                OwnerEmail = _blogConfig.GeneralSettings.OwnerEmail,
                OwnerDescription = _blogConfig.GeneralSettings.Description,
                OwnerShortDescription = _blogConfig.GeneralSettings.ShortDescription,
                SelectedTimeZoneId = _blogConfig.GeneralSettings.TimeZoneId,
                SelectedUtcOffset = _timeZoneResolver.GetTimeSpanByZoneId(_blogConfig.GeneralSettings.TimeZoneId),
                SelectedThemeFileName = _blogConfig.GeneralSettings.ThemeFileName,
                AutoDarkLightTheme = _blogConfig.GeneralSettings.AutoDarkLightTheme
            };
        }
    }
}
