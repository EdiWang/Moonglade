using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.ViewComponents;

public class CategoryListViewComponent(IMediator mediator, ILogger<CategoryListViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
    {
        try
        {
            var cats = await mediator.Send(new GetCategoriesQuery());
            return isMenu ? View("Menu", cats) : View(cats);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error GetCategoriesQuery()");
            return Content("ERROR");
        }
    }
}