using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.ViewComponents;

public class CategoryListViewComponent(IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
    {
        var cats = await mediator.Send(new GetCategoriesQuery());
        return isMenu ? View("Menu", cats) : View(cats);
    }
}