using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Pages
{
    [Authorize]
    public class BlogPagePreviewModel : PageModel
    {
        private readonly IMediator _mediator;

        public BlogPage BlogPage { get; set; }

        public BlogPagePreviewModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> OnGetAsync(Guid pageId)
        {
            var page = await _mediator.Send(new GetPageByIdQuery(pageId));
            if (page is null) return NotFound();

            BlogPage = page;
            return Page();
        }
    }
}
