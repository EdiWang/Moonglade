using Moonglade.Core.TagFeature;

namespace Moonglade.Web.ViewComponents;

public class HotTagsViewComponent : ViewComponent
{
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;

    public HotTagsViewComponent(IBlogConfig blogConfig, IMediator mediator)
    {
        _blogConfig = blogConfig;
        _mediator = mediator;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var tags = await _mediator.Send(new GetHotTagsQuery(_blogConfig.ContentSettings.HotTagAmount));
            return View(tags);
        }
        catch (Exception e)
        {
            return Content(e.Message);
        }
    }
}