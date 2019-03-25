using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.Web.Controllers
{
    public partial class TagsController
    {
        [Authorize]
        [Route("manage")]
        public IActionResult Manage()
        {
            var query = _tagService.GetTags().Select(t => new TagGridModel
            {
                Id = t.Id,
                Name = t.DisplayName
            });

            var grid = query.ToList();
            return View(grid);
        }

        [Authorize]
        [HttpPost]
        [Route("update")]
        public IActionResult Update(int tagId, string newTagName)
        {
            Logger.LogInformation($"Updating tag ID {tagId} with new name '{newTagName}'");
            var response = _tagService.UpdateTag(tagId, newTagName);
            if (response.IsSuccess)
            {
                return Json(new {tagId, newTagName});
            }

            return ServerError();
        }

        [Authorize]
        [HttpPost]
        [Route("delete")]
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