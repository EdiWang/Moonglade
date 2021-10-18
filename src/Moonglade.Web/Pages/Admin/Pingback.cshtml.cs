using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;
using Moonglade.Pingback;

namespace Moonglade.Web.Pages.Admin
{
    public class PingbackModel : PageModel
    {
        private readonly IMediator _mediator;

        public IReadOnlyList<PingbackEntity> PingbackRecords { get; set; }

        public PingbackModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task OnGet()
        {
            PingbackRecords = await _mediator.Send(new GetPingbacksQuery());
        }
    }
}
