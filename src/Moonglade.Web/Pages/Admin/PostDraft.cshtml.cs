using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin
{
    public class PostDraftModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;

        public IReadOnlyList<PostSegment> PostSegments { get; set; }

        public PostDraftModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public async Task OnGet()
        {
            PostSegments = await _postQueryService.ListSegmentAsync(PostStatus.Draft);
        }
    }
}
