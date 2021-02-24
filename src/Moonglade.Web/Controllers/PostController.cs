using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Pingback.AspNetCore;

namespace Moonglade.Web.Controllers
{
    [Route("post")]
    public class PostController : Controller
    {
        private readonly IPostService _postService;
        private readonly IBlogConfig _blogConfig;

        public PostController(
            IPostService postService,
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

            var slugInfo = new PostSlug(year, month, day, slug);
            var post = await _postService.GetAsync(slugInfo);

            if (post is null) return NotFound();

            ViewBag.TitlePrefix = $"{post.Title}";
            return View(post);
        }

        [Authorize]
        [Route("preview/{postId:guid}")]
        public async Task<IActionResult> Preview(Guid postId)
        {
            var post = await _postService.GetDraft(postId);
            if (post is null) return NotFound();

            ViewBag.TitlePrefix = $"{post.Title}";
            ViewBag.IsDraftPreview = true;
            return View("Slug", post);
        }
    }
}