using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Web.Authentication;

namespace Moonglade.Web.Controllers
{
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
    [Route("api/graph")]
    [ApiController]
    public class BlogGraphController : ControllerBase
    {
        private readonly ILogger<BlogGraphController> _logger;
        private readonly TagService _tagService;
        private readonly CategoryService _categoryService;
        private readonly PostService _postService;
        private readonly CustomPageService _customPageService;

        public BlogGraphController(
            ILogger<BlogGraphController> logger,
            TagService tagService,
            CategoryService categoryService,
            PostService postService,
            CustomPageService customPageService)
        {
            _logger = logger;

            _tagService = tagService;
            _categoryService = categoryService;
            _postService = postService;
            _customPageService = customPageService;
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
            var response = await _tagService.GetAllAsync();
            if (response.IsSuccess)
            {
                return Ok(response.Item);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Categories()
        {
            var response = await _categoryService.GetAllAsync();
            if (response.IsSuccess)
            {
                return Ok(response.Item);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
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
        [ProducesResponseType(typeof(IEnumerable<CustomPageSegment>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SegmentPages()
        {
            var response = await _customPageService.ListSegmentAsync();
            if (response.IsSuccess)
            {
                // for security, only allow published pages to be listed to third party API calls
                var published = response.Item.Where(p => p.IsPublished);
                return Ok(published);
            }
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
