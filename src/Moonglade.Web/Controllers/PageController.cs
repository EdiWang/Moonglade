using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Pages;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using NUglify;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
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

        [HttpGet("segment/published")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        [ProducesResponseType(typeof(IEnumerable<PageSegment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Segment()
        {
            var pageSegments = await _pageService.ListSegment();
            if (pageSegments is not null)
            {
                // for security, only allow published pages to be listed to third party API calls
                var published = pageSegments.Where(p => p.IsPublished);
                return Ok(published);
            }
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost("createoredit")]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateOrEdit(PageEditModel model)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(model.CssContent))
                {
                    var uglifyTest = Uglify.Css(model.CssContent);
                    if (uglifyTest.HasErrors)
                    {
                        foreach (var err in uglifyTest.Errors)
                        {
                            ModelState.AddModelError(model.CssContent, err.ToString());
                        }
                        return BadRequest(ModelState.CombineErrorMessages());
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