using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Theme;

namespace Moonglade.Web.Pages.Settings;

public class GeneralModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    private readonly ITimeZoneResolver _timeZoneResolver;
    private readonly IMediator _mediator;

    public GeneralSettings ViewModel { get; set; }

    public CreateThemeRequest ThemeRequest { get; set; }

    public IReadOnlyList<ThemeSegment> Themes { get; set; }

    public GeneralModel(IBlogConfig blogConfig, ITimeZoneResolver timeZoneResolver, IMediator mediator)
    {
        _blogConfig = blogConfig;
        _timeZoneResolver = timeZoneResolver;
        _mediator = mediator;
    }

    public async Task OnGetAsync()
    {
        ViewModel = _blogConfig.GeneralSettings;
        ViewModel.SelectedUtcOffset = _timeZoneResolver.GetTimeSpanByZoneId(_blogConfig.GeneralSettings.TimeZoneId);

        Themes = await _mediator.Send(new GetAllThemeSegmentQuery());
    }
}