namespace Moonglade.Web.ViewComponents;

public class MenuViewComponent(IBlogConfig blogConfig, ILogger<MenuViewComponent> logger) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        try
        {
            var settings = blogConfig.CustomMenuSettings;
            return View(settings);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}