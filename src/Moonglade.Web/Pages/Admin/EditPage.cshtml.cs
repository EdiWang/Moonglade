using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Pages;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Admin
{
    public class EditPageModel : PageModel
    {
        private readonly IBlogPageService _blogPageService;

        public Guid PageId { get; set; }

        public PageEditModel PageEditModel { get; set; }

        public EditPageModel(IBlogPageService blogPageService)
        {
            _blogPageService = blogPageService;
            PageEditModel = new();
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id is null) return Page();

            var page = await _blogPageService.GetAsync(id.Value);
            if (page is null) return NotFound();

            PageId = page.Id;

            PageEditModel = new()
            {
                Title = page.Title,
                Slug = page.Slug,
                MetaDescription = page.MetaDescription,
                CssContent = page.CssContent,
                RawHtmlContent = page.RawHtmlContent,
                HideSidebar = page.HideSidebar,
                IsPublished = page.IsPublished
            };

            return Page();
        }
    }
}
