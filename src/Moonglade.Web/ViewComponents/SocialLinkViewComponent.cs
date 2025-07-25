using LiteBus.Queries.Abstractions;

namespace Moonglade.Web.ViewComponents;

public class SocialLinkViewComponent(IQueryMediator queryMediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var links = await queryMediator.QueryAsync(new GetAllSocialLinksQuery());
        return View(links ?? []);
    }
}