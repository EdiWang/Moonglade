using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
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
        // [Route("/manifest.json")]
        public IActionResult Manifest()
        {
            var model = new ManifestModel
            {
                ShortName = _blogConfig.GeneralSettings.SiteTitle,
                Name = _blogConfig.GeneralSettings.SiteTitle,
                StartUrl = "/",
                Icons = new List<ManifestIcon>()
                {
                    // TODO
                },
                // BackgroundColor = "#",
                // ThemeColor = "#",
                Display = "standalone",
                Orientation = "portrait"
            };
            return Json(model);
        }
    }
}