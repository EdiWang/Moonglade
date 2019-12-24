using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    public class AssetsController : MoongladeController
    {
        private readonly IBlogConfig _blogConfig;

        public AssetsController(
            ILogger<AssetsController> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig) : base(logger, settings)
        {
            _blogConfig = blogConfig;
        }

        // Credits: https://github.com/Anduin2017/Blog
        [ResponseCache(Duration = 3600)]
        [Route("/manifest.json")]
        public async Task<IActionResult> Manifest([FromServices]IWebHostEnvironment hostEnvironment)
        {
            var themeColor = await Utils.GetThemeColorAsync(hostEnvironment.WebRootPath, _blogConfig.GeneralSettings.ThemeFileName);

            var model = new ManifestModel
            {
                ShortName = _blogConfig.GeneralSettings.SiteTitle,
                Name = _blogConfig.GeneralSettings.SiteTitle,
                Description = _blogConfig.GeneralSettings.SiteTitle,
                StartUrl = "/",
                Icons = new List<ManifestIcon>
                {
                    new ManifestIcon("/android-icon-{0}.png",36,"0.75"),
                    new ManifestIcon("/android-icon-{0}.png",48,"1.0"),
                    new ManifestIcon("/android-icon-{0}.png",72,"1.5"),
                    new ManifestIcon("/android-icon-{0}.png",96,"2.0"),
                    new ManifestIcon("/android-icon-{0}.png",144,"3.0"),
                    new ManifestIcon("/android-icon-{0}.png",192,"4.0")
                },
                BackgroundColor = themeColor,
                ThemeColor = themeColor,
                Display = "standalone",
                Orientation = "portrait"
            };
            return Json(model);
        }
    }
}