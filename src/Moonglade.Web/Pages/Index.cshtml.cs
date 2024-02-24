using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages;

public class IndexModel(IBlogConfig blogConfig, ICacheAside cache, IMediator mediator) : PageModel
{
    public BasePagedList<PostDigest> Posts { get; set; }

    public async Task OnGet(int p = 1)
    {
        var pagesize = blogConfig.ContentSettings.PostListPageSize;

        var posts = await mediator.Send(new ListPostsQuery(pagesize, p));
        var totalPostsCount = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "postcount", _ => mediator.Send(new CountPostQuery(CountType.Public)));

        var list = new BasePagedList<PostDigest>(posts, p, pagesize, totalPostsCount);

        Posts = list;
    }
}