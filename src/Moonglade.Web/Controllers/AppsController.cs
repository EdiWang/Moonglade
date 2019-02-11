using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Controllers
{
    [Route("apps")]
    public class AppsController : MoongladeController
    {
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 3600)]
        public IActionResult Index()
        {
            return View();
        }
    }
}