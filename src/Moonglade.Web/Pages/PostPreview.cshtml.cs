using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;

namespace Moonglade.Web.Pages
{
    [Authorize]
    public class PostPreviewModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;

        public Post Post { get; set; }

        public PostPreviewModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public async Task<IActionResult> OnGetAsync(Guid postId)
        {
            var post = await _postQueryService.GetDraftAsync(postId);
            if (post is null) return NotFound();

            ViewData["TitlePrefix"] = $"{post.Title}";

            Post = post;
            return Page();
        }
    }
}
