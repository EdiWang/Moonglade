using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.CategoryFeature;

namespace Moonglade.Web.Pages.Admin;

public class CategoryModel : PageModel
{
    private readonly IMediator _mediator;

    public EditCategoryRequest EditCategoryRequest { get; set; }

    public IReadOnlyList<Category> Categories { get; set; }

    public CategoryModel(IMediator mediator)
    {
        _mediator = mediator;
        EditCategoryRequest = new();
    }

    public async Task OnGet()
    {
        Categories = await _mediator.Send(new GetCategoriesQuery());
    }
}