using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Page;

namespace Moonglade.Web.Pages.Admin
{
    public class BlogPageModel : PageModel
    {
        private readonly IBlogPageService _blogPageService;

        public IReadOnlyList<PageSegment> PageSegments { get; set; }

        public BlogPageModel(IBlogPageService blogPageService)
        {
            _blogPageService = blogPageService;
        }

        public async Task OnGet()
        {
            PageSegments = await _blogPageService.ListSegment();
        }
    }
}
