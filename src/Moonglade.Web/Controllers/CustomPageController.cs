using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("page")]
    public class CustomPageController : MoongladeController
    {
        private readonly CustomPageService _customPageService;

        public CustomPageController(
            ILogger<CustomPageController> logger,
            IOptions<AppSettings> settings, 
            CustomPageService customPageService) : base(logger, settings)
        {
            _customPageService = customPageService;
        }

        [Route("{routeName}")]
        public async Task<IActionResult> Index(string routeName)
        {
            if (string.IsNullOrWhiteSpace(routeName))
            {
                return BadRequest();
            }

            var response = await _customPageService.GetPageAsync(routeName);
            if (response.IsSuccess)
            {
                if (response.Item == null)
                {
                    return NotFound();
                }

                return View(response.Item);
            }
            return ServerError();
        }
    }
}