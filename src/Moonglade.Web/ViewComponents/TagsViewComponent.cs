using Moonglade.Web.Rendering;
using LiteBus.Queries.Abstractions;
using Moonglade.Features.Tag;

namespace Moonglade.Web.ViewComponents;

public class TagsViewComponent(IBlogConfig blogConfig, IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var settings = ViewData.CaptureSettings(blogConfig);
        var tags = await queryMediator.QueryAsync(new ListTopTagsQuery(settings.GeneralSettings.HotTagAmount));
        return View(tags);
    }
}
