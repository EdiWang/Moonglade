using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("tags")]
    public partial class TagsController : MoongladeController
    {
        private readonly TagService _tagService;
        private readonly PostService _postService;

        public TagsController(MoongladeDbContext context,
            ILogger<TagsController> logger,
            IOptions<AppSettings> settings,
            TagService tagService, 
            PostService postService)
            : base(context, logger, settings)
        {
            _tagService = tagService;
            _postService = postService;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var response = await _tagService.GetTagCountListAsync();
            return !response.IsSuccess ? ServerError() : View(response.Item);
        }

        [Route("list/{normalizedName}")]
        public IActionResult List(string normalizedName)
        {
            ViewBag.ErrorMessage = string.Empty;

            var tag = _tagService.GetTag(normalizedName);

            var postResponse = _postService.GetPostsByTag(normalizedName.ToLower());
            if (!postResponse.IsSuccess)
            {
                return ServerError();
            }

            var posts = postResponse.Item.ToList();
            if (posts.Any())
            {
                ViewBag.TitlePrefix = tag.DisplayName;
                return View(posts);
            }
            return NotFound();
        }

        [Route("get-all-tag-names")]
        public IActionResult GetAllTagNames()
        {
            var tagNames = _tagService.GetAllTagNames();
            return Json(tagNames);
        }
    }
}