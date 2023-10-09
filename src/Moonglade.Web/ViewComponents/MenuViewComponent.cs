namespace Moonglade.Web.ViewComponents;

public class MenuViewComponent : ViewComponent
{
    private readonly ILogger<MenuViewComponent> _logger;
    private readonly IBlogConfig _blogConfig;

    public MenuViewComponent(IBlogConfig blogConfig, ILogger<MenuViewComponent> logger)
    {
        _blogConfig = blogConfig;
        _logger = logger;
    }

    public IViewComponentResult Invoke()
    {
        try
        {
            var settings = _blogConfig.CustomMenuSettings;
            return View(settings);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}