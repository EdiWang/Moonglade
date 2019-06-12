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
        // TODO: Cache
        [Route("/manifest.json")]
        public IActionResult Manifest()
        {
            var model = new ManifestModel
            {
                ShortName = _blogConfig.GeneralSettings.SiteTitle,
                Name = _blogConfig.GeneralSettings.SiteTitle,
                StartUrl = "/",
                Icons = new List<ManifestIcon>()
                {
                    // TODO: Refact code
                    new ManifestIcon
                    {
                        Src = "/android-icon-36x36.png",
                        Sizes = "36x36",
                        Type = "image/png",
                        Density = "0.75"
                    },
                    new ManifestIcon
                    {
                        Src = "/android-icon-48x48.png",
                        Sizes = "48x48",
                        Type = "image/png",
                        Density = "1.0"
                    },
                    new ManifestIcon
                    {
                        Src = "/android-icon-72x72.png",
                        Sizes = "72x72",
                        Type = "image/png",
                        Density = "1.5"
                    },
                    new ManifestIcon
                    {
                        Src = "/android-icon-96x96.png",
                        Sizes = "96x96",
                        Type = "image/png",
                        Density = "2.0"
                    },
                    new ManifestIcon
                    {
                        Src = "/android-icon-144x144.png",
                        Sizes = "144x144",
                        Type = "image/png",
                        Density = "3.0"
                    },
                    new ManifestIcon
                    {
                        Src = "/android-icon-192x192.png",
                        Sizes = "192x192",
                        Type = "image/png",
                        Density = "4.0"
                    }
                },
                BackgroundColor = "#2a579a",
                ThemeColor = "#2a579a",
                Display = "standalone",
                Orientation = "portrait"
            };
            return Json(model);
        }
    }
}