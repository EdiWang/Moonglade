using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Data.Spec;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class RecycleBinModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;

        public IReadOnlyList<PostSegment> Posts { get; set; }

        public RecycleBinModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public async Task OnGet()
        {
            Posts = await _postQueryService.ListSegmentAsync(PostStatus.Deleted);
        }
    }
}
