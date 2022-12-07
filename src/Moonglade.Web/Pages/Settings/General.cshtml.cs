using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Moonglade.Web.Pages.Settings;

public class GeneralModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    private readonly ITimeZoneResolver _timeZoneResolver;

    public GeneralSettings ViewModel { get; set; }

    public CreateThemeRequest ThemeRequest { get; set; }

    public GeneralModel(IBlogConfig blogConfig, ITimeZoneResolver timeZoneResolver)
    {
        _blogConfig = blogConfig;
        _timeZoneResolver = timeZoneResolver;
    }

    public void OnGet()
    {
        ViewModel = _blogConfig.GeneralSettings;
        ViewModel.SelectedUtcOffset = _timeZoneResolver.GetTimeSpanByZoneId(_blogConfig.GeneralSettings.TimeZoneId);
    }
}