using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Widgets;

namespace Moonglade.Web.ViewComponents;

public class WidgetsViewComponent(
    IQueryMediator queryMediator,
    ICacheAside cache,
    IConfiguration configuration) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(WidgetRegion region, IReadOnlyList<WidgetEntity> draft = null)
    {
        var widgets = draft ?? await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "widgets", async () =>
        {
            var data = await queryMediator.QueryAsync(new ListWidgetsQuery());
            return data;
        }, TimeSpan.FromMinutes(int.Parse(configuration["WidgetCacheMinutes"])));

        var filtered = widgets
            .Where(w => w.IsEnabled && w.Region == region)
            .ToList();

        return View("/Pages/Components/Widgets/Default.cshtml", filtered);
    }
}
