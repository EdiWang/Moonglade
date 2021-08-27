using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.FriendLink;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class FriendLinkModel : PageModel
    {
        private readonly IMediator _mediator;

        public FriendLinkEditModel FriendLinkEditViewModel { get; set; }

        public IReadOnlyList<Link> FriendLinks { get; set; }

        public FriendLinkModel(IMediator mediator)
        {
            _mediator = mediator;
            FriendLinkEditViewModel = new();
        }

        public async Task OnGet()
        {
            FriendLinks = await _mediator.Send(new GetAllLinksQuery());
        }
    }
}
