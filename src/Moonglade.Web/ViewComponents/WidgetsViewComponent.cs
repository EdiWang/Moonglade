using LiteBus.Queries.Abstractions;
using Moonglade.Widgets;

namespace Moonglade.Web.ViewComponents;

public class WidgetsViewComponent(IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var widgets = await queryMediator.QueryAsync(new ListWidgetsQuery());
        return View(widgets);
    }
}
