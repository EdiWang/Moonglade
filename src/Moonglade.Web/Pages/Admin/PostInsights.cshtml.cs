using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin
{
    public class PostInsightsModel : PageModel
    {
        private readonly IPostService _postService;

        public IReadOnlyList<PostSegment> TopReadList { get; set; }

        public IReadOnlyList<PostSegment> TopCommentedList { get; set; }

        public PostInsightsModel(IPostService postService)
        {
            _postService = postService;
        }

        public async Task OnGet()
        {
            TopReadList = await _postService.ListInsights(PostInsightsType.TopRead);
            TopCommentedList = await _postService.ListInsights(PostInsightsType.TopCommented);
        }
    }
}
