using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;

namespace Moonglade.Web.Pages.Admin
{
    public class EditPageModel : PageModel
    {
        private readonly IMediator _mediator;

        public Guid PageId { get; set; }

        public EditPageRequest EditPageRequest { get; set; }

        public EditPageModel(IMediator mediator)
        {
            _mediator = mediator;
            EditPageRequest = new();
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id is null) return Page();

            var page = await _mediator.Send(new GetPageByIdQuery(id.Value));
            if (page is null) return NotFound();

            PageId = page.Id;

            EditPageRequest = new()
            {
                Title = page.Title,
                Slug = page.Slug,
                MetaDescription = page.MetaDescription,
                CssContent = page.CssContent,
                RawHtmlContent = page.RawHtmlContent,
                HideSidebar = page.HideSidebar,
                IsPublished = page.IsPublished
            };

            return Page();
        }
    }
}
