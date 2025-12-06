using LiteBus.Queries.Abstractions;
using Moonglade.Widgets;

namespace Moonglade.Web.ViewComponents;

public class WidgetsViewComponent(
    IQueryMediator queryMediator,
    ICacheAside cache,
    IConfiguration configuration) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var widgets = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "widgets", async p =>
        {
            var exp = int.Parse(configuration["WidgetCacheMinutes"]);
            p.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(exp);

            var data = await queryMediator.QueryAsync(new ListWidgetsQuery());
            return data;
        });

        return View(widgets);
    }
}
