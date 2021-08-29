using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class PostInsightsModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;

        public IReadOnlyList<PostSegment> TopReadList { get; set; }

        public IReadOnlyList<PostSegment> TopCommentedList { get; set; }

        public PostInsightsModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public async Task OnGet()
        {
            TopReadList = await _postQueryService.ListSegmentAsync(PostInsightsType.TopRead);
            TopCommentedList = await _postQueryService.ListSegmentAsync(PostInsightsType.TopCommented);
        }
    }
}
