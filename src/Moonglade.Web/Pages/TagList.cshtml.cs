using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Core.TagFeature;
using X.PagedList;

namespace Moonglade.Web.Pages;

public class TagListModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IBlogConfig _blogConfig;
    private readonly ICacheAside _cache;

    [BindProperty(SupportsGet = true)]
    public int P { get; set; }
    public StaticPagedList<PostDigest> Posts { get; set; }

    public TagListModel(IMediator mediator, IBlogConfig blogConfig, ICacheAside cache)
    {
        _mediator = mediator;
        _blogConfig = blogConfig;
        _cache = cache;

        P = 1;
    }

    public async Task<IActionResult> OnGet(string normalizedName)
    {
        var tagResponse = await _mediator.Send(new GetTagQuery(normalizedName));
        if (tagResponse is null) return NotFound();

        var pagesize = _blogConfig.ContentSettings.PostListPageSize;
        var posts = await _mediator.Send(new ListByTagQuery(tagResponse.Id, pagesize, P));
        var count = await _cache.GetOrCreateAsync(BlogCachePartition.PostCountTag.ToString(), tagResponse.Id.ToString(), _ => _mediator.Send(new CountPostQuery(CountType.Tag, TagId: tagResponse.Id)));

        ViewData["TitlePrefix"] = tagResponse.DisplayName;

        var list = new StaticPagedList<PostDigest>(posts, P, pagesize, count);
        Posts = list;

        return Page();
    }
}