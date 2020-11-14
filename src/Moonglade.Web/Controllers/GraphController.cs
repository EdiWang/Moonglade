using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Web.Authentication;
using Moonglade.Web.Filters;

namespace Moonglade.Web.Controllers
{
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [Route("api/graph")]
    [ApiController]
    [AppendMoongladeVersion]
    public class GraphController : ControllerBase
    {
        private readonly ILogger<GraphController> _logger;
        private readonly TagService _tagService;
        private readonly CategoryService _categoryService;
        private readonly PostService _postService;
        private readonly PageService _pageService;

        public GraphController(
            ILogger<GraphController> logger,
            TagService tagService,
            CategoryService categoryService,
            PostService postService,
            PageService pageService)
        {
            _logger = logger;

            _tagService = tagService;
            _categoryService = categoryService;
            _postService = postService;
            _pageService = pageService;
        }

        [HttpGet("version")]
        public ActionResult<string> Version()
        {
            return Utils.AppVersion;
        }

        [HttpGet("tags")]
        [ProducesResponseType(typeof(IEnumerable<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Tags()
        {
            var tags = await _tagService.GetAllAsync();
            return Ok(tags);
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Categories()
        {
            var cats = await _categoryService.GetAllAsync();
            return Ok(cats);
        }

        [HttpGet("posts/segment/published")]
        [ProducesResponseType(typeof(IEnumerable<PostSegment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SegmentPosts()
        {
            try
            {
                // for security, only allow published posts to be listed to third party API calls
                var list = await _postService.ListSegmentAsync(PostPublishStatus.Published);
                return Ok(list);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("pages/segment/published")]
        [ProducesResponseType(typeof(IEnumerable<PageSegment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SegmentPages()
        {
            var pageSegments = await _pageService.ListSegmentAsync();
            if (pageSegments is not null)
            {
                // for security, only allow published pages to be listed to third party API calls
                var published = pageSegments.Where(p => p.IsPublished);
                return Ok(published);
            }
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
