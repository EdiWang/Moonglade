namespace Moonglade.Web.ViewComponents;

public class SocialLinkViewComponent(ILogger<SocialLinkViewComponent> logger, IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var links = await mediator.Send(new GetAllSocialLinksQuery());
            return View(links ?? []);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error Reading SocialLink.");
            return Content("ERROR");
        }
    }
}