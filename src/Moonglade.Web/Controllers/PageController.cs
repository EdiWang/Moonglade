using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Pages;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using NUglify;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("page")]
    public class PageController : Controller
    {
        private readonly IBlogCache _cache;
        private readonly IPageService _pageService;
        private readonly ILogger<PageController> _logger;

        public PageController(
            IBlogCache cache,
            IPageService pageService,
            ILogger<PageController> logger)
        {
            _cache = cache;
            _pageService = pageService;
            _logger = logger;
        }

        [Route("preview/{pageId:guid}")]
        public async Task<IActionResult> Preview(Guid pageId)
        {
            var page = await _pageService.GetAsync(pageId);
            if (page is null) return NotFound();

            ViewBag.IsDraftPreview = true;

            return View("~/Views/Home/Page.cshtml", page);
        }

        [HttpGet("manage/create")]
        public IActionResult Create()
        {
            var model = new PageEditModel();
            return View("CreateOrEdit", model);
        }

        [HttpGet("manage/edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var page = await _pageService.GetAsync(id);
            if (page is null) return NotFound();

            var model = new PageEditModel
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                MetaDescription = page.MetaDescription,
                CssContent = page.CssContent,
                RawHtmlContent = page.RawHtmlContent,
                HideSidebar = page.HideSidebar,
                IsPublished = page.IsPublished
            };

            return View("CreateOrEdit", model);
        }

        [HttpPost("manage/createoredit")]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        public async Task<IActionResult> CreateOrEdit(PageEditModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                if (!string.IsNullOrWhiteSpace(model.CssContent))
                {
                    var uglifyTest = Uglify.Css(model.CssContent);
                    if (uglifyTest.HasErrors)
                    {
                        foreach (var err in uglifyTest.Errors)
                        {
                            ModelState.AddModelError(model.CssContent, err.ToString());
                        }
                        return BadRequest(ModelState);
                    }
                }

                var req = new UpdatePageRequest
                {
                    HtmlContent = model.RawHtmlContent,
                    CssContent = model.CssContent,
                    HideSidebar = model.HideSidebar,
                    Slug = model.Slug,
                    MetaDescription = model.MetaDescription,
                    Title = model.Title,
                    IsPublished = model.IsPublished
                };

                var uid = model.Id == Guid.Empty ?
                    await _pageService.CreateAsync(req) :
                    await _pageService.UpdateAsync(model.Id, req);

                _cache.Remove(CacheDivision.Page, req.Slug.ToLower());

                return Json(new { PageId = uid });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Create or Edit CustomPage.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Json(e.Message);
            }
        }

        [HttpDelete("{pageId:guid}/{slug}")]
        public async Task<IActionResult> Delete(Guid pageId, string slug)
        {
            await _pageService.DeleteAsync(pageId);

            _cache.Remove(CacheDivision.Page, slug.ToLower());
            return Ok();
        }
    }
}