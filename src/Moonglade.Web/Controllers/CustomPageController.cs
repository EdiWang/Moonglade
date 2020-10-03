using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Core.Caching;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Route("page")]
    public class CustomPageController : BlogController
    {
        private readonly IBlogCache _cache;
        private readonly CustomPageService _customPageService;
        private static string[] InvalidPageRouteNames => new[] { "index", "manage" };

        public CustomPageController(
            ILogger<CustomPageController> logger,
            IOptions<AppSettings> settings,
            IBlogCache cache,
            CustomPageService customPageService) : base(logger, settings)
        {
            _cache = cache;
            _customPageService = customPageService;
        }

        [HttpGet("{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> Index(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

            var pageResponse = await _cache.GetOrCreateAsync(CacheDivision.Page, slug.ToLower(), async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(AppSettings.CacheSlidingExpirationMinutes["Page"]);

                var response = await _customPageService.GetAsync(slug);
                return response;
            });

            if (!pageResponse.IsSuccess) return ServerError();

            if (pageResponse.Item == null)
            {
                Logger.LogWarning($"Page not found. {nameof(slug)}: '{slug}'");
                return NotFound();
            }

            if (!pageResponse.Item.IsPublished) return NotFound();

            return View(pageResponse.Item);
        }

        [Authorize]
        [Route("preview/{pageId}")]
        public async Task<IActionResult> Preview(Guid pageId)
        {
            var response = await _customPageService.GetAsync(pageId);
            if (!response.IsSuccess) return ServerError(response.Message);

            var page = response.Item;
            if (page == null)
            {
                Logger.LogWarning($"Page not found, parameter '{pageId}'.");
                return NotFound();
            }

            ViewBag.IsDraftPreview = true;
            return View("Index", page);
        }

        [Authorize]
        [HttpGet("manage")]
        public async Task<IActionResult> Manage()
        {
            var response = await _customPageService.ListSegmentAsync();
            return response.IsSuccess ? View("~/Views/Admin/ManageCustomPage.cshtml", response.Item) : ServerError();
        }

        [Authorize]
        [HttpGet("manage/create")]
        public IActionResult Create()
        {
            var model = new CustomPageEditViewModel();
            return View("CreateOrEdit", model);
        }

        [Authorize]
        [HttpGet("manage/edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var response = await _customPageService.GetAsync(id);
            if (!response.IsSuccess) return ServerError();
            if (response.Item == null) return NotFound();

            var model = new CustomPageEditViewModel
            {
                Id = response.Item.Id,
                Title = response.Item.Title,
                Slug = response.Item.Slug,
                MetaDescription = response.Item.MetaDescription,
                CssContent = response.Item.CssContent,
                RawHtmlContent = response.Item.RawHtmlContent,
                HideSidebar = response.Item.HideSidebar,
                IsPublished = response.Item.IsPublished
            };

            return View("CreateOrEdit", model);
        }

        [Authorize]
        [HttpPost("manage/createoredit")]
        public async Task<IActionResult> CreateOrEdit(CustomPageEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(new FailedResponse("Invalid ModelState"));
                }

                if (InvalidPageRouteNames.Contains(model.Slug.ToLower()))
                {
                    ModelState.AddModelError(nameof(model.Slug), "Reserved Slug.");
                    return View("CreateOrEdit", model);
                }

                var req = new EditCustomPageRequest(model.Id)
                {
                    HtmlContent = model.RawHtmlContent,
                    CssContent = model.CssContent,
                    HideSidebar = model.HideSidebar,
                    Slug = model.Slug,
                    MetaDescription = model.MetaDescription,
                    Title = model.Title,
                    IsPublished = model.IsPublished
                };

                var response = model.Id == Guid.Empty ?
                    await _customPageService.CreateAsync(req) :
                    await _customPageService.UpdateAsync(req);

                if (response.IsSuccess)
                {
                    Logger.LogInformation($"User '{User.Identity.Name}' updated custom page id '{response.Item}'");
                    _cache.Remove(CacheDivision.Page, req.Slug.ToLower());

                    return Json(new { PageId = response.Item });
                }

                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new FailedResponse(response.Message));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Create or Edit CustomPage.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(new FailedResponse(e.Message));
            }
        }

        [Authorize]
        [HttpPost("manage/delete")]
        public async Task<IActionResult> Delete(Guid pageId, string slug)
        {
            try
            {
                var response = await _customPageService.DeleteAsync(pageId);
                if (!response.IsSuccess) return ServerError();

                _cache.Remove(CacheDivision.Page, slug.ToLower());
                return Json(pageId);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error Delete CustomPage, Id: {pageId}.");
                return ServerError();
            }
        }
    }
}