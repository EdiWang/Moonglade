using LiteBus.Queries.Abstractions;
using Moonglade.Widgets;

namespace Moonglade.Web.ViewComponents;

public class WidgetsViewComponent(
    IQueryMediator queryMediator,
    ICacheAside cache,
    IConfiguration configuration) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int minOrder = int.MinValue, int maxOrder = int.MaxValue)
    {
        var widgets = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "widgets", async p =>
        {
            var exp = int.Parse(configuration["WidgetCacheMinutes"]);
            p.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(exp);

            var data = await queryMediator.QueryAsync(new ListWidgetsQuery());
            return data;
        });

        var filtered = widgets
            .Where(w => w.IsEnabled && w.DisplayOrder >= minOrder && w.DisplayOrder <= maxOrder)
            .OrderBy(w => w.DisplayOrder)
            .ToList();

        return View(filtered);
    }
}
