using LiteBus.Queries.Abstractions;
using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.ViewComponents;

public class CategoryListViewComponent(IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
    {
        var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());
        return isMenu ? View("Menu", cats) : View(cats);
    }
}