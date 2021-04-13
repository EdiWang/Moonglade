using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Data.Spec;

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
            Posts = await _postQueryService.ListSegment(PostStatus.Deleted);
        }
    }
}
