using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("tags")]
    public partial class TagsController : MoongladeController
    {
        private readonly TagService _tagService;
        private readonly PostService _postService;
        private readonly IMoongladeAudit _moongladeAudit;

        public TagsController(
            ILogger<TagsController> logger,
            IOptions<AppSettings> settings,
            TagService tagService,
            PostService postService, 
            IMoongladeAudit moongladeAudit)
            : base(logger, settings)
        {
            _tagService = tagService;
            _postService = postService;
            _moongladeAudit = moongladeAudit;
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
            var tagResponse = _tagService.GetTag(normalizedName);
            if (!tagResponse.IsSuccess)
            {
                SetFriendlyErrorMessage();
                return View();
            }

            if (tagResponse.Item == null)
            {
                return NotFound();
            }

            ViewBag.TitlePrefix = tagResponse.Item.TagName;
            var postResponse = await _postService.GetPostsByTagAsync(tagResponse.Item.Id);
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
            var tagNames = await _tagService.GetAllTagNamesAsync();
            return Json(tagNames.Item);
        }
    }
}