using LiteBus.Queries.Abstractions;
using Moonglade.Features.Category;

namespace Moonglade.Web.ViewComponents;

public class SubListViewComponent(ILogger<SubListViewComponent> logger, IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cats = await queryMediator.QueryAsync(new ListCategoriesQuery());
            var items = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.Slug));

            return View(items);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}