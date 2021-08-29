using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using System.Collections.Generic;
using System.Threading.Tasks;

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
