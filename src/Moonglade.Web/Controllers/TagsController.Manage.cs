using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Moonglade.Web.Controllers
{
    public partial class TagsController
    {
        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return View(tags);
        }

        [Authorize]
        [HttpPost("update")]
        public IActionResult Update(int tagId, string newTagName)
        {
            Logger.LogInformation($"Updating tag ID {tagId} with new name '{newTagName}'");
            var response = _tagService.UpdateTag(tagId, newTagName);
            if (response.IsSuccess)
            {
                return Json(new { tagId, newTagName });
            }

            return ServerError();
        }

        [Authorize]
        [HttpPost("delete")]
        public IActionResult Delete(int tagId)
        {
            var response = _tagService.Delete(tagId);
            if (response.IsSuccess)
            {
                Logger.LogInformation($"Deleted tag {tagId}");
                return Json(tagId);
            }

            return ServerError();
        }
    }
}