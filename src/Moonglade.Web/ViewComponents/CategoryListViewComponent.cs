using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.ViewComponents;

public class CategoryListViewComponent : ViewComponent
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoryListViewComponent> _logger;

    public CategoryListViewComponent(IMediator mediator, ILogger<CategoryListViewComponent> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(bool isMenu)
    {
        try
        {
            var cats = await _mediator.Send(new GetCategoriesQuery());
            return isMenu ? View("Menu", cats) : View(cats);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error GetCategoriesQuery()");
            return Content(e.Message);
        }
    }
}