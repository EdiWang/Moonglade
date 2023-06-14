using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.CategoryFeature;
using Moonglade.Core.PostFeature;
using X.PagedList;

namespace Moonglade.Web.Pages;

public class CategoryListModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IBlogConfig _blogConfig;
    private readonly ICacheAside _cache;

    [BindProperty(SupportsGet = true)]
    public int P { get; set; }
    public StaticPagedList<PostDigest> Posts { get; set; }
    public Category Cat { get; set; }

    public CategoryListModel(
        IBlogConfig blogConfig,
        IMediator mediator,
        ICacheAside cache)
    {
        _blogConfig = blogConfig;
        _mediator = mediator;
        _cache = cache;

        P = 1;
    }

    public async Task<IActionResult> OnGetAsync(string routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

        var pageSize = _blogConfig.ContentSettings.PostListPageSize;
        Cat = await _mediator.Send(new GetCategoryByRouteQuery(routeName));

        if (Cat is null) return NotFound();

        var postCount = await _cache.GetOrCreateAsync(BlogCachePartition.PostCountCategory.ToString(), Cat.Id.ToString(),
            _ => _mediator.Send(new CountPostQuery(CountType.Category, Cat.Id)));

        var postList = await _mediator.Send(new ListPostsQuery(pageSize, P, Cat.Id));

        Posts = new(postList, P, pageSize, postCount);
        return Page();
    }
}