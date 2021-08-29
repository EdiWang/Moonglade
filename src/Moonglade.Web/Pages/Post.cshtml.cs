using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Core.PostFeature;
using Moonglade.Pingback;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages
{
    [AddPingbackHeader("pingback")]
    public class PostModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;

        public Post Post { get; set; }

        public PostModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
        {
            if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

            var slugInfo = new PostSlug(year, month, day, slug);
            var post = await _postQueryService.GetAsync(slugInfo);

            if (post is null) return NotFound();

            ViewData["TitlePrefix"] = $"{post.Title}";

            Post = post;
            return Page();
        }
    }
}
