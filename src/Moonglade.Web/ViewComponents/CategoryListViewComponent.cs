using LiteBus.Queries.Abstractions;
using Moonglade.Features.Category;

namespace Moonglade.Web.ViewComponents;

public class CategoryListViewComponent(IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
    {
        var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());
        return View(isMenu ? "/Pages/Components/CategoryList/Menu.cshtml" : "/Pages/Components/CategoryList/Default.cshtml", cats);
    }
}
