using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.ViewComponents;

public class CategoryListViewComponent : ViewComponent
{
    private readonly IMediator _mediator;

    public CategoryListViewComponent(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
    {
        try
        {
            var cats = await _mediator.Send(new GetCategoriesQuery());
            return isMenu ? View("CatMenu", cats) : View(cats);
        }
        catch (Exception e)
        {
            return Content(e.Message);
        }
    }
}