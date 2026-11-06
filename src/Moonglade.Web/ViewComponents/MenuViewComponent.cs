using Moonglade.Web.Rendering;

namespace Moonglade.Web.ViewComponents;

public class MenuViewComponent(IBlogConfig blogConfig, ILogger<MenuViewComponent> logger) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var settings = ViewData.CaptureSettings(blogConfig);
        try
        {
            return View(settings.CustomMenuSettings);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Content("ERROR");
        }
    }
}
