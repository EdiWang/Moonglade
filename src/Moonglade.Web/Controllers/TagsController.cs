using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auth;
using Moonglade.Caching.Filters;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly IMediator _mediator;

        public TagsController(ITagService tagService, IMediator mediator)
        {
            _tagService = tagService;
            _mediator = mediator;
        }

        [HttpGet("list")]
        [FeatureGate(FeatureFlags.EnableWebApi)]
        [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
        [ProducesResponseType(typeof(IReadOnlyList<Tag>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List()
        {
            var tags = await _mediator.Send(new GetTagsQuery());
            return Ok(tags);
        }

        [HttpGet("names")]
        [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Names()
        {
            var tagNames = await _mediator.Send(new GetTagNamesQuery());
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
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
        public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, [Required][FromBody] string name)
        {
            var oc = await _tagService.UpdateAsync(id, name);
            if (oc == OperationCode.ObjectNotFound) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCacheType.PagingCount })]
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
        public async Task<IActionResult> Delete([Range(0, int.MaxValue)] int id)
        {
            var oc = await _mediator.Send(new DeleteTagCommand(id));
            if (oc == OperationCode.ObjectNotFound) return NotFound();

            return NoContent();
        }
    }
}