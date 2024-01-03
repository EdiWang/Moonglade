using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages;

public class FeaturedModel(IBlogConfig blogConfig, ICacheAside cache, IMediator mediator) : PageModel
{
    public BasePagedList<PostDigest> Posts { get; set; }

    public async Task OnGet(int p = 1)
    {
        var pagesize = blogConfig.ContentSettings.PostListPageSize;
        var posts = await mediator.Send(new ListFeaturedQuery(pagesize, p));
        var count = await cache.GetOrCreateAsync(BlogCachePartition.PostCountFeatured.ToString(), "featured", _ => mediator.Send(new CountPostQuery(CountType.Featured)));

        var list = new BasePagedList<PostDigest>(posts, p, pagesize, count);
        Posts = list;
    }
}