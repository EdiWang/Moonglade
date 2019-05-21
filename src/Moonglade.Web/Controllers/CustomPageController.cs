using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

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

        [HttpGet("{routeName}")]
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

        [Authorize]
        [HttpGet("manage")]
        public async Task<IActionResult> Manage()
        {
            var response = await _customPageService.GetPagesMetaDataListAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }
            return ServerError();
        }

        [Authorize]
        [HttpGet("edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var response = await _customPageService.GetPageAsync(id);
            if (response.IsSuccess)
            {
                if (response.Item == null)
                {
                    return NotFound();
                }

                var model = new CustomPageEditViewModel
                {
                    Id = response.Item.Id,
                    Title = response.Item.Title,
                    RouteName = response.Item.RouteName,
                    CssContent = response.Item.CssContent,
                    RawHtmlContent = response.Item.RawHtmlContent,
                    HideSidebar = response.Item.HideSidebar
                };

                return View("CreateOrEdit", model);
            }
            return ServerError();
        }

        [Authorize]
        [HttpGet("create")]
        public IActionResult Create()
        {
            var model = new CustomPageEditViewModel();
            return View("CreateOrEdit", model);
        }
    }
}