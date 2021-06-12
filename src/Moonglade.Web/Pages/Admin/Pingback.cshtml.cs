using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;
using Moonglade.Pingback;

namespace Moonglade.Web.Pages.Admin
{
    public class PingbackModel : PageModel
    {
        private readonly IPingbackService _pingbackService;

        public IReadOnlyList<PingbackEntity> PingbackRecords { get; set; }

        public PingbackModel(IPingbackService pingbackService)
        {
            _pingbackService = pingbackService;
        }

        public async Task OnGet()
        {
            PingbackRecords = await _pingbackService.GetPingbackHistoryAsync();
        }
    }
}
