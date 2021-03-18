using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Pages;

namespace Moonglade.Web.Pages.Admin
{
    public class BlogPageModel : PageModel
    {
        private readonly IPageService _pageService;

        public IReadOnlyList<PageSegment> PageSegments { get; set; }

        public BlogPageModel(IPageService pageService)
        {
            _pageService = pageService;
        }

        public async Task OnGet()
        {
            PageSegments = await _pageService.ListSegment();
        }
    }
}
