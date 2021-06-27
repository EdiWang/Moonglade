using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Theme;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class GeneralModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        private readonly ITimeZoneResolver _timeZoneResolver;
        private readonly IThemeService _themeService;

        public GeneralSettingsViewModel ViewModel { get; set; }

        public CreateThemeRequest ThemeRequest { get; set; }

        public string[] Themes { get; set; }

        public GeneralModel(IBlogConfig blogConfig, ITimeZoneResolver timeZoneResolver, IThemeService themeService)
        {
            _blogConfig = blogConfig;
            _timeZoneResolver = timeZoneResolver;
            _themeService = themeService;
        }

        public async Task OnGetAsync()
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
                SelectedThemeFileName = _blogConfig.GeneralSettings.ThemeName,
                AutoDarkLightTheme = _blogConfig.GeneralSettings.AutoDarkLightTheme
            };

            Themes = (await _themeService.GetAllNames()).ToArray();
        }
    }
}
