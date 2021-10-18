using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;

namespace Moonglade.Web.Pages
{
    public class ArchiveModel : PageModel
    {
        private readonly IMediator _mediator;

        public ArchiveModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public IReadOnlyList<Archive> Archives { get; set; }

        public async Task OnGet()
        {
            var archives = await _mediator.Send(new GetArchiveQuery());
            Archives = archives;
        }
    }
}
