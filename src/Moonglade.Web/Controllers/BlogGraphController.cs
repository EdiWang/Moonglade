using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
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

        public BlogGraphController(
            ILogger<BlogGraphController> logger,
            TagService tagService,
            CategoryService categoryService)
        {
            _logger = logger;

            _tagService = tagService;
            _categoryService = categoryService;
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
    }
}
