using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Pingback.Mvc;

namespace Moonglade.Web.Controllers
{
    [Route("post")]
    public class PostController : BlogController
    {
        private readonly PostService _postService;
        private readonly IBlogConfig _blogConfig;

        public PostController(
            PostService postService,
            IBlogConfig blogConfig)
        {
            _postService = postService;
            _blogConfig = blogConfig;
        }

        [Route("{year:int:min(1975):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}")]
        [AddPingbackHeader("pingback")]
        public async Task<IActionResult> Slug(int year, int month, int day, string slug)
        {
            if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

            var slugInfo = new PostSlugInfo(year, month, day, slug);
            var post = await _postService.GetAsync(slugInfo);

            if (post is null) return NotFound();

            ViewBag.TitlePrefix = $"{post.Title}";
            return View(post);
        }

        [Route("{year:int:min(1975):length(4)}/{month:int:range(1,12)}/{day:int:range(1,31)}/{slug:regex(^(?!-)([[a-zA-Z0-9-]]+)$)}/{type:regex(^(meta|content)$)}")]
        public async Task<IActionResult> Raw(int year, int month, int day, string slug, string type)
        {
            var slugInfo = new PostSlugInfo(year, month, day, slug);

            if (!_blogConfig.SecuritySettings.EnablePostRawEndpoint
                || year > DateTime.UtcNow.Year
                || string.IsNullOrWhiteSpace(slug)) return NotFound();

            switch (type.ToLower())
            {
                case "meta":
                    var meta = await _postService.GetSegmentAsync(slugInfo);
                    return Json(meta);

                case "content":
                    var content = await _postService.GetContentAsync(slugInfo);
                    return Content(content, "text/plain");
            }

            return BadRequest();
        }

        [Authorize]
        [Route("preview/{postId:guid}")]
        public async Task<IActionResult> Preview(Guid postId)
        {
            var post = await _postService.GetDraftAsync(postId);
            if (post is null) return NotFound();

            ViewBag.TitlePrefix = $"{post.Title}";
            ViewBag.IsDraftPreview = true;
            return View("Slug", post);
        }
    }
}