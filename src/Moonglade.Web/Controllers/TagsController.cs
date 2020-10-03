using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("tags")]
    public class TagsController : BlogController
    {
        private readonly TagService _tagService;
        private readonly PostService _postService;

        public TagsController(
            ILogger<TagsController> logger,
            IOptions<AppSettings> settings,
            TagService tagService,
            PostService postService)
            : base(logger, settings)
        {
            _tagService = tagService;
            _postService = postService;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var response = await _tagService.GetTagCountListAsync();
            if (!response.IsSuccess)
            {
                SetFriendlyErrorMessage();
            }
            return View(response.Item);
        }

        [Route("list/{normalizedName:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        public async Task<IActionResult> List(string normalizedName)
        {
            var tagResponse = _tagService.Get(normalizedName);
            if (!tagResponse.IsSuccess)
            {
                SetFriendlyErrorMessage();
                return View();
            }

            if (tagResponse.Item == null) return NotFound();

            ViewBag.TitlePrefix = tagResponse.Item.DisplayName;
            var postResponse = await _postService.GetByTagAsync(tagResponse.Item.Id);
            if (!postResponse.IsSuccess)
            {
                SetFriendlyErrorMessage();
                return View();
            }

            var posts = postResponse.Item;
            return View(posts);
        }

        [Route("get-all-tag-names")]
        public async Task<IActionResult> GetAllTagNames()
        {
            var tagNames = await _tagService.GetAllNamesAsync();
            return Json(tagNames.Item);
        }

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var response = await _tagService.GetAllAsync();
            return response.IsSuccess ? View("~/Views/Admin/ManageTags.cshtml", response.Item) : ServerError();
        }

        [Authorize]
        [HttpPost("update")]
        public async Task<IActionResult> Update(int tagId, string newTagName)
        {
            var response = await _tagService.UpdateAsync(tagId, newTagName);
            return response.IsSuccess ? Json(new { tagId, newTagName }) : ServerError();
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(int tagId)
        {
            var response = await _tagService.DeleteAsync(tagId);
            return response.IsSuccess ? Json(tagId) : ServerError();
        }
    }
}