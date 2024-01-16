using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.CategoryFeature;
using Moonglade.Core.PostFeature;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages;

public class CategoryListModel(
        IBlogConfig blogConfig,
        IMediator mediator,
        ICacheAside cache) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;
    public BasePagedList<PostDigest> Posts { get; set; }
    public Category Cat { get; set; }

    public async Task<IActionResult> OnGetAsync(string routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

        var pageSize = blogConfig.ContentSettings.PostListPageSize;
        Cat = await mediator.Send(new GetCategoryByRouteQuery(routeName));

        if (Cat is null) return NotFound();

        var postCount = await cache.GetOrCreateAsync(BlogCachePartition.PostCountCategory.ToString(), Cat.Id.ToString(),
            _ => mediator.Send(new CountPostQuery(CountType.Category, Cat.Id)));

        var postList = await mediator.Send(new ListPostsQuery(pageSize, P, Cat.Id));

        Posts = new(postList, P, pageSize, postCount);
        return Page();
    }
}