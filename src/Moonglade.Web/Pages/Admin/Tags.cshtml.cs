using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages.Admin
{
    public class TagsModel : PageModel
    {
        private readonly IMediator _mediator;
        public IReadOnlyList<Tag> Tags { get; set; }

        public TagsModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task OnGet()
        {
            Tags = await _mediator.Send(new GetTagsQuery());
        }
    }
}
