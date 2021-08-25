using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

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
