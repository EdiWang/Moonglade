using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Web.Controllers
{
    public partial class TagsController
    {
        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var response = await _tagService.GetAllTagsAsync();
            return response.IsSuccess ? View(response.Item) : ServerError();
        }

        [Authorize]
        [HttpPost("update")]
        public IActionResult Update(int tagId, string newTagName)
        {
            Logger.LogInformation($"User '{User.Identity.Name}' updating tag id '{tagId}' with new name '{newTagName}'");
            var response = _tagService.UpdateTag(tagId, newTagName);
            _moongladeAudit.AddAuditEntry(EventType.Content, EventId.TagUpdated, $"Tag id '{tagId}' is updated.");

            return response.IsSuccess ? Json(new { tagId, newTagName }) : ServerError();
        }

        [Authorize]
        [HttpPost("delete")]
        public IActionResult Delete(int tagId)
        {
            var response = _tagService.Delete(tagId);
            if (response.IsSuccess)
            {
                Logger.LogInformation($"User '{User.Identity.Name}' deleted tag id: '{tagId}'");
                _moongladeAudit.AddAuditEntry(EventType.Content, EventId.TagDeleted, $"Tag id '{tagId}' is deleted");

                return Json(tagId);
            }

            return ServerError();
        }
    }
}