using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin
{
    public class PostDraftModel : PageModel
    {
        private readonly IPostService _postService;

        public IReadOnlyList<PostSegment> PostSegments { get; set; }

        public PostDraftModel(IPostService postService)
        {
            _postService = postService;
        }

        public async Task OnGet()
        {
            PostSegments = await _postService.ListSegment(PostStatus.Draft);
        }
    }
}
