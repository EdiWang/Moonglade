using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Core.TagFeature;

namespace Moonglade.Web.ViewComponents;

public class TagsViewComponent(IBlogConfig blogConfig, IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var tags = await queryMediator.QueryAsync(new GetHotTagsQuery(blogConfig.GeneralSettings.HotTagAmount));
        return View(tags);
    }
}