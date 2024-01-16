using Moonglade.Core.TagFeature;

namespace Moonglade.Web.ViewComponents;

public class TagsViewComponent(IBlogConfig blogConfig, IMediator mediator, ILogger<SubListViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var tags = await mediator.Send(new GetHotTagsQuery(blogConfig.GeneralSettings.HotTagAmount));
            return View(tags);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}