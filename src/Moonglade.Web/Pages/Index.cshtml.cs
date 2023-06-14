using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using X.PagedList;

namespace Moonglade.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;
    private readonly ICacheAside _cache;

    public string SortBy { get; set; }

    public StaticPagedList<PostDigest> Posts { get; set; }

    public IndexModel(
        IBlogConfig blogConfig, ICacheAside cache, IMediator mediator)
    {
        _blogConfig = blogConfig;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task OnGet(int p = 1, string sortBy = "Recent")
    {
        var pagesize = _blogConfig.ContentSettings.PostListPageSize;
        if (Enum.TryParse(sortBy, out PostsSortBy sortByEnum))
        {
            ViewData["sortBy"] = sortBy;

            var posts = await _mediator.Send(new ListPostsQuery(pagesize, p, sortBy: sortByEnum));
            var totalPostsCount = await _cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "postcount", _ => _mediator.Send(new CountPostQuery(CountType.Public)));

            var list = new StaticPagedList<PostDigest>(posts, p, pagesize, totalPostsCount);

            Posts = list;
        }
        else
        {
            throw new ArgumentException(message: $"Invalid argument value '{sortBy}' for argument: '{nameof(sortBy)}'!");
        }
    }
}