using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using X.PagedList;

namespace Moonglade.Web.Pages;

public class FeaturedModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;
    private readonly ICacheAside _cache;
    public StaticPagedList<PostDigest> Posts { get; set; }

    public FeaturedModel(
        IBlogConfig blogConfig, ICacheAside cache, IMediator mediator)
    {
        _blogConfig = blogConfig;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task OnGet(int p = 1)
    {
        var pagesize = _blogConfig.ContentSettings.PostListPageSize;
        var posts = await _mediator.Send(new ListFeaturedQuery(pagesize, p));
        var count = await _cache.GetOrCreateAsync(BlogCachePartition.PostCountFeatured.ToString(), "featured", _ => _mediator.Send(new CountPostQuery(CountType.Featured)));

        var list = new StaticPagedList<PostDigest>(posts, p, pagesize, count);
        Posts = list;
    }
}