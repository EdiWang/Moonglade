using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.ViewComponents;

public class SubListViewComponent(ILogger<SubListViewComponent> logger, IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cats = await mediator.Send(new GetCategoriesQuery());
            var items = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));

            return View(items);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}