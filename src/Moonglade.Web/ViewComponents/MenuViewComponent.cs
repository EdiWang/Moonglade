namespace Moonglade.Web.ViewComponents;

public class MenuViewComponent : ViewComponent
{
    private readonly IBlogConfig _blogConfig;

    public MenuViewComponent(IBlogConfig blogConfig)
    {
        _blogConfig = blogConfig;
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
            return Content(e.Message);
        }
    }
}