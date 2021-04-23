using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;

namespace Moonglade.Web.Pages
{
    public class ArchiveModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;

        public ArchiveModel(IPostQueryService postQueryService)
        {
            _postQueryService = postQueryService;
        }

        public IReadOnlyList<Archive> Archives { get; set; }

        public async Task OnGet()
        {
            var archives = await _postQueryService.GetArchiveAsync();
            Archives = archives;
        }
    }
}
