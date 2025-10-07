using LiteBus.Queries.Abstractions;
using Moonglade.Features.Tag;

namespace Moonglade.Web.ViewComponents;

public class TagsViewComponent(IBlogConfig blogConfig, IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var tags = await queryMediator.QueryAsync(new ListTopTagsQuery(blogConfig.GeneralSettings.HotTagAmount));
        return View(tags);
    }
}