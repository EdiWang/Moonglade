using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;

namespace Moonglade.Web.Pages
{
    public class SearchModel : PageModel
    {
        private readonly ISearchService _searchService;

        public SearchModel(ISearchService searchService)
        {
            _searchService = searchService;
        }

        public IReadOnlyList<PostDigest> Posts { get; set; }

        public async Task<IActionResult> OnGetAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return RedirectToPage("Index");
            }

            ViewData["TitlePrefix"] = term;

            var posts = await _searchService.SearchAsync(term);
            Posts = posts;

            return Page();
        }
    }
}
