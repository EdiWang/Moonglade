namespace Moonglade.Web.ViewComponents;

public class SocialLinkViewComponent(IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var links = await mediator.Send(new GetAllSocialLinksQuery());
        return View(links ?? []);
    }
}