using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Core;

namespace Moonglade.Web.Controllers
{
    [Route("tags")]
    public class TagsController : BlogController
    {
        private readonly TagService _tagService;

        public TagsController(
            ILogger<TagsController> logger,
            TagService tagService)
            : base(logger)
        {
            _tagService = tagService;
        }

        [Route("get-all-tag-names")]
        public async Task<IActionResult> GetAllTagNames()
        {
            var tagNames = await _tagService.GetAllNamesAsync();
            return Json(tagNames);
        }

        [Authorize]
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] EditTagRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _tagService.UpdateAsync(request.TagId, request.NewName);
            return Ok();
        }

        [Authorize]
        [HttpDelete("{tagId}")]
        public async Task<IActionResult> Delete(int tagId)
        {
            await _tagService.DeleteAsync(tagId);
            return Ok();
        }
    }

    public class EditTagRequest
    {
        [Range(1, 9999)]
        public int TagId { get; set; }

        [Required]
        public string NewName { get; set; }
    }
}