using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Moonglade.Web.Pages.Settings;

public class AppearanceModel(IBlogConfig blogConfig) : PageModel
{
    public AppearanceSettings ViewModel { get; set; }

    public CreateThemeRequest ThemeRequest { get; set; }

    public void OnGet()
    {
        ViewModel = blogConfig.AppearanceSettings;
    }
}