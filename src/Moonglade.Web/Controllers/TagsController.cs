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
        private readonly PostService _postService;

        public TagsController(
            ILogger<TagsController> logger,
            TagService tagService,
            PostService postService)
            : base(logger)
        {
            _tagService = tagService;
            _postService = postService;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var tags = await _tagService.GetTagCountListAsync();
                return View(tags);
            }
            catch (Exception e)
            {
                SetFriendlyErrorMessage();
                Logger.LogError(e, e.Message);
                return View();
            }
        }

        [Route("list/{normalizedName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> List(string normalizedName)
        {
            try
            {
                var tagResponse = _tagService.Get(normalizedName);
                if (tagResponse == null) return NotFound();

                ViewBag.TitlePrefix = tagResponse.DisplayName;
                var posts = await _postService.GetByTagAsync(tagResponse.Id);

                return View(posts);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                SetFriendlyErrorMessage();
                return View();
            }
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