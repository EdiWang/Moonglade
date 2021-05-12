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
using Moonglade.Page;
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
        private readonly IBlogPageService _blogPageService;
        private readonly ILogger<PageController> _logger;

        public PageController(
            IBlogCache cache,
            IBlogPageService blogPageService,
            ILogger<PageController> logger)
        {
            _cache = cache;
            _blogPageService = blogPageService;
            _logger = logger;
        }

        [HttpGet("segment/published")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        [ProducesResponseType(typeof(IEnumerable<PageSegment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Segment()
        {
            var pageSegments = await _blogPageService.ListSegment();
            if (pageSegments is not null)
            {
                // for security, only allow published pages to be listed to third party API calls
                var published = pageSegments.Where(p => p.IsPublished);
                return Ok(published);
            }
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create(PageEditModel model)
        {
            try
            {
                return await CreateOrEdit(model, async request => await _blogPageService.CreateAsync(request));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Create CustomPage.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id:guid}")]
        [ServiceFilter(typeof(ClearSiteMapCache))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Edit(Guid id, PageEditModel model)
        {
            try
            {
                return await CreateOrEdit(model, async request => await _blogPageService.UpdateAsync(id, request));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Edit CustomPage.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<IActionResult> CreateOrEdit(PageEditModel model, Func<UpdatePageRequest, Task<Guid>> pageServiceAction)
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

            var uid = await pageServiceAction(req);

            _cache.Remove(CacheDivision.Page, req.Slug.ToLower());
            return Ok(new { PageId = uid });
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var page = await _blogPageService.GetAsync(id);
            if (page == null) return NotFound();

            await _blogPageService.DeleteAsync(id);

            _cache.Remove(CacheDivision.Page, page.Slug);
            return Ok();
        }
    }
}