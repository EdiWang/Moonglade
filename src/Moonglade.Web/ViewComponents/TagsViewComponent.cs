using Moonglade.Core.TagFeature;

namespace Moonglade.Web.ViewComponents;

public class TagsViewComponent : ViewComponent
{
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;
    private readonly ILogger<SubListViewComponent> _logger;

    public TagsViewComponent(IBlogConfig blogConfig, IMediator mediator, ILogger<SubListViewComponent> logger)
    {
        _blogConfig = blogConfig;
        _mediator = mediator;
        _logger = logger;
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
            _logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}