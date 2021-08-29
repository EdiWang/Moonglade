using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using Moonglade.Core.PostFeature;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages
{
    public class ArchiveListModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;
        public IReadOnlyList<PostDigest> Posts { get; set; }

        public ArchiveListModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public async Task<IActionResult> OnGetAsync(int year, int? month)
        {
            if (year > DateTime.UtcNow.Year) return BadRequest();

            IReadOnlyList<PostDigest> model;

            if (month is not null)
            {
                // {year}/{month}
                ViewData["ArchiveInfo"] = $"{year}.{month}";
                model = await _postQueryService.ListArchiveAsync(year, month);
            }
            else
            {
                // {year}
                ViewData["ArchiveInfo"] = $"{year}";
                model = await _postQueryService.ListArchiveAsync(year, null);
            }

            Posts = model;
            return Page();
        }
    }
}
