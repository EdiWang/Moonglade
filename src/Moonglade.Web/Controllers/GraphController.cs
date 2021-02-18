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
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Pages;
using Moonglade.Web.Filters;

namespace Moonglade.Web.Controllers
{
    [FeatureGate(FeatureFlags.EnableWebApi)]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [Route("api/graph")]
    [ApiController]
    [AppendAppVersion]
    public class GraphController : ControllerBase
    {
        private readonly ILogger<GraphController> _logger;
        private readonly ITagService _tagService;
        private readonly ICategoryService _categoryService;
        private readonly IPostService _postService;
        private readonly IPageService _pageService;

        public GraphController(
            ILogger<GraphController> logger,
            ITagService tagService,
            ICategoryService categoryService,
            IPostService postService,
            IPageService pageService)
        {
            _logger = logger;

            _tagService = tagService;
            _categoryService = categoryService;
            _postService = postService;
            _pageService = pageService;
        }

        [HttpGet("tags")]
        [ProducesResponseType(typeof(IEnumerable<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Tags()
        {
            var tags = await _tagService.GetAll();
            return Ok(tags);
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Categories()
        {
            var cats = await _categoryService.GetAll();
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
                var list = await _postService.ListSegment(PostStatus.Published);
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
            var pageSegments = await _pageService.ListSegment();
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
