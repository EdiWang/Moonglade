using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;

namespace Moonglade.Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost("search")]
        public IActionResult Post(string term)
        {
            return !string.IsNullOrWhiteSpace(term) ?
                RedirectToAction(nameof(Search), new { term }) :
                RedirectToAction("Index", "Home");
        }

        [HttpGet("search/{term}")]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TitlePrefix = term;

            var posts = await _searchService.SearchAsync(term);
            return View(posts);
        }
    }
}