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
        private readonly IPageService _pageService;

        public PageEditModel PageEditModel { get; set; }

        public EditPageModel(IPageService pageService)
        {
            _pageService = pageService;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id is null)
            {
                PageEditModel = new();
                return Page();
            }

            var page = await _pageService.GetAsync(id.Value);
            if (page is null) return NotFound();

            PageEditModel = new()
            {
                Id = page.Id,
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
