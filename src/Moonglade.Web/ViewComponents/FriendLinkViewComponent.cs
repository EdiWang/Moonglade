using Moonglade.FriendLink;

namespace Moonglade.Web.ViewComponents;

public class FriendLinkViewComponent(IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var links = await mediator.Send(new GetAllLinksQuery());
        return View(links ?? []);
    }
}