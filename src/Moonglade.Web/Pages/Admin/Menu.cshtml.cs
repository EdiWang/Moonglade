using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Menus;

namespace Moonglade.Web.Pages.Admin
{
    public class MenuModel : PageModel
    {
        private readonly IMediator _mediator;

        [BindProperty]
        public IReadOnlyList<Menu> MenuItems { get; set; }

        public MenuModel(IMediator mediator)
        {
            _mediator = mediator;
            MenuItems = new List<Menu>();
        }

        public async Task OnGet()
        {
            MenuItems = await _mediator.Send(new GetAllMenusQuery());
        }
    }
}
