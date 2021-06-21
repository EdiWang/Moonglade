using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Caching.Filters;
using Moonglade.Configuration.Settings;
using Moonglade.Core;

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
        [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
        [ProducesResponseType(typeof(IReadOnlyList<Tag>), StatusCodes.Status200OK)]
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([Required][FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return BadRequest();
            if (!TagService.ValidateTagName(name)) return Conflict();

            await _tagService.Create(name.Trim());
            return Ok();
        }

        [HttpPut("{id:int}")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, [Required][FromBody] string name)
        {
            await _tagService.UpdateAsync(id, name);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
        [ProducesResponseType(StatusCodes.Status204NoContent),]
        public async Task<IActionResult> Delete([Range(0, int.MaxValue)] int id)
        {
            await _tagService.DeleteAsync(id);
            return NoContent();
        }
    }
}