using Microsoft.AspNetCore.Mvc;

namespace Moonglade.Web.Controllers
{
    [Route("about")]
    [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 3600)]
    public class AboutController : MoongladeController
    {
        public IActionResult Index()
        {
            return View();
        }

        [Route("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}