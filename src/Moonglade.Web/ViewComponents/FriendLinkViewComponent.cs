using LiteBus.Queries.Abstractions;
using Moonglade.FriendLink;

namespace Moonglade.Web.ViewComponents;

public class FriendLinkViewComponent(IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var links = await queryMediator.QueryAsync(new ListLinksQuery());
        return View(links ?? []);
    }
}