using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages;

public class IndexModel(IBlogConfig blogConfig, ICacheAside cache, IMediator mediator) : PageModel
{
    public string SortBy { get; set; }

    public StaticPagedList<PostDigest> Posts { get; set; }

    public async Task OnGet(int p = 1, string sortBy = "Recent")
    {
        var pagesize = blogConfig.ContentSettings.PostListPageSize;
        if (Enum.TryParse(sortBy, out PostsSortBy sortByEnum))
        {
            ViewData["sortBy"] = sortBy;

            var posts = await mediator.Send(new ListPostsQuery(pagesize, p, sortBy: sortByEnum));
            var totalPostsCount = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "postcount", _ => mediator.Send(new CountPostQuery(CountType.Public)));

            var list = new StaticPagedList<PostDigest>(posts, p, pagesize, totalPostsCount);

            Posts = list;
        }
        else
        {
            throw new ArgumentException(message: $"Invalid argument value '{sortBy}' for argument: '{nameof(sortBy)}'!");
        }
    }
}