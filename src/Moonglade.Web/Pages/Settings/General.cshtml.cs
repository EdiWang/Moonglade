using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Moonglade.Web.Pages.Settings;

public class GeneralModel(IBlogConfig blogConfig, ITimeZoneResolver timeZoneResolver) : PageModel
{
    public GeneralSettings ViewModel { get; set; }

    public CreateThemeRequest ThemeRequest { get; set; }

    public void OnGet()
    {
        ViewModel = blogConfig.GeneralSettings;
        ViewModel.SelectedUtcOffset = timeZoneResolver.GetTimeSpanByZoneId(blogConfig.GeneralSettings.TimeZoneId);
    }
}