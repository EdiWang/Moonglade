using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Utils;
using Moonglade.Web.Filters;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet("list")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = ApiKeyAuthenticationOptions.DefaultScheme)]
        [ProducesResponseType(typeof(IReadOnlyList<Tag>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> List()
        {
            var tags = await _tagService.GetAll();
            return Ok(tags);
        }

        [HttpGet("names")]
        [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Names()
        {
            var tagNames = await _tagService.GetAllNames();
            return Ok(tagNames);
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest();
            if (!TagService.ValidateTagName(name)) return Conflict();

            await _tagService.Create(name.Trim());
            return Ok();
        }

        [HttpPut("update")]
        [TypeFilter(typeof(ClearPagingCountCache))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(EditTagRequest request)
        {
            await _tagService.UpdateAsync(request.TagId, request.NewName);
            return Ok();
        }

        [HttpDelete("{tagId}")]
        [TypeFilter(typeof(ClearPagingCountCache))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(int tagId)
        {
            if (tagId <= 0)
            {
                ModelState.AddModelError(nameof(tagId), "Value out of range");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            await _tagService.DeleteAsync(tagId);
            return Ok();
        }
    }

    public class EditTagRequest
    {
        [Range(1, int.MaxValue)]
        public int TagId { get; set; }

        [Required]
        public string NewName { get; set; }
    }
}