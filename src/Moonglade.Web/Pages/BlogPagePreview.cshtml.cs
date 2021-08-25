using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages
{
    [Authorize]
    public class BlogPagePreviewModel : PageModel
    {
        private readonly IBlogPageService _blogPageService;

        public BlogPage BlogPage { get; set; }

        public BlogPagePreviewModel(IBlogPageService blogPageService)
        {
            _blogPageService = blogPageService;
        }

        public async Task<IActionResult> OnGetAsync(Guid pageId)
        {
            var page = await _blogPageService.GetAsync(pageId);
            if (page is null) return NotFound();

            BlogPage = page;
            return Page();
        }
    }
}
