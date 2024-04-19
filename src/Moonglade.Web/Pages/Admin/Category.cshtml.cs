using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.CategoryFeature;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Pages.Admin;

public class CategoryModel(IMediator mediator) : PageModel
{
    public CreateCategoryCommand EditCategoryRequest { get; set; } = new();

    public List<CategoryEntity> Categories { get; set; }

    public async Task OnGet() => Categories = await mediator.Send(new GetCategoriesQuery());
}