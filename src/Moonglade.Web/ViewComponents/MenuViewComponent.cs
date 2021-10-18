using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Menus;

namespace Moonglade.Web.ViewComponents;

public class MenuViewComponent : ViewComponent
{
    private readonly IBlogCache _cache;
    private readonly IMediator _mediator;

    public MenuViewComponent(IBlogCache cache, IMediator mediator)
    {
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var menus = await _cache.GetOrCreateAsync(CacheDivision.General, "menu", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                var items = await _mediator.Send(new GetAllMenusQuery());
                return items;
            });

            return View(menus);
        }
        catch (Exception e)
        {
            return Content(e.Message);
        }
    }
}