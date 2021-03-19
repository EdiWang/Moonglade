using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Pages.Admin
{
    public class RecycleBinModel : PageModel
    {
        private readonly IPostService _postService;

        public IReadOnlyList<PostSegment> Posts { get; set; }

        public RecycleBinModel(IPostService postService)
        {
            _postService = postService;
        }

        public async Task OnGet()
        {
            Posts = await _postService.ListSegment(PostStatus.Deleted);
        }
    }
}
