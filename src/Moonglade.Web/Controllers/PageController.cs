using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [ApiController]
    [AppendAppVersion]
    [Route("api/[controller]")]
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

        [HttpPost("createoredit")]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrEdit([FromForm] PageEditModel model)
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

                return Ok(new { PageId = uid });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Create or Edit CustomPage.");
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete("{pageId:guid}/{slug}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid pageId, string slug)
        {
            await _pageService.DeleteAsync(pageId);

            _cache.Remove(CacheDivision.Page, slug.ToLower());
            return Ok();
        }
    }
}