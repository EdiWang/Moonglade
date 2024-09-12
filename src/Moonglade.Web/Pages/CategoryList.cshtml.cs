using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.CategoryFeature;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages;

public class CategoryListModel(IBlogConfig blogConfig, IMediator mediator) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;
    public BasePagedList<PostDigest> Posts { get; set; }
    public CategoryEntity Cat { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var pageSize = blogConfig.ContentSettings.PostListPageSize;
        Cat = await mediator.Send(new GetCategoryBySlugQuery(slug));

        if (Cat is null) return NotFound();

        var postCount = await mediator.Send(new CountPostQuery(CountType.Category, Cat.Id));
        var postList = await mediator.Send(new ListPostsQuery(pageSize, P, Cat.Id));

        Posts = new(postList, P, pageSize, postCount);
        return Page();
    }
}