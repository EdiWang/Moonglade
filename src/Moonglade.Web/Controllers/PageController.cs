using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Caching;
using Moonglade.Caching.Filters;
using Moonglade.Configuration.Settings;
using Moonglade.Page;
using Moonglade.Utils;
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

        public PageController(
            IBlogCache cache,
            IBlogPageService blogPageService)
        {
            _cache = cache;
            _blogPageService = blogPageService;
        }

        [HttpGet("segment/published")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
        [ProducesResponseType(typeof(IEnumerable<PageSegment>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Segment()
        {
            var pageSegments = await _blogPageService.ListSegmentAsync();
            if (pageSegments is null) return Ok(Array.Empty<PageSegment>());

            // for security, only allow published pages to be listed to third party API calls
            var published = pageSegments.Where(p => p.IsPublished);
            return Ok(published);
        }

        [HttpPost]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IActionResult> Create(PageEditModel model)
        {
            return CreateOrEdit(model, async request => await _blogPageService.CreateAsync(request));
        }

        [HttpPut("{id:guid}")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.SiteMap })]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IActionResult> Edit([NotEmpty] Guid id, PageEditModel model)
        {
            return CreateOrEdit(model, async request => await _blogPageService.UpdateAsync(id, request));
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
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
        public async Task<IActionResult> Delete([NotEmpty] Guid id)
        {
            var page = await _blogPageService.GetAsync(id);
            if (page == null) return NotFound();

            await _blogPageService.DeleteAsync(id);

            _cache.Remove(CacheDivision.Page, page.Slug);
            return NoContent();
        }
    }
}