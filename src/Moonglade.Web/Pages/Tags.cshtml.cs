using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages
{
    public class TagsModel : PageModel
    {
        private readonly IMediator _mediator;
        public IReadOnlyList<KeyValuePair<Tag, int>> Tags { get; set; }

        public TagsModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task OnGet()
        {
            var tags = await _mediator.Send(new GetTagCountListQuery());
            Tags = tags;
        }
    }
}
