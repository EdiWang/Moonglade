using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.FriendLink;

namespace Moonglade.Web.Pages.Admin
{
    public class FriendLinkModel : PageModel
    {
        private readonly IMediator _mediator;

        public EditLinkRequest EditLinkRequest { get; set; }

        public IReadOnlyList<Link> Links { get; set; }

        public FriendLinkModel(IMediator mediator)
        {
            _mediator = mediator;
            EditLinkRequest = new();
        }

        public async Task OnGet()
        {
            Links = await _mediator.Send(new GetAllLinksQuery());
        }
    }
}
